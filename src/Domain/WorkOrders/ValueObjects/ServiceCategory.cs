using ErrorOr;

namespace DoorX.Domain.WorkOrders.ValueObjects;

/// <summary>
/// Value Object representing the type of maintenance service needed
/// </summary>
public record ServiceCategory
{
    private static readonly HashSet<string> ValidCategories = new()
    {
        "Plumbing",
        "Electrical",
        "HVAC",
        "Appliance",
        "PestControl",
        "Cleaning",
        "GeneralMaintenance"
    };

    public string Value { get; init; }

    private ServiceCategory(string value)
    {
        Value = value;
    }

    public static ErrorOr<ServiceCategory> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("ServiceCategory.Value", "Service category cannot be empty");

        if (!ValidCategories.Contains(value))
            return Error.Validation("ServiceCategory.Value", $"Invalid service category. Must be one of: {string.Join(", ", ValidCategories)}");

        return new ServiceCategory(value);
    }

    // Predefined categories
    public static ServiceCategory Plumbing => new("Plumbing");
    public static ServiceCategory Electrical => new("Electrical");
    public static ServiceCategory HVAC => new("HVAC");
    public static ServiceCategory Appliance => new("Appliance");
    public static ServiceCategory PestControl => new("PestControl");
    public static ServiceCategory Cleaning => new("Cleaning");
    public static ServiceCategory GeneralMaintenance => new("GeneralMaintenance");

    public override string ToString() => Value;
}
