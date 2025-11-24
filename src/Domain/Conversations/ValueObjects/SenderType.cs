using DoorX.Domain.Common;

namespace DoorX.Domain.Conversations.ValueObjects;

/// <summary>
/// Smart Enum representing who sent a message
/// </summary>
/// <remarks>
/// Persistence Strategy: MEMORY ONLY (Smart Enum)
/// - Fixed set of actors in the system
/// - Stored as VARCHAR(50) in database for clarity
/// - These are core domain actors
///
/// See docs/CATALOGS_PERSISTENCE.md for details
/// </remarks>
public sealed class SenderType : SmartEnum<SenderType>
{
    // Predefined sender types (ID, Name, Display Name)
    public static readonly SenderType Tenant = new(1, "Tenant", "Tenant", "👤", "#3B82F6");
    public static readonly SenderType Vendor = new(2, "Vendor", "Service Provider", "🔧", "#F59E0B");
    public static readonly SenderType AI = new(3, "AI", "Aimee (AI Assistant)", "🤖", "#8B5CF6");
    public static readonly SenderType PropertyManager = new(4, "PropertyManager", "Property Manager", "👔", "#10B981");

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Icon emoji for UI display
    /// </summary>
    public string Icon { get; }

    /// <summary>
    /// Color code for UI display
    /// </summary>
    public string ColorCode { get; }

    private SenderType(int id, string name, string displayName, string icon, string colorCode)
        : base(id, name)
    {
        DisplayName = displayName;
        Icon = icon;
        ColorCode = colorCode;
    }

    /// <summary>
    /// Checks if this is the AI assistant
    /// </summary>
    public bool IsAI() => this == AI;

    /// <summary>
    /// Checks if this is a human user (not AI)
    /// </summary>
    public bool IsHuman() => !IsAI();

    /// <summary>
    /// Checks if this sender can create work orders
    /// </summary>
    public bool CanCreateWorkOrders()
    {
        return this == Tenant || this == PropertyManager;
    }

    /// <summary>
    /// Checks if this sender can submit bids
    /// </summary>
    public bool CanSubmitBids()
    {
        return this == Vendor;
    }

    /// <summary>
    /// Checks if this sender can approve work
    /// </summary>
    public bool CanApproveWork()
    {
        return this == PropertyManager || this == Tenant;
    }

    /// <summary>
    /// Gets the notification priority for messages from this sender
    /// </summary>
    public int GetNotificationPriority()
    {
        return this switch
        {
            var s when s == Tenant => 5,            // High (customer)
            var s when s == Vendor => 4,            // Medium-High (service provider)
            var s when s == PropertyManager => 3,   // Medium (admin)
            var s when s == AI => 2,                // Low (automated)
            _ => 1
        };
    }
}
