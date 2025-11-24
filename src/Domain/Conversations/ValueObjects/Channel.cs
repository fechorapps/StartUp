using DoorX.Domain.Common;

namespace DoorX.Domain.Conversations.ValueObjects;

/// <summary>
/// Smart Enum representing the communication channel
/// </summary>
/// <remarks>
/// Persistence Strategy: MEMORY ONLY (Smart Enum)
/// - Fixed set of supported communication channels
/// - Stored as VARCHAR(50) in database for clarity
/// - Adding new channels requires infrastructure code changes
///
/// See docs/CATALOGS_PERSISTENCE.md for details
/// </remarks>
public sealed class Channel : SmartEnum<Channel>
{
    // Predefined channels (ID, Name, Description)
    public static readonly Channel SMS = new(1, "SMS", "Text message via cellular network", "📱");
    public static readonly Channel WhatsApp = new(2, "WhatsApp", "WhatsApp messaging", "💬");
    public static readonly Channel WebChat = new(3, "WebChat", "Web chat interface", "🖥️");
    public static readonly Channel Email = new(4, "Email", "Email communication", "📧");

    /// <summary>
    /// Description of the channel
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Icon emoji for UI display
    /// </summary>
    public string Icon { get; }

    private Channel(int id, string name, string description, string icon) : base(id, name)
    {
        Description = description;
        Icon = icon;
    }

    /// <summary>
    /// Checks if this channel supports real-time messaging
    /// </summary>
    public bool SupportsRealtime()
    {
        return this == WhatsApp || this == WebChat;
    }

    /// <summary>
    /// Checks if this channel supports rich media (images, files)
    /// </summary>
    public bool SupportsRichMedia()
    {
        return this == WhatsApp || this == Email || this == WebChat;
    }

    /// <summary>
    /// Checks if this channel requires mobile phone number
    /// </summary>
    public bool RequiresPhoneNumber()
    {
        return this == SMS || this == WhatsApp;
    }

    /// <summary>
    /// Gets the typical response time expectation for this channel
    /// </summary>
    public TimeSpan GetTypicalResponseTime()
    {
        return this switch
        {
            var c when c == SMS => TimeSpan.FromMinutes(15),
            var c when c == WhatsApp => TimeSpan.FromMinutes(5),
            var c when c == WebChat => TimeSpan.FromMinutes(2),
            var c when c == Email => TimeSpan.FromHours(2),
            _ => TimeSpan.FromHours(1)
        };
    }

    /// <summary>
    /// Gets the maximum message length for this channel
    /// </summary>
    public int GetMaxMessageLength()
    {
        return this switch
        {
            var c when c == SMS => 1600,           // Standard SMS limit (multiple messages)
            var c when c == WhatsApp => 65536,     // WhatsApp limit
            var c when c == WebChat => 10000,      // Practical web limit
            var c when c == Email => 100000,       // Practical email limit
            _ => 1000
        };
    }
}
