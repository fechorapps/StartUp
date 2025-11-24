namespace DoorX.Domain.Properties.ValueObjects;

/// <summary>
/// Strongly-typed identifier for Property entity
/// </summary>
public record PropertyId(Guid Value)
{
    public static PropertyId CreateUnique() => new(Guid.NewGuid());

    public static PropertyId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
