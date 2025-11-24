using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Conversations.ValueObjects;
using DoorX.Domain.Tenants.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.Conversations.Events;

/// <summary>
/// Domain event raised when a new conversation is started
/// </summary>
public record ConversationStartedEvent(
    ConversationId ConversationId,
    WorkOrderId WorkOrderId,
    TenantId TenantId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
