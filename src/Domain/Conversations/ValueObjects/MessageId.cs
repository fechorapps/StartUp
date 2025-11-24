namespace DoorX.Domain.Conversations.ValueObjects;

/// <summary>
/// Strongly-typed identifier for Message entity
/// </summary>
public record MessageId(Guid Value)
{
    public static MessageId CreateUnique() => new(Guid.NewGuid());

    public static MessageId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
