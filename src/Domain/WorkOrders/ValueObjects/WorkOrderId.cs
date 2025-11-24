namespace DoorX.Domain.WorkOrders.ValueObjects;

/// <summary>
/// Strongly-typed identifier for WorkOrder entity
/// </summary>
public record WorkOrderId(Guid Value)
{
    public static WorkOrderId CreateUnique() => new(Guid.NewGuid());

    public static WorkOrderId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
