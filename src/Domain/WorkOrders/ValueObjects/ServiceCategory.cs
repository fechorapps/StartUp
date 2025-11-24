using DoorX.Domain.Common;

namespace DoorX.Domain.WorkOrders.ValueObjects;

/// <summary>
/// Smart Enum representing the type of maintenance service needed
/// </summary>
/// <remarks>
/// Persistence Strategy: MEMORY ONLY (Smart Enum)
/// - Fixed catalog defined by business
/// - Stored as INT in database for performance
/// - Changes require code deployment
///
/// See docs/CATALOGS_PERSISTENCE.md for details
/// </remarks>
public sealed class ServiceCategory : SmartEnum<ServiceCategory>
{
    // Predefined service categories (ID, Name)
    public static readonly ServiceCategory Plumbing = new(1, "Plumbing", "Repairs and maintenance of water systems, pipes, faucets, and fixtures");
    public static readonly ServiceCategory Electrical = new(2, "Electrical", "Electrical repairs, wiring, outlets, and lighting");
    public static readonly ServiceCategory HVAC = new(3, "HVAC", "Heating, ventilation, and air conditioning systems");
    public static readonly ServiceCategory Appliance = new(4, "Appliance", "Repair and maintenance of household appliances");
    public static readonly ServiceCategory PestControl = new(5, "PestControl", "Pest inspection and extermination services");
    public static readonly ServiceCategory Cleaning = new(6, "Cleaning", "Professional cleaning services");
    public static readonly ServiceCategory GeneralMaintenance = new(7, "GeneralMaintenance", "General repairs and maintenance work");

    /// <summary>
    /// Description of the service category
    /// </summary>
    public string Description { get; }

    private ServiceCategory(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    /// <summary>
    /// Checks if this category requires specialized certification
    /// </summary>
    public bool RequiresCertification()
    {
        return this == Electrical || this == HVAC || this == PestControl;
    }

    /// <summary>
    /// Gets the typical response time priority for this category
    /// </summary>
    public string GetTypicalPriority()
    {
        return this switch
        {
            var c when c == Electrical => "High (potential safety issue)",
            var c when c == HVAC => "High (comfort issue)",
            var c when c == Plumbing => "High (water damage risk)",
            var c when c == PestControl => "Normal",
            var c when c == Appliance => "Normal",
            var c when c == Cleaning => "Low",
            var c when c == GeneralMaintenance => "Normal",
            _ => "Normal"
        };
    }
}
