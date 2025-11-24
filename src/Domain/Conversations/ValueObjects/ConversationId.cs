namespace DoorX.Domain.Conversations.ValueObjects;

/// <summary>
/// Strongly-typed identifier for Conversation entity
/// </summary>
public record ConversationId(Guid Value)
{
    public static ConversationId CreateUnique() => new(Guid.NewGuid());

    public static ConversationId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
