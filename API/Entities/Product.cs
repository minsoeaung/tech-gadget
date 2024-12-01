using System.ComponentModel.DataAnnotations;

namespace API.Entities;

public class Product
{
    public int Id { get; set; }
    [Required] public string Name { get; set; }
    [MaxLength(100)] public string Sku { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public int QuantityInStock { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; }

    public int BrandId { get; set; }
    public Brand Brand { get; set; }

    [Range(0.0, 5.0)] public double AverageRating { get; private set; }
    [Range(0, int.MaxValue)] public int NumOfRatings { get; private set; }

    public void AddNewRating(int rating)
    {
        if (rating is <= 0 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5 (inclusive)");

        AverageRating = ((AverageRating * NumOfRatings) + rating) / ++NumOfRatings;
    }

    public void RemoveRating(int rating)
    {
        if (rating is <= 0 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5 (inclusive)");

        switch (NumOfRatings)
        {
            case 1:
                AverageRating = 0;
                NumOfRatings = 0;
                return;
            case > 0:
                AverageRating = ((AverageRating * NumOfRatings) - rating) / --NumOfRatings;
                break;
        }
    }

    public void UpdateRating(int oldRating, int newRating)
    {
        if ((oldRating is <= 0 or > 5) || (newRating is <= 0 or > 5))
            throw new InvalidOperationException("Rating must be between 1 and 5 (inclusive)");

        RemoveRating(oldRating);
        AddNewRating(newRating);
    }
}