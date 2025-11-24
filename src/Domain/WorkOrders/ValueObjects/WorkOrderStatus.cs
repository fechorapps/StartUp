using ErrorOr;

namespace DoorX.Domain.WorkOrders.ValueObjects;

/// <summary>
/// Value Object representing the lifecycle status of a work order
/// </summary>
public record WorkOrderStatus
{
    private static readonly Dictionary<string, List<string>> ValidTransitions = new()
    {
        ["Open"] = new() { "Categorized", "Cancelled" },
        ["Categorized"] = new() { "VendorSearch", "Cancelled" },
        ["VendorSearch"] = new() { "Bidding", "Cancelled" },
        ["Bidding"] = new() { "Scheduled", "VendorSearch", "Cancelled" },
        ["Scheduled"] = new() { "InProgress", "Cancelled" },
        ["InProgress"] = new() { "Completed", "Cancelled" },
        ["Completed"] = new() { "Closed" },
        ["Closed"] = new() { },
        ["Cancelled"] = new() { }
    };

    public string Value { get; init; }

    private WorkOrderStatus(string value)
    {
        Value = value;
    }

    public static ErrorOr<WorkOrderStatus> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("WorkOrderStatus.Value", "Status cannot be empty");

        if (!ValidTransitions.ContainsKey(value))
            return Error.Validation("WorkOrderStatus.Value", $"Invalid status: {value}");

        return new WorkOrderStatus(value);
    }

    // Predefined statuses
    public static WorkOrderStatus Open => new("Open");                    // Tenant reported problem
    public static WorkOrderStatus Categorized => new("Categorized");      // AI identified problem type
    public static WorkOrderStatus VendorSearch => new("VendorSearch");    // Searching for available vendors
    public static WorkOrderStatus Bidding => new("Bidding");              // Waiting for vendor quotes
    public static WorkOrderStatus Scheduled => new("Scheduled");          // Vendor assigned, date confirmed
    public static WorkOrderStatus InProgress => new("InProgress");        // Vendor working
    public static WorkOrderStatus Completed => new("Completed");          // Work finished
    public static WorkOrderStatus Closed => new("Closed");                // Tenant confirmed satisfaction
    public static WorkOrderStatus Cancelled => new("Cancelled");          // Work order cancelled

    public bool CanTransitionTo(WorkOrderStatus newStatus)
    {
        return ValidTransitions[Value].Contains(newStatus.Value);
    }

    public bool IsFinalState() => Value is "Closed" or "Cancelled";

    public bool IsActive() => !IsFinalState();

    public override string ToString() => Value;
}
