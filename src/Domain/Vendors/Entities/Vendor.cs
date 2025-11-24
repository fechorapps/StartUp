using DoorX.Domain.Common;
using DoorX.Domain.Common.ValueObjects;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;
using ErrorOr;

namespace DoorX.Domain.Vendors.Entities;

/// <summary>
/// Aggregate Root representing a service vendor/contractor
/// </summary>
/// <remarks>
/// A Vendor is a service provider who performs maintenance work.
/// Vendors have service categories they offer and service areas where they operate.
/// </remarks>
public sealed class Vendor : AggregateRoot<VendorId>
{
    private readonly List<ServiceCategory> _serviceCategories = new();
    private readonly List<ServiceArea> _serviceAreas = new();

    private Vendor(
        VendorId id,
        string companyName,
        ContactInfo contactInfo,
        Rating rating) : base(id)
    {
        CompanyName = companyName;
        ContactInfo = contactInfo;
        Rating = rating;
    }

    /// <summary>
    /// Company or individual vendor name
    /// </summary>
    public string CompanyName { get; private set; }

    /// <summary>
    /// Contact information (email and phone)
    /// </summary>
    public ContactInfo ContactInfo { get; private set; }

    /// <summary>
    /// Service categories the vendor offers (read-only collection)
    /// </summary>
    public IReadOnlyCollection<ServiceCategory> ServiceCategories => _serviceCategories.AsReadOnly();

    /// <summary>
    /// Service areas (ZIP codes) where the vendor operates (read-only collection)
    /// </summary>
    public IReadOnlyCollection<ServiceArea> ServiceAreas => _serviceAreas.AsReadOnly();

    /// <summary>
    /// Vendor's rating based on completed work
    /// </summary>
    public Rating Rating { get; private set; }

    /// <summary>
    /// Whether the vendor is currently available for new work
    /// </summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>
    /// Whether the vendor account is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Optional external reference ID from PMS
    /// </summary>
    public string? ExternalPmsId { get; private set; }

    /// <summary>
    /// Factory method to create a new Vendor
    /// </summary>
    public static ErrorOr<Vendor> Create(
        string companyName,
        ContactInfo contactInfo,
        string? externalPmsId = null)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return Error.Validation("Vendor.CompanyName", "Company name is required");

        var vendor = new Vendor(
            VendorId.CreateUnique(),
            companyName,
            contactInfo,
            Rating.Unrated)
        {
            ExternalPmsId = externalPmsId
        };

        return vendor;
    }

    /// <summary>
    /// Adds a service category the vendor offers
    /// </summary>
    public ErrorOr<Success> AddServiceCategory(ServiceCategory category)
    {
        if (_serviceCategories.Contains(category))
            return Error.Conflict("Vendor.ServiceCategory", "Service category already added");

        _serviceCategories.Add(category);
        return Result.Success;
    }

    /// <summary>
    /// Removes a service category
    /// </summary>
    public ErrorOr<Success> RemoveServiceCategory(ServiceCategory category)
    {
        if (!_serviceCategories.Contains(category))
            return Error.NotFound("Vendor.ServiceCategory", "Service category not found");

        _serviceCategories.Remove(category);
        return Result.Success;
    }

    /// <summary>
    /// Adds a service area (ZIP code) where the vendor operates
    /// </summary>
    public ErrorOr<Success> AddServiceArea(ServiceArea area)
    {
        if (_serviceAreas.Any(a => a.ZipCode == area.ZipCode))
            return Error.Conflict("Vendor.ServiceArea", "Service area already added");

        _serviceAreas.Add(area);
        return Result.Success;
    }

    /// <summary>
    /// Removes a service area
    /// </summary>
    public ErrorOr<Success> RemoveServiceArea(ServiceArea area)
    {
        var existingArea = _serviceAreas.FirstOrDefault(a => a.ZipCode == area.ZipCode);
        if (existingArea is null)
            return Error.NotFound("Vendor.ServiceArea", "Service area not found");

        _serviceAreas.Remove(existingArea);
        return Result.Success;
    }

    /// <summary>
    /// Checks if vendor can service a specific category and location
    /// </summary>
    public bool CanService(ServiceCategory category, string zipCode)
    {
        if (!IsActive || !IsAvailable)
            return false;

        var hasCategory = _serviceCategories.Contains(category);
        var hasArea = _serviceAreas.Any(a => a.ZipCode == zipCode);

        return hasCategory && hasArea;
    }

    /// <summary>
    /// Updates vendor contact information
    /// </summary>
    public ErrorOr<Success> UpdateContactInfo(ContactInfo contactInfo)
    {
        ContactInfo = contactInfo;
        return Result.Success;
    }

    /// <summary>
    /// Updates vendor rating
    /// </summary>
    public ErrorOr<Success> UpdateRating(Rating rating)
    {
        Rating = rating;
        return Result.Success;
    }

    /// <summary>
    /// Sets vendor availability
    /// </summary>
    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }

    /// <summary>
    /// Deactivates the vendor account
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        IsAvailable = false;
    }

    /// <summary>
    /// Reactivates the vendor account
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
    private Vendor() : base() { }
#pragma warning restore CS8618
}
