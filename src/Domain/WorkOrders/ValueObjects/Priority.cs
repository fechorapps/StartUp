using ErrorOr;

namespace DoorX.Domain.WorkOrders.ValueObjects;

/// <summary>
/// Value Object representing the urgency level of a work order
/// </summary>
public record Priority
{
    public string Value { get; init; }
    public int ExpectedResponseHours { get; init; }

    private Priority(string value, int expectedResponseHours)
    {
        Value = value;
        ExpectedResponseHours = expectedResponseHours;
    }

    public static ErrorOr<Priority> Create(string value)
    {
        return value.ToLower() switch
        {
            "emergency" => Emergency,
            "high" => High,
            "normal" => Normal,
            "low" => Low,
            _ => Error.Validation("Priority.Value", "Invalid priority. Must be: Emergency, High, Normal, or Low")
        };
    }

    // Predefined priority levels
    public static Priority Emergency => new("Emergency", 24);      // < 24 hours - No water, no electricity, safety issues
    public static Priority High => new("High", 48);                // 1-2 days - Major problems
    public static Priority Normal => new("Normal", 120);           // 3-5 days - Standard repairs
    public static Priority Low => new("Low", 168);                 // 5+ days - Cosmetic improvements

    public bool IsEmergency() => Value == "Emergency";

    public override string ToString() => Value;
}
