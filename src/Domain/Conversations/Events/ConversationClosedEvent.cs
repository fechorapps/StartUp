using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Conversations.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.Conversations.Events;

/// <summary>
/// Domain event raised when a conversation is closed
/// </summary>
public record ConversationClosedEvent(
    ConversationId ConversationId,
    WorkOrderId WorkOrderId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
