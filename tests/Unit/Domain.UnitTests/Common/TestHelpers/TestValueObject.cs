using DoorX.Domain.Common;

namespace Domain.UnitTests.Common.TestHelpers;

/// <summary>
/// Concrete implementation of ValueObject for testing purposes.
/// Represents an address with street, city, and zip code.
/// </summary>
internal sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string ZipCode { get; }

    public Address(string street, string city, string zipCode)
    {
        Street = street;
        City = city;
        ZipCode = zipCode;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return ZipCode;
    }
}

/// <summary>
/// Another value object for testing with nullable components.
/// </summary>
internal sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    public string? Description { get; }

    public Money(decimal amount, string currency, string? description = null)
    {
        Amount = amount;
        Currency = currency;
        Description = description;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
        yield return Description;
    }
}

/// <summary>
/// Value object with no components for testing edge cases.
/// </summary>
internal sealed class EmptyValueObject : ValueObject
{
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield break;
    }
}
