using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.WorkOrders.Events;

/// <summary>
/// Domain event raised when a work order's status changes
/// </summary>
public record WorkOrderStatusChangedEvent(
    WorkOrderId WorkOrderId,
    WorkOrderStatus OldStatus,
    WorkOrderStatus NewStatus) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
