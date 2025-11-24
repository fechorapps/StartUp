using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.WorkOrders.Events;

/// <summary>
/// Domain event raised when work begins on a work order
/// </summary>
public record WorkStartedEvent(
    WorkOrderId WorkOrderId,
    VendorId VendorId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
