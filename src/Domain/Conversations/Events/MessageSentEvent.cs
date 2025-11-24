using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Conversations.ValueObjects;

namespace DoorX.Domain.Conversations.Events;

/// <summary>
/// Domain event raised when a message is sent in a conversation
/// </summary>
public record MessageSentEvent(
    ConversationId ConversationId,
    MessageId MessageId,
    SenderType SenderType,
    Channel Channel) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
