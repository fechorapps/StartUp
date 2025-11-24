using ErrorOr;

namespace DoorX.Domain.Vendors.ValueObjects;

/// <summary>
/// Value Object representing a ZIP code service area
/// </summary>
public record ServiceArea
{
    public string ZipCode { get; init; }

    private ServiceArea(string zipCode)
    {
        ZipCode = zipCode;
    }

    public static ErrorOr<ServiceArea> Create(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            return Error.Validation("ServiceArea.ZipCode", "ZIP code is required");

        // Basic US ZIP code validation (5 or 9 digits)
        var cleaned = zipCode.Replace("-", "").Trim();
        if (cleaned.Length != 5 && cleaned.Length != 9)
            return Error.Validation("ServiceArea.ZipCode", "ZIP code must be 5 or 9 digits");

        if (!cleaned.All(char.IsDigit))
            return Error.Validation("ServiceArea.ZipCode", "ZIP code must contain only digits");

        return new ServiceArea(zipCode);
    }

    public override string ToString() => ZipCode;
}
