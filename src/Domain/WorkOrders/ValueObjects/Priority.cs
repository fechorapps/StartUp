using DoorX.Domain.Common;

namespace DoorX.Domain.WorkOrders.ValueObjects;

/// <summary>
/// Smart Enum representing the urgency level of a work order
/// </summary>
/// <remarks>
/// Persistence Strategy: MEMORY ONLY (Smart Enum)
/// - Fixed priority levels defined by business rules
/// - Stored as INT in database for query performance
/// - Response times are part of business logic
///
/// See docs/CATALOGS_PERSISTENCE.md for details
/// </remarks>
public sealed class Priority : SmartEnum<Priority>
{
    // Predefined priority levels (ID, Name, Expected Response Hours)
    public static readonly Priority Emergency = new(1, "Emergency", 24, "Safety issues, no water/electricity, severe damage");
    public static readonly Priority High = new(2, "High", 48, "Major problems affecting habitability");
    public static readonly Priority Normal = new(3, "Normal", 120, "Standard repairs and maintenance");
    public static readonly Priority Low = new(4, "Low", 168, "Cosmetic improvements and minor issues");

    /// <summary>
    /// Expected response time in hours
    /// </summary>
    public int ExpectedResponseHours { get; }

    /// <summary>
    /// Description of when to use this priority
    /// </summary>
    public string Description { get; }

    private Priority(int id, string name, int expectedResponseHours, string description)
        : base(id, name)
    {
        ExpectedResponseHours = expectedResponseHours;
        Description = description;
    }

    /// <summary>
    /// Checks if this is an emergency priority
    /// </summary>
    public bool IsEmergency() => this == Emergency;

    /// <summary>
    /// Checks if this requires urgent attention (Emergency or High)
    /// </summary>
    public bool IsUrgent() => this == Emergency || this == High;

    /// <summary>
    /// Gets the expected response time as a TimeSpan
    /// </summary>
    public TimeSpan GetExpectedResponseTime() => TimeSpan.FromHours(ExpectedResponseHours);

    /// <summary>
    /// Gets the expected completion date based on creation time
    /// </summary>
    public DateTime GetExpectedCompletionDate(DateTime createdAt)
    {
        return createdAt.AddHours(ExpectedResponseHours);
    }

    /// <summary>
    /// Checks if a work order is overdue based on priority
    /// </summary>
    public bool IsOverdue(DateTime createdAt, DateTime now)
    {
        var expectedCompletion = GetExpectedCompletionDate(createdAt);
        return now > expectedCompletion;
    }

    /// <summary>
    /// Gets the color code for UI display
    /// </summary>
    public string GetColorCode()
    {
        return this switch
        {
            var p when p == Emergency => "#DC2626",  // Red
            var p when p == High => "#F59E0B",       // Orange
            var p when p == Normal => "#3B82F6",     // Blue
            var p when p == Low => "#10B981",        // Green
            _ => "#6B7280"                           // Gray
        };
    }
}
