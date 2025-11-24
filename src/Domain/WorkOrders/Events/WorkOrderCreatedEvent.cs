using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Properties.ValueObjects;
using DoorX.Domain.Tenants.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.WorkOrders.Events;

/// <summary>
/// Domain event raised when a new work order is created
/// </summary>
public record WorkOrderCreatedEvent(
    WorkOrderId WorkOrderId,
    TenantId TenantId,
    PropertyId PropertyId,
    ServiceCategory Category,
    Priority Priority) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
