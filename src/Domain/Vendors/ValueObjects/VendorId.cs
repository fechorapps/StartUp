namespace DoorX.Domain.Vendors.ValueObjects;

/// <summary>
/// Strongly-typed identifier for Vendor entity
/// </summary>
public record VendorId(Guid Value)
{
    public static VendorId CreateUnique() => new(Guid.NewGuid());

    public static VendorId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
