using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Tenants.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.WorkOrders.Events;

/// <summary>
/// Domain event raised when a work order is closed (tenant confirmed satisfaction)
/// </summary>
public record WorkOrderClosedEvent(
    WorkOrderId WorkOrderId,
    TenantId TenantId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
