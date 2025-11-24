namespace DoorX.Domain.Tenants.ValueObjects;

/// <summary>
/// Strongly-typed identifier for Tenant entity
/// </summary>
public record TenantId(Guid Value)
{
    public static TenantId CreateUnique() => new(Guid.NewGuid());

    public static TenantId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
