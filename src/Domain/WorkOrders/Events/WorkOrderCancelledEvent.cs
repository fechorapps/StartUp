using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.WorkOrders.Events;

/// <summary>
/// Domain event raised when a work order is cancelled
/// </summary>
public record WorkOrderCancelledEvent(
    WorkOrderId WorkOrderId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
