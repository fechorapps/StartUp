using ErrorOr;

namespace DoorX.Domain.Conversations.ValueObjects;

/// <summary>
/// Value Object representing the communication channel
/// </summary>
public record Channel
{
    private static readonly HashSet<string> ValidChannels = new()
    {
        "SMS", "WhatsApp", "WebChat", "Email"
    };

    public string Value { get; init; }

    private Channel(string value)
    {
        Value = value;
    }

    public static ErrorOr<Channel> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("Channel.Value", "Channel cannot be empty");

        if (!ValidChannels.Contains(value))
            return Error.Validation("Channel.Value", $"Invalid channel. Must be one of: {string.Join(", ", ValidChannels)}");

        return new Channel(value);
    }

    public static Channel SMS => new("SMS");
    public static Channel WhatsApp => new("WhatsApp");
    public static Channel WebChat => new("WebChat");
    public static Channel Email => new("Email");

    public override string ToString() => Value;
}
