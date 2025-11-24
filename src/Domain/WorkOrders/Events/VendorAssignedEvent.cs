using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.WorkOrders.Events;

/// <summary>
/// Domain event raised when a vendor is assigned to a work order
/// </summary>
public record VendorAssignedEvent(
    WorkOrderId WorkOrderId,
    VendorId VendorId,
    DateTime ScheduledFor) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
