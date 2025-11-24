using DoorX.Domain.Common;

namespace DoorX.Domain.Properties.ValueObjects;

/// <summary>
/// Smart Enum representing property type
/// </summary>
/// <remarks>
/// Persistence Strategy: MEMORY ONLY (Smart Enum)
/// - Standard property types in real estate
/// - Stored as VARCHAR(50) in database for clarity
/// - Rarely changes
///
/// See docs/CATALOGS_PERSISTENCE.md for details
/// </remarks>
public sealed class PropertyType : SmartEnum<PropertyType>
{
    // Predefined property types (ID, Name, Description)
    public static readonly PropertyType Apartment = new(1, "Apartment", "Multi-unit residential building", "🏢");
    public static readonly PropertyType House = new(2, "House", "Single-family detached home", "🏠");
    public static readonly PropertyType Condo = new(3, "Condo", "Individually owned unit in a multi-unit building", "🏘️");
    public static readonly PropertyType Townhouse = new(4, "Townhouse", "Multi-floor home sharing walls with adjacent units", "🏘️");
    public static readonly PropertyType CommercialBuilding = new(5, "CommercialBuilding", "Commercial property for business use", "🏢");
    public static readonly PropertyType Other = new(6, "Other", "Other type of property", "🏗️");

    /// <summary>
    /// Description of the property type
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Icon emoji for UI display
    /// </summary>
    public string Icon { get; }

    private PropertyType(int id, string name, string description, string icon) : base(id, name)
    {
        Description = description;
        Icon = icon;
    }

    /// <summary>
    /// Checks if this is a residential property type
    /// </summary>
    public bool IsResidential()
    {
        return this == Apartment || this == House || this == Condo || this == Townhouse;
    }

    /// <summary>
    /// Checks if this is a commercial property type
    /// </summary>
    public bool IsCommercial()
    {
        return this == CommercialBuilding;
    }

    /// <summary>
    /// Checks if this property type typically has multiple units
    /// </summary>
    public bool IsMultiUnit()
    {
        return this == Apartment || this == Condo || this == Townhouse;
    }

    /// <summary>
    /// Gets typical maintenance categories for this property type
    /// </summary>
    public string[] GetTypicalMaintenanceCategories()
    {
        if (IsCommercial())
        {
            return new[] { "HVAC", "Electrical", "Plumbing", "GeneralMaintenance", "Cleaning" };
        }

        return new[] { "HVAC", "Electrical", "Plumbing", "Appliance", "GeneralMaintenance" };
    }
}
