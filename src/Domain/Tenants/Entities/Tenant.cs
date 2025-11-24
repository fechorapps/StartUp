using DoorX.Domain.Common;
using DoorX.Domain.Common.ValueObjects;
using DoorX.Domain.Properties.ValueObjects;
using DoorX.Domain.Tenants.ValueObjects;
using ErrorOr;

namespace DoorX.Domain.Tenants.Entities;

/// <summary>
/// Aggregate Root representing a tenant (resident) who lives in a property
/// </summary>
/// <remarks>
/// A Tenant is a person who lives in a property and can report maintenance issues.
/// Tenants communicate through various channels (SMS, WhatsApp, Web) to report problems.
/// </remarks>
public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant(
        TenantId id,
        string fullName,
        ContactInfo contactInfo,
        PropertyId propertyId,
        Language preferredLanguage) : base(id)
    {
        FullName = fullName;
        ContactInfo = contactInfo;
        PropertyId = propertyId;
        PreferredLanguage = preferredLanguage;
    }

    /// <summary>
    /// Full name of the tenant
    /// </summary>
    public string FullName { get; private set; }

    /// <summary>
    /// Contact information (email and phone)
    /// </summary>
    public ContactInfo ContactInfo { get; private set; }

    /// <summary>
    /// Property where the tenant lives
    /// </summary>
    public PropertyId PropertyId { get; private set; }

    /// <summary>
    /// Preferred language for communication
    /// </summary>
    public Language PreferredLanguage { get; private set; }

    /// <summary>
    /// Optional external reference ID from PMS
    /// </summary>
    public string? ExternalPmsId { get; private set; }

    /// <summary>
    /// Whether the tenant account is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Factory method to create a new Tenant
    /// </summary>
    public static ErrorOr<Tenant> Create(
        string fullName,
        ContactInfo contactInfo,
        PropertyId propertyId,
        Language preferredLanguage,
        string? externalPmsId = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Error.Validation("Tenant.FullName", "Tenant name is required");

        var tenant = new Tenant(
            TenantId.CreateUnique(),
            fullName,
            contactInfo,
            propertyId,
            preferredLanguage)
        {
            ExternalPmsId = externalPmsId
        };

        return tenant;
    }

    /// <summary>
    /// Updates tenant information
    /// </summary>
    public ErrorOr<Success> UpdateContactInfo(ContactInfo contactInfo)
    {
        ContactInfo = contactInfo;
        return Result.Success;
    }

    /// <summary>
    /// Updates preferred language
    /// </summary>
    public void UpdatePreferredLanguage(Language language)
    {
        PreferredLanguage = language;
    }

    /// <summary>
    /// Moves tenant to a different property
    /// </summary>
    public void MoveTo(PropertyId newPropertyId)
    {
        PropertyId = newPropertyId;
    }

    /// <summary>
    /// Deactivates the tenant account
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reactivates the tenant account
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
    private Tenant() : base() { }
#pragma warning restore CS8618
}
