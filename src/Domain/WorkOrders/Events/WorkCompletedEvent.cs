using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.WorkOrders.Events;

/// <summary>
/// Domain event raised when work on a work order is completed
/// </summary>
public record WorkCompletedEvent(
    WorkOrderId WorkOrderId,
    VendorId VendorId,
    DateTime CompletedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
