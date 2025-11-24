using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.WorkOrders.Events;

/// <summary>
/// Domain event raised when a work order's priority changes
/// </summary>
public record WorkOrderPriorityChangedEvent(
    WorkOrderId WorkOrderId,
    Priority OldPriority,
    Priority NewPriority) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
