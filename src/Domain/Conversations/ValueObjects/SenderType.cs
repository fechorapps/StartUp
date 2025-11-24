using ErrorOr;

namespace DoorX.Domain.Conversations.ValueObjects;

/// <summary>
/// Value Object representing who sent a message
/// </summary>
public record SenderType
{
    private static readonly HashSet<string> ValidSenderTypes = new()
    {
        "Tenant", "Vendor", "AI", "PropertyManager"
    };

    public string Value { get; init; }

    private SenderType(string value)
    {
        Value = value;
    }

    public static ErrorOr<SenderType> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("SenderType.Value", "Sender type cannot be empty");

        if (!ValidSenderTypes.Contains(value))
            return Error.Validation("SenderType.Value", $"Invalid sender type. Must be one of: {string.Join(", ", ValidSenderTypes)}");

        return new SenderType(value);
    }

    public static SenderType Tenant => new("Tenant");
    public static SenderType Vendor => new("Vendor");
    public static SenderType AI => new("AI");
    public static SenderType PropertyManager => new("PropertyManager");

    public bool IsAI() => Value == "AI";

    public override string ToString() => Value;
}
