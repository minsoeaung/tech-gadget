using System.Web;
using API.ApiErrors;
using API.Configurations;
using API.Data;
using API.DTOs.Responses;
using API.Entities;
using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Created = ErrorOr.Created;

namespace API.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<User> _userManager;
    private readonly IImageService _imageService;
    private readonly AwsConfig _awsConfig;
    private readonly ITokenService _tokenService;
    private readonly StoreContext _context;


    public AccountService(UserManager<User> userManager, IImageService imageService, IOptions<AwsConfig> awsConfig,
        ITokenService tokenService, StoreContext context)
    {
        _userManager = userManager;
        _imageService = imageService;
        _tokenService = tokenService;
        _context = context;
        _awsConfig = awsConfig.Value;
    }

    public async Task<ErrorOr<(User, IList<string> roles)>> GetAccount(int userId)
    {
        if (userId == 0)
            return Error.Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return Errors.User.NotFound;

        return (user, await _userManager.GetRolesAsync(user));
    }

    public async Task<ErrorOr<Updated>> UpdateProfilePicture(int userId, IFormFile picture)
    {
        if (userId == 0)
            return Error.Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return Errors.User.NotFound;

        await _imageService.UploadImageAsync(user.Id, picture, ImageFolders.UserImages);

        // ProfilePicture is always the same, only the pic is changed.
        if (user.ProfilePicture == null)
        {
            user.ProfilePicture = $"{_awsConfig.DistributionDomainName}/{ImageFolders.UserImages}/{user.Id}";
            await _userManager.UpdateAsync(user);
        }

        return Result.Updated;
    }

    public async Task<ErrorOr<Created>> CreateAdminAccount(string username, string email, string password)
    {
        var existingUser = await _userManager.FindByNameAsync(username);
        if (existingUser != null)
            return Errors.User.DuplicateUserName;

        // Validations are done using "DataAnnotations" in DTO
        var newUser = new User
        {
            UserName = username.Trim(),
            Email = email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserAddress = new UserAddress()
        };

        var createResult = await _userManager.CreateAsync(newUser, password);
        if (!createResult.Succeeded)
            return GetErrors(createResult.Errors);

        var roles = new List<string> { "Admin", "User" };
        var addToRolesResult = await _userManager.AddToRolesAsync(newUser, roles);
        if (!addToRolesResult.Succeeded)
            return GetErrors(addToRolesResult.Errors);

        return Result.Created;
    }

    public async Task<ErrorOr<(User, AccessToken, RefreshToken, IList<string> roles)>> LoginAccount(string username,
        string password)
    {
        var existingUser = await _userManager.FindByNameAsync(username.Trim());
        if (existingUser == null)
            return Errors.User.NotFoundUsername;

        var passwordValid = await _userManager.CheckPasswordAsync(existingUser, password);
        if (!passwordValid)
            return Errors.User.IncorrectPassword;

        var userRoles = await _userManager.GetRolesAsync(existingUser);

        var accessToken = _tokenService.GenerateAccessToken(existingUser, userRoles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var userSession = new UserSession
        {
            RefreshToken = refreshToken.Token,
            RefreshTokenCreatedAt = refreshToken.CreatedAt,
            RefreshTokenExpiredAt = refreshToken.ExpiredAt,
            UserId = existingUser.Id,
            User = existingUser
        };

        await _context.UserSessions.AddAsync(userSession);
        await _context.SaveChangesAsync();

        return (existingUser, accessToken, refreshToken, userRoles);
    }

    public async Task<ErrorOr<(User, AccessToken, RefreshToken, IList<string> roles)>> RegisterAccount(string username,
        string email, string password)
    {
        var user = new User
        {
            UserName = username.Trim(),
            Email = email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserAddress = new UserAddress()
        };

        var userCreateResult = await _userManager.CreateAsync(user, password);

        if (!userCreateResult.Succeeded)
            return GetErrors(userCreateResult.Errors);

        IList<string> roles = new List<string> { "User" };
        await _userManager.AddToRolesAsync(user, roles);

        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var userLogin = new UserSession
        {
            RefreshToken = refreshToken.Token,
            RefreshTokenCreatedAt = refreshToken.CreatedAt,
            RefreshTokenExpiredAt = refreshToken.ExpiredAt,
            UserId = user.Id,
            User = user
        };

        await _context.UserSessions.AddAsync(userLogin);
        await _context.SaveChangesAsync();

        return (user, accessToken, refreshToken, roles);
    }

    public async Task<ErrorOr<(AccessToken, RefreshToken)>> RenewsTheTokens(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Errors.User.InvalidRefreshToken;

        var userSession = await _context.UserSessions
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.RefreshToken == refreshToken);

        if (userSession == null)
            return Errors.User.InvalidRefreshToken;

        if (userSession.RefreshTokenExpiredAt < DateTime.UtcNow)
        {
            _context.UserSessions.Remove(userSession);
            await _context.SaveChangesAsync();
            return Errors.User.ExpiredToken;
        }

        var newRefreshToken = _tokenService.GenerateRefreshToken();
        userSession.RefreshToken = newRefreshToken.Token;
        userSession.RefreshTokenCreatedAt = newRefreshToken.CreatedAt;
        userSession.RefreshTokenExpiredAt = newRefreshToken.ExpiredAt;

        _context.UserSessions.Update(userSession);
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(userSession.User);

        var newAccessToken = _tokenService.GenerateAccessToken(userSession.User, roles);

        return (newAccessToken, newRefreshToken);
    }

    public async Task<ErrorOr<Success>> LogoutAccount(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Errors.User.InvalidRefreshToken;

        var userSession = await _context.UserSessions
            .FirstOrDefaultAsync(l => l.RefreshToken == refreshToken);

        if (userSession != null)
        {
            _context.UserSessions.Remove(userSession);
            await _context.SaveChangesAsync();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> VerifyEmail(int userId, string token)
    {
        if (userId == 0)
            return Error.Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Errors.User.NotFound;

        var confirmResult = await _userManager.ConfirmEmailAsync(user, token);
        if (!confirmResult.Succeeded)
            return GetErrors(confirmResult.Errors);

        return Result.Success;
    }

    public async Task<ErrorOr<Updated>> UpdateUserName(int userId, string newUserName)
    {
        if (userId == 0)
            return Error.Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return Errors.User.NotFound;

        user.UserName = newUserName.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return GetErrors(updateResult.Errors);

        return Result.Updated;
    }

    public async Task<ErrorOr<Deleted>> DeleteAccount(int userId, string password)
    {
        if (userId == 0) return Error.Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Errors.User.NotFound;

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
            return Errors.User.IncorrectPassword;

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
            return GetErrors(deleteResult.Errors);

        return Result.Deleted;
    }

    public async Task<ErrorOr<Updated>> UpdateAccountPassword(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Errors.User.NotFoundEmail;

        var goodToken = HttpUtility.UrlDecode(token);

        var resetResult = await _userManager.ResetPasswordAsync(user, goodToken, newPassword);
        if (!resetResult.Succeeded)
            return GetErrors(resetResult.Errors);

        return Result.Updated;
    }

    private List<Error> GetErrors(IEnumerable<IdentityError> identityErrors)
    {
        return identityErrors
            .Select(identityError => Error.Validation(identityError.Code, identityError.Description)).ToList();
    }
}