using DoorX.Domain.Common;
using DoorX.Domain.Conversations.Events;
using DoorX.Domain.Conversations.ValueObjects;
using DoorX.Domain.Tenants.ValueObjects;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;
using ErrorOr;

namespace DoorX.Domain.Conversations.Entities;

/// <summary>
/// Aggregate Root representing a conversation thread about a work order
/// </summary>
/// <remarks>
/// A Conversation manages all communication between participants (Tenant, Vendor, AI)
/// regarding a specific work order. It supports multi-channel messaging (SMS, WhatsApp, Web).
/// Participants: Tenant + Vendor (if assigned) + Aimee (AI Assistant)
/// </remarks>
public sealed class Conversation : AggregateRoot<ConversationId>
{
    private readonly List<Message> _messages = new();

    private Conversation(
        ConversationId id,
        WorkOrderId workOrderId,
        TenantId tenantId) : base(id)
    {
        WorkOrderId = workOrderId;
        TenantId = tenantId;
        IsActive = true;
    }

    /// <summary>
    /// Work order this conversation is about
    /// </summary>
    public WorkOrderId WorkOrderId { get; private set; }

    /// <summary>
    /// Tenant participating in the conversation
    /// </summary>
    public TenantId TenantId { get; private set; }

    /// <summary>
    /// Vendor participating in the conversation (if assigned)
    /// </summary>
    public VendorId? VendorId { get; private set; }

    /// <summary>
    /// Messages in the conversation (read-only collection)
    /// </summary>
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Whether the conversation is still active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When the conversation was closed (if closed)
    /// </summary>
    public DateTime? ClosedAt { get; private set; }

    /// <summary>
    /// Factory method to create a new Conversation
    /// </summary>
    public static ErrorOr<Conversation> Create(
        WorkOrderId workOrderId,
        TenantId tenantId)
    {
        var conversation = new Conversation(
            ConversationId.CreateUnique(),
            workOrderId,
            tenantId);

        conversation.AddDomainEvent(new ConversationStartedEvent(conversation.Id, workOrderId, tenantId));

        return conversation;
    }

    /// <summary>
    /// Adds a vendor to the conversation
    /// </summary>
    public ErrorOr<Success> AddVendor(VendorId vendorId)
    {
        if (!IsActive)
            return Error.Validation("Conversation.AddVendor", "Cannot add vendor to an inactive conversation");

        if (VendorId is not null && VendorId != vendorId)
            return Error.Validation("Conversation.AddVendor", "Conversation already has a different vendor assigned");

        VendorId = vendorId;

        AddDomainEvent(new VendorAddedToConversationEvent(Id, vendorId));

        return Result.Success;
    }

    /// <summary>
    /// Adds a message to the conversation
    /// </summary>
    public ErrorOr<Success> AddMessage(Message message)
    {
        if (!IsActive)
            return Error.Validation("Conversation.AddMessage", "Cannot add messages to an inactive conversation");

        _messages.Add(message);

        AddDomainEvent(new MessageSentEvent(Id, message.Id, message.SenderType, message.Channel));

        return Result.Success;
    }

    /// <summary>
    /// Adds a message to the conversation (convenience method)
    /// </summary>
    public ErrorOr<Success> AddMessage(string content, SenderType senderType, Channel channel)
    {
        var messageResult = Message.Create(content, senderType, channel);
        if (messageResult.IsError)
            return messageResult.Errors;

        return AddMessage(messageResult.Value);
    }

    /// <summary>
    /// Marks all messages as read
    /// </summary>
    public void MarkAllMessagesAsRead()
    {
        foreach (var message in _messages.Where(m => !m.IsRead))
        {
            message.MarkAsRead();
        }
    }

    /// <summary>
    /// Gets unread message count
    /// </summary>
    public int GetUnreadMessageCount()
    {
        return _messages.Count(m => !m.IsRead);
    }

    /// <summary>
    /// Gets the last message in the conversation
    /// </summary>
    public Message? GetLastMessage()
    {
        return _messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
    }

    /// <summary>
    /// Closes the conversation
    /// </summary>
    public ErrorOr<Success> Close()
    {
        if (!IsActive)
            return Error.Validation("Conversation.Close", "Conversation is already closed");

        IsActive = false;
        ClosedAt = DateTime.UtcNow;

        AddDomainEvent(new ConversationClosedEvent(Id, WorkOrderId));

        return Result.Success;
    }

    /// <summary>
    /// Reopens the conversation
    /// </summary>
    public ErrorOr<Success> Reopen()
    {
        if (IsActive)
            return Error.Validation("Conversation.Reopen", "Conversation is already active");

        IsActive = true;
        ClosedAt = null;

        return Result.Success;
    }

#pragma warning disable CS8618
    private Conversation() : base() { }
#pragma warning restore CS8618
}
