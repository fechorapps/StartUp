using DoorX.Domain.Conversations.Entities;
using DoorX.Domain.Conversations.ValueObjects;
using DoorX.Domain.Tenants.ValueObjects;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.UnitTests.Entities;

public class ConversationTests
{
    private readonly WorkOrderId _workOrderId = WorkOrderId.CreateUnique();
    private readonly TenantId _tenantId = TenantId.CreateUnique();
    private readonly VendorId _vendorId = VendorId.CreateUnique();

    [Fact]
    public void Create_WithValidData_ShouldReturnConversation()
    {
        // Act
        var result = Conversation.Create(_workOrderId, _tenantId);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.WorkOrderId.Should().Be(_workOrderId);
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.VendorId.Should().BeNull();
        result.Value.IsActive.Should().BeTrue();
        result.Value.ClosedAt.Should().BeNull();
        result.Value.Messages.Should().BeEmpty();
    }

    [Fact]
    public void AddVendor_WithValidVendor_ShouldSucceed()
    {
        // Arrange
        var conversation = CreateValidConversation();

        // Act
        var result = conversation.AddVendor(_vendorId);

        // Assert
        result.IsError.Should().BeFalse();
        conversation.VendorId.Should().Be(_vendorId);
    }

    [Fact]
    public void AddVendor_ToInactiveConversation_ShouldReturnError()
    {
        // Arrange
        var conversation = CreateValidConversation();
        conversation.Close();

        // Act
        var result = conversation.AddVendor(_vendorId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Conversation.AddVendor");
    }

    [Fact]
    public void AddVendor_WhenDifferentVendorAlreadyExists_ShouldReturnError()
    {
        // Arrange
        var conversation = CreateValidConversation();
        conversation.AddVendor(_vendorId);
        var differentVendorId = VendorId.CreateUnique();

        // Act
        var result = conversation.AddVendor(differentVendorId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Conversation.AddVendor");
        conversation.VendorId.Should().Be(_vendorId);
    }

    [Fact]
    public void AddVendor_SameVendorTwice_ShouldSucceed()
    {
        // Arrange
        var conversation = CreateValidConversation();
        conversation.AddVendor(_vendorId);

        // Act
        var result = conversation.AddVendor(_vendorId);

        // Assert
        result.IsError.Should().BeFalse();
        conversation.VendorId.Should().Be(_vendorId);
    }

    [Fact]
    public void AddMessage_WithValidContent_ShouldSucceed()
    {
        // Arrange
        var conversation = CreateValidConversation();

        // Act
        var result = conversation.AddMessage("Hello, I need help!", SenderType.Tenant, Channel.SMS);

        // Assert
        result.IsError.Should().BeFalse();
        conversation.Messages.Should().HaveCount(1);
        conversation.Messages.First().Content.Should().Be("Hello, I need help!");
    }

    [Fact]
    public void AddMessage_ToInactiveConversation_ShouldReturnError()
    {
        // Arrange
        var conversation = CreateValidConversation();
        conversation.Close();

        // Act
        var result = conversation.AddMessage("Test message", SenderType.Tenant, Channel.SMS);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Conversation.AddMessage");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void AddMessage_WithEmptyContent_ShouldReturnError(string? content)
    {
        // Arrange
        var conversation = CreateValidConversation();

        // Act
        var result = conversation.AddMessage(content!, SenderType.Tenant, Channel.SMS);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Message.Content");
    }

    [Fact]
    public void AddMessage_WithMessageObject_ShouldSucceed()
    {
        // Arrange
        var conversation = CreateValidConversation();
        var message = Message.Create("Test message", SenderType.AI, Channel.WebChat).Value;

        // Act
        var result = conversation.AddMessage(message);

        // Assert
        result.IsError.Should().BeFalse();
        conversation.Messages.Should().Contain(message);
    }

    [Fact]
    public void MarkAllMessagesAsRead_ShouldMarkAllUnreadMessages()
    {
        // Arrange
        var conversation = CreateValidConversation();
        conversation.AddMessage("Message 1", SenderType.Tenant, Channel.SMS);
        conversation.AddMessage("Message 2", SenderType.AI, Channel.SMS);
        conversation.AddMessage("Message 3", SenderType.Vendor, Channel.SMS);

        // Act
        conversation.MarkAllMessagesAsRead();

        // Assert
        conversation.Messages.Should().AllSatisfy(m => m.IsRead.Should().BeTrue());
        conversation.GetUnreadMessageCount().Should().Be(0);
    }

    [Fact]
    public void GetUnreadMessageCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var conversation = CreateValidConversation();
        conversation.AddMessage("Message 1", SenderType.Tenant, Channel.SMS);
        conversation.AddMessage("Message 2", SenderType.AI, Channel.SMS);
        conversation.AddMessage("Message 3", SenderType.Vendor, Channel.SMS);

        // Mark first message as read
        var firstMessage = conversation.Messages.First();
        firstMessage.MarkAsRead();

        // Act
        var unreadCount = conversation.GetUnreadMessageCount();

        // Assert
        unreadCount.Should().Be(2);
    }

    [Fact]
    public void GetLastMessage_ShouldReturnMostRecentMessage()
    {
        // Arrange
        var conversation = CreateValidConversation();
        conversation.AddMessage("First message", SenderType.Tenant, Channel.SMS);
        Thread.Sleep(10); // Ensure different timestamps
        conversation.AddMessage("Second message", SenderType.AI, Channel.SMS);
        Thread.Sleep(10);
        conversation.AddMessage("Last message", SenderType.Vendor, Channel.SMS);

        // Act
        var lastMessage = conversation.GetLastMessage();

        // Assert
        lastMessage.Should().NotBeNull();
        lastMessage!.Content.Should().Be("Last message");
    }

    [Fact]
    public void GetLastMessage_EmptyConversation_ShouldReturnNull()
    {
        // Arrange
        var conversation = CreateValidConversation();

        // Act
        var lastMessage = conversation.GetLastMessage();

        // Assert
        lastMessage.Should().BeNull();
    }

    [Fact]
    public void Close_ActiveConversation_ShouldSucceed()
    {
        // Arrange
        var conversation = CreateValidConversation();

        // Act
        var result = conversation.Close();

        // Assert
        result.IsError.Should().BeFalse();
        conversation.IsActive.Should().BeFalse();
        conversation.ClosedAt.Should().NotBeNull();
        conversation.ClosedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Close_AlreadyClosedConversation_ShouldReturnError()
    {
        // Arrange
        var conversation = CreateValidConversation();
        conversation.Close();

        // Act
        var result = conversation.Close();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Conversation.Close");
    }

    [Fact]
    public void Reopen_ClosedConversation_ShouldSucceed()
    {
        // Arrange
        var conversation = CreateValidConversation();
        conversation.Close();

        // Act
        var result = conversation.Reopen();

        // Assert
        result.IsError.Should().BeFalse();
        conversation.IsActive.Should().BeTrue();
        conversation.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void Reopen_AlreadyActiveConversation_ShouldReturnError()
    {
        // Arrange
        var conversation = CreateValidConversation();

        // Act
        var result = conversation.Reopen();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Conversation.Reopen");
    }

    [Fact]
    public void MultipleSenders_ShouldTrackMessagesCorrectly()
    {
        // Arrange
        var conversation = CreateValidConversation();

        // Act
        conversation.AddMessage("Tenant: I have a leak", SenderType.Tenant, Channel.SMS);
        conversation.AddMessage("AI: I can help you with that", SenderType.AI, Channel.SMS);
        conversation.AddMessage("Vendor: I can fix it for $100", SenderType.Vendor, Channel.SMS);

        // Assert
        conversation.Messages.Should().HaveCount(3);
        conversation.Messages.Count(m => m.SenderType == SenderType.Tenant).Should().Be(1);
        conversation.Messages.Count(m => m.SenderType == SenderType.AI).Should().Be(1);
        conversation.Messages.Count(m => m.SenderType == SenderType.Vendor).Should().Be(1);
    }

    [Fact]
    public void MultipleChannels_ShouldTrackChannelsCorrectly()
    {
        // Arrange
        var conversation = CreateValidConversation();

        // Act
        conversation.AddMessage("SMS message", SenderType.Tenant, Channel.SMS);
        conversation.AddMessage("WhatsApp message", SenderType.Tenant, Channel.WhatsApp);
        conversation.AddMessage("Web message", SenderType.AI, Channel.WebChat);

        // Assert
        conversation.Messages.Should().HaveCount(3);
        conversation.Messages.Count(m => m.Channel == Channel.SMS).Should().Be(1);
        conversation.Messages.Count(m => m.Channel == Channel.WhatsApp).Should().Be(1);
        conversation.Messages.Count(m => m.Channel == Channel.WebChat).Should().Be(1);
    }

    private Conversation CreateValidConversation()
    {
        return Conversation.Create(_workOrderId, _tenantId).Value;
    }
}
