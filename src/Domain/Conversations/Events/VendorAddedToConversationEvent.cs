using DoorX.Domain.Common.Interfaces;
using DoorX.Domain.Conversations.ValueObjects;
using DoorX.Domain.Vendors.ValueObjects;

namespace DoorX.Domain.Conversations.Events;

/// <summary>
/// Domain event raised when a vendor is added to a conversation
/// </summary>
public record VendorAddedToConversationEvent(
    ConversationId ConversationId,
    VendorId VendorId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
