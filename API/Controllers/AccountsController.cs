using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Web;
using API.ApiErrors;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;
using API.Services;
using ErrorOr;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/[controller]")]
public class AccountsController : BaseApiController
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly IMailService _mailService;

    private readonly IAccountService _accountService;

    public AccountsController(UserManager<User> userManager,
        IMapper mapper,
        IMailService mailService,
        IAccountService accountService)
    {
        _userManager = userManager;
        _mapper = mapper;
        _mailService = mailService;
        _accountService = accountService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<AccountDetails>> Me()
    {
        var errorOrUser = await _accountService.GetAccount(GetUserId());

        return errorOrUser.Match(
            user => Ok(_mapper.Map<AccountDetails>((user.Item1, user.Item2))),
            Problem
        );
    }

    [HttpPost("profile-picture")]
    public async Task<ActionResult> UpdateProfilePicture([Required] [FromForm] IFormFile picture)
    {
        var errorOrUpdated = await _accountService.UpdateProfilePicture(GetUserId(), picture);

        return errorOrUpdated.Match(_ => NoContent(), Problem);
    }

    [Authorize(Roles = "Super")]
    [HttpPost("register-admin")]
    public async Task<ActionResult> RegisterAdmin(RegisterDto dto)
    {
        var errorOrCreated = await _accountService.CreateAdminAccount(dto.UserName, dto.Email, dto.Password);

        return errorOrCreated.Match(_ => NoContent(), Problem);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(LoginDto dto)
    {
        var errorOrUser = await _accountService.LoginAccount(dto.UserName, dto.Password);
        if (errorOrUser.IsError)
            return Problem(errorOrUser.Errors);

        var (user, accessToken, refreshToken, roles) = errorOrUser.Value;

        SetRefreshTokenInCookie(refreshToken);

        return new AuthResult
        {
            AccessToken = accessToken.Token,
            AccountDetails = _mapper.Map<AccountDetails>((user, roles))
        };
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(RegisterDto dto)
    {
        var errorOrUser = await _accountService.RegisterAccount(dto.UserName, dto.Email, dto.Password);
        if (errorOrUser.IsError)
            return Problem(errorOrUser.Errors);

        var (user, accessToken, refreshToken, roles) = errorOrUser.Value;

        SetRefreshTokenInCookie(refreshToken);

        return new AuthResult
        {
            AccessToken = accessToken.Token,
            AccountDetails = _mapper.Map<AccountDetails>((user, roles))
        };
    }

    [AllowAnonymous]
    [HttpGet("renew-tokens")]
    public async Task<ActionResult<AccessToken>> RenewTokens()
    {
        var errorOrRenewedToken = await _accountService.RenewsTheTokens(Request.Cookies["refreshToken"] ?? "");

        if (errorOrRenewedToken.IsError)
            return Problem(errorOrRenewedToken.Errors);

        var (newAccessToken, newRefreshToken) = errorOrRenewedToken.Value;

        SetRefreshTokenInCookie(newRefreshToken);

        return newAccessToken;
    }

    [AllowAnonymous]
    [HttpGet("logout")]
    public async Task<ActionResult> Logout()
    {
        var errorOrLogout = await _accountService.LogoutAccount(Request.Cookies["refreshToken"] ?? "");

        if (errorOrLogout.IsError)
            return Problem(errorOrLogout.Errors);

        DeleteRefreshTokenInCookie();

        return Ok();
    }

    [AllowAnonymous]
    [HttpGet("verify-email")]
    public async Task<ActionResult> ConfirmEmail(string userId, string token)
    {
        int.TryParse(userId, out var validId);

        var errorOrVerify = await _accountService.VerifyEmail(validId, token);

        return errorOrVerify.Match(
            _ => Ok("Your email is verified.\nPlease refresh your page to see changes."),
            Problem
        );
    }

    [HttpPost("send-verification-mail")]
    public async Task<ActionResult> SendConfirmEmailLink()
    {
        var errorOrUser = await _accountService.GetAccount(GetUserId());
        if (errorOrUser.IsError)
            return Problem(errorOrUser.Errors);

        var (user, _) = errorOrUser.Value;

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var url = Request.Scheme +
                  "://" +
                  Request.Host +
                  Url.Action(
                      nameof(ConfirmEmail),
                      "Accounts",
                      new { userId = user.Id, token }
                  );

        await _mailService.SendMailAsync(user.Email ?? string.Empty, "Verify your email address",
            $"Hi, {user.UserName}. Please click the link below to verify your email address: <br/><br/> {url}");

        return Ok();
    }

    [HttpPost("username")]
    public async Task<ActionResult> UpdateUsername([StringLength(50, MinimumLength = 4,
            ErrorMessage = "Username must have a minimum length of 4 and a maximum length of 50.")]
        string username)
    {
        var errorOrUser = await _accountService.UpdateUserName(GetUserId(), username);
        return errorOrUser.Match(_ => Ok(), Problem);
    }

    [HttpDelete("{userId}")]
    public async Task<ActionResult> DeleteAccount(int userId, string password)
    {
        var errorOrDeleted = await _accountService.DeleteAccount(userId, password);
        return errorOrDeleted.Match(_ => Ok(), Problem);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var errorOrReset = await _accountService.UpdateAccountPassword(dto.Email, dto.Token, dto.NewPassword);
        return errorOrReset.Match(_ => Ok(), Problem);
    }

    [AllowAnonymous]
    [HttpPost("send-reset-password-mail")]
    public async Task<ActionResult> SendResetPasswordMail(string email, string clientUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Problem(new List<Error> { Errors.User.NotFoundEmail });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var url = $"{clientUrl}?token={HttpUtility.UrlEncode(token)}&email={email}";

        await _mailService.SendMailAsync(user.Email ?? string.Empty, "Password Reset Request",
            $"Hi, {user.UserName}. Please click the link below to reset your password: <br/><br/> <a href=\"{url}\" target=\"_blank\">[{clientUrl}]</a> <br/><br/> The link will expire in 24h. If the link has expired, you can initiate another password reset request.\n");
        return Ok();
    }

    private int GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userId, out var validId);
        return validId;
    }

    private void SetRefreshTokenInCookie(RefreshToken refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            Secure = true,
            HttpOnly = true,
            Expires = refreshToken.ExpiredAt,
            IsEssential = true,
            SameSite = SameSiteMode.None,
        };

        Response.Cookies.Append("refreshToken", refreshToken.Token, cookieOptions);
    }

    private void DeleteRefreshTokenInCookie()
    {
        var cookieOptions = new CookieOptions
        {
            Secure = true,
            HttpOnly = true,
            Expires = DateTime.Now.AddDays(-1),
            IsEssential = true,
            SameSite = SameSiteMode.None,
        };

        Response.Cookies.Append("refreshToken", "", cookieOptions);
    }
}