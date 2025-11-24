using DoorX.Domain.Common;
using DoorX.Domain.Conversations.ValueObjects;
using ErrorOr;

namespace DoorX.Domain.Conversations.Entities;

/// <summary>
/// Entity representing an individual message in a conversation
/// </summary>
/// <remarks>
/// Message is a child entity within the Conversation aggregate.
/// It cannot exist independently and has no repository of its own.
/// </remarks>
public sealed class Message : Entity<MessageId>
{
    private Message(
        MessageId id,
        string content,
        SenderType senderType,
        Channel channel) : base(id)
    {
        Content = content;
        SenderType = senderType;
        Channel = channel;
        SentAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Message content/text
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Who sent the message (Tenant, Vendor, AI, PropertyManager)
    /// </summary>
    public SenderType SenderType { get; private set; }

    /// <summary>
    /// Communication channel used (SMS, WhatsApp, WebChat, Email)
    /// </summary>
    public Channel Channel { get; private set; }

    /// <summary>
    /// When the message was sent
    /// </summary>
    public DateTime SentAt { get; private set; }

    /// <summary>
    /// Whether the message has been read by the recipient
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// When the message was read (if read)
    /// </summary>
    public DateTime? ReadAt { get; private set; }

    /// <summary>
    /// Factory method to create a new Message
    /// </summary>
    public static ErrorOr<Message> Create(
        string content,
        SenderType senderType,
        Channel channel)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Error.Validation("Message.Content", "Message content cannot be empty");

        var message = new Message(
            MessageId.CreateUnique(),
            content,
            senderType,
            channel);

        return message;
    }

    /// <summary>
    /// Marks the message as read
    /// </summary>
    internal void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

#pragma warning disable CS8618
    private Message() : base() { }
#pragma warning restore CS8618
}
