namespace DoorX.Domain.WorkOrders.ValueObjects;

/// <summary>
/// Strongly-typed identifier for VendorBid entity
/// </summary>
public record VendorBidId(Guid Value)
{
    public static VendorBidId CreateUnique() => new(Guid.NewGuid());

    public static VendorBidId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
