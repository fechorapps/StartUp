using ErrorOr;

namespace DoorX.Domain.Properties.ValueObjects;

/// <summary>
/// Value Object representing a physical address
/// </summary>
public record Address
{
    public string Street { get; init; }
    public string? Unit { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string ZipCode { get; init; }
    public string Country { get; init; }

    private Address(string street, string? unit, string city, string state, string zipCode, string country)
    {
        Street = street;
        Unit = unit;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public static ErrorOr<Address> Create(string street, string? unit, string city, string state, string zipCode, string country = "USA")
    {
        if (string.IsNullOrWhiteSpace(street))
            return Error.Validation("Address.Street", "Street is required");

        if (string.IsNullOrWhiteSpace(city))
            return Error.Validation("Address.City", "City is required");

        if (string.IsNullOrWhiteSpace(state))
            return Error.Validation("Address.State", "State is required");

        if (string.IsNullOrWhiteSpace(zipCode))
            return Error.Validation("Address.ZipCode", "ZIP code is required");

        return new Address(street, unit, city, state, zipCode, country);
    }

    public string GetFullAddress()
    {
        var unitPart = string.IsNullOrWhiteSpace(Unit) ? "" : $" {Unit}";
        return $"{Street}{unitPart}, {City}, {State} {ZipCode}, {Country}";
    }

    public override string ToString() => GetFullAddress();
}
