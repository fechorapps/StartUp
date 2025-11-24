using DoorX.Domain.Common;
using DoorX.Domain.Properties.ValueObjects;
using ErrorOr;

namespace DoorX.Domain.Properties.Entities;

/// <summary>
/// Aggregate Root representing a rental property in the system
/// </summary>
/// <remarks>
/// A Property represents a physical location where tenants live and where maintenance work orders occur.
/// It contains basic property information needed for maintenance coordination.
/// </remarks>
public sealed class Property : AggregateRoot<PropertyId>
{
    private Property(
        PropertyId id,
        string name,
        Address address,
        PropertyType propertyType) : base(id)
    {
        Name = name;
        Address = address;
        PropertyType = propertyType;
    }

    /// <summary>
    /// Property name or identifier (e.g., "Building A - Unit 101")
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Physical address of the property
    /// </summary>
    public Address Address { get; private set; }

    /// <summary>
    /// Type of property (Apartment, House, Condo, etc.)
    /// </summary>
    public PropertyType PropertyType { get; private set; }

    /// <summary>
    /// Optional external reference ID from PMS (Property Management System)
    /// </summary>
    public string? ExternalPmsId { get; private set; }

    /// <summary>
    /// Whether the property is currently active for maintenance requests
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Factory method to create a new Property
    /// </summary>
    public static ErrorOr<Property> Create(
        string name,
        Address address,
        PropertyType propertyType,
        string? externalPmsId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Property.Name", "Property name is required");

        var property = new Property(
            PropertyId.CreateUnique(),
            name,
            address,
            propertyType)
        {
            ExternalPmsId = externalPmsId
        };

        return property;
    }

    /// <summary>
    /// Updates the property information
    /// </summary>
    public ErrorOr<Success> Update(string name, Address address, PropertyType propertyType)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Property.Name", "Property name is required");

        Name = name;
        Address = address;
        PropertyType = propertyType;

        return Result.Success;
    }

    /// <summary>
    /// Deactivates the property (no longer accepting work orders)
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reactivates the property
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Sets the external PMS reference ID
    /// </summary>
    public void SetExternalPmsId(string externalPmsId)
    {
        ExternalPmsId = externalPmsId;
    }

#pragma warning disable CS8618
    private Property() : base() { }
#pragma warning restore CS8618
}

/// <summary>
/// Value Object representing property type
/// </summary>
public record PropertyType
{
    private static readonly HashSet<string> ValidTypes = new()
    {
        "Apartment", "House", "Condo", "Townhouse", "CommercialBuilding", "Other"
    };

    public string Value { get; init; }

    private PropertyType(string value)
    {
        Value = value;
    }

    public static ErrorOr<PropertyType> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("PropertyType.Value", "Property type is required");

        if (!ValidTypes.Contains(value))
            return Error.Validation("PropertyType.Value", $"Invalid property type. Must be one of: {string.Join(", ", ValidTypes)}");

        return new PropertyType(value);
    }

    public static PropertyType Apartment => new("Apartment");
    public static PropertyType House => new("House");
    public static PropertyType Condo => new("Condo");
    public static PropertyType Townhouse => new("Townhouse");
    public static PropertyType CommercialBuilding => new("CommercialBuilding");
    public static PropertyType Other => new("Other");

    public override string ToString() => Value;
}
