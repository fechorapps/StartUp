using ErrorOr;

namespace DoorX.Domain.Vendors.ValueObjects;

/// <summary>
/// Value Object representing a vendor's rating (0-5 stars)
/// </summary>
public record Rating
{
    public decimal Value { get; init; }
    public int TotalReviews { get; init; }

    private Rating(decimal value, int totalReviews)
    {
        Value = value;
        TotalReviews = totalReviews;
    }

    public static ErrorOr<Rating> Create(decimal value, int totalReviews = 0)
    {
        if (value < 0 || value > 5)
            return Error.Validation("Rating.Value", "Rating must be between 0 and 5");

        if (totalReviews < 0)
            return Error.Validation("Rating.TotalReviews", "Total reviews cannot be negative");

        return new Rating(value, totalReviews);
    }

    public static Rating Unrated => new(0, 0);

    public bool IsUnrated() => TotalReviews == 0;

    public override string ToString() => IsUnrated() ? "No rating" : $"{Value:F1} ({TotalReviews} reviews)";
}
