using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.WorkOrders.Events;

/// <summary>
/// Domain event raised when a vendor submits a bid for a work order
/// </summary>
public record VendorBidReceivedEvent(
    WorkOrderId WorkOrderId,
    VendorId VendorId,
    Money EstimatedCost) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
