using DoorX.Domain.Common;

namespace DoorX.Domain.WorkOrders.ValueObjects;

/// <summary>
/// Smart Enum representing the lifecycle status of a work order
/// </summary>
/// <remarks>
/// Persistence Strategy: MEMORY ONLY (Smart Enum)
/// - Fixed workflow defined by business process
/// - Stored as INT in database for query performance
/// - State transitions are enforced in domain logic
///
/// Workflow: Open → Categorized → VendorSearch → Bidding → Scheduled → InProgress → Completed → Closed
///           └────────────────────────────────────────────────────────────────────────────────────→ Cancelled
///
/// See docs/CATALOGS_PERSISTENCE.md for details
/// </remarks>
public sealed class WorkOrderStatus : SmartEnum<WorkOrderStatus>
{
    // Predefined statuses (ID, Name, Description)
    public static readonly WorkOrderStatus Open = new(1, "Open", "Tenant reported problem");
    public static readonly WorkOrderStatus Categorized = new(2, "Categorized", "AI identified problem type");
    public static readonly WorkOrderStatus VendorSearch = new(3, "VendorSearch", "Searching for available vendors");
    public static readonly WorkOrderStatus Bidding = new(4, "Bidding", "Waiting for vendor quotes");
    public static readonly WorkOrderStatus Scheduled = new(5, "Scheduled", "Vendor assigned, date confirmed");
    public static readonly WorkOrderStatus InProgress = new(6, "InProgress", "Vendor working");
    public static readonly WorkOrderStatus Completed = new(7, "Completed", "Work finished");
    public static readonly WorkOrderStatus Closed = new(8, "Closed", "Tenant confirmed satisfaction");
    public static readonly WorkOrderStatus Cancelled = new(9, "Cancelled", "Work order cancelled");

    /// <summary>
    /// Description of this status
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Valid next states from this status
    /// </summary>
    private readonly HashSet<WorkOrderStatus> _validTransitions;

    private WorkOrderStatus(int id, string name, string description) : base(id, name)
    {
        Description = description;
        _validTransitions = new HashSet<WorkOrderStatus>();
    }

    /// <summary>
    /// Initialize valid transitions (called once after all statuses are created)
    /// </summary>
    static WorkOrderStatus()
    {
        // Define valid transitions
        Open._validTransitions.UnionWith(new[] { Categorized, Cancelled });
        Categorized._validTransitions.UnionWith(new[] { VendorSearch, Cancelled });
        VendorSearch._validTransitions.UnionWith(new[] { Bidding, Cancelled });
        Bidding._validTransitions.UnionWith(new[] { Scheduled, VendorSearch, Cancelled });
        Scheduled._validTransitions.UnionWith(new[] { InProgress, Cancelled });
        InProgress._validTransitions.UnionWith(new[] { Completed, Cancelled });
        Completed._validTransitions.UnionWith(new[] { Closed });
        // Closed and Cancelled have no valid transitions (final states)
    }

    /// <summary>
    /// Checks if transition to new status is valid
    /// </summary>
    public bool CanTransitionTo(WorkOrderStatus newStatus)
    {
        return _validTransitions.Contains(newStatus);
    }

    /// <summary>
    /// Gets all valid next states from current status
    /// </summary>
    public IEnumerable<WorkOrderStatus> GetValidTransitions()
    {
        return _validTransitions;
    }

    /// <summary>
    /// Checks if this is a final state (no further transitions)
    /// </summary>
    public bool IsFinalState() => this == Closed || this == Cancelled;

    /// <summary>
    /// Checks if this is an active state (not final)
    /// </summary>
    public bool IsActive() => !IsFinalState();

    /// <summary>
    /// Checks if this status allows vendor assignment
    /// </summary>
    public bool AllowsVendorAssignment() => this == Bidding;

    /// <summary>
    /// Checks if this status allows modifications
    /// </summary>
    public bool AllowsModifications() => IsActive();

    /// <summary>
    /// Gets the color code for UI display
    /// </summary>
    public string GetColorCode()
    {
        return this switch
        {
            var s when s == Open => "#9CA3AF",          // Gray
            var s when s == Categorized => "#6366F1",    // Indigo
            var s when s == VendorSearch => "#8B5CF6",  // Purple
            var s when s == Bidding => "#3B82F6",       // Blue
            var s when s == Scheduled => "#F59E0B",     // Orange
            var s when s == InProgress => "#FBBF24",    // Yellow
            var s when s == Completed => "#10B981",     // Green
            var s when s == Closed => "#059669",        // Dark Green
            var s when s == Cancelled => "#DC2626",     // Red
            _ => "#6B7280"                              // Gray
        };
    }

    /// <summary>
    /// Gets the progress percentage for UI display
    /// </summary>
    public int GetProgressPercentage()
    {
        return this switch
        {
            var s when s == Open => 0,
            var s when s == Categorized => 10,
            var s when s == VendorSearch => 25,
            var s when s == Bidding => 40,
            var s when s == Scheduled => 50,
            var s when s == InProgress => 75,
            var s when s == Completed => 90,
            var s when s == Closed => 100,
            var s when s == Cancelled => 0,
            _ => 0
        };
    }
}
