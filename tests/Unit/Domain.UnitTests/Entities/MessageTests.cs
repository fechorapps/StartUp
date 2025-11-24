using DoorX.Domain.Conversations.Entities;
using DoorX.Domain.Conversations.ValueObjects;

namespace DoorX.Domain.UnitTests.Entities;

public class MessageTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnMessage()
    {
        // Act
        var result = Message.Create("Hello, I need help!", SenderType.Tenant, Channel.SMS);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Content.Should().Be("Hello, I need help!");
        result.Value.SenderType.Should().Be(SenderType.Tenant);
        result.Value.Channel.Should().Be(Channel.SMS);
        result.Value.IsRead.Should().BeFalse();
        result.Value.ReadAt.Should().BeNull();
        result.Value.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyContent_ShouldReturnError(string? content)
    {
        // Act
        var result = Message.Create(content!, SenderType.Tenant, Channel.SMS);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Message.Content");
    }

    [Theory]
    [InlineData("Short")]
    [InlineData("This is a medium length message")]
    [InlineData("This is a very long message that contains a lot of text to test if the message entity can handle long content without any issues")]
    public void Create_WithVariousContentLengths_ShouldSucceed(string content)
    {
        // Act
        var result = Message.Create(content, SenderType.AI, Channel.WebChat);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Content.Should().Be(content);
    }

    [Fact]
    public void Create_WithAllSenderTypes_ShouldSucceed()
    {
        // Arrange
        var senderTypes = new[]
        {
            SenderType.Tenant,
            SenderType.Vendor,
            SenderType.AI,
            SenderType.PropertyManager
        };

        // Act & Assert
        foreach (var senderType in senderTypes)
        {
            var result = Message.Create("Test message", senderType, Channel.SMS);
            result.IsError.Should().BeFalse();
            result.Value.SenderType.Should().Be(senderType);
        }
    }

    [Fact]
    public void Create_WithAllChannels_ShouldSucceed()
    {
        // Arrange
        var channels = new[]
        {
            Channel.SMS,
            Channel.WhatsApp,
            Channel.WebChat,
            Channel.Email
        };

        // Act & Assert
        foreach (var channel in channels)
        {
            var result = Message.Create("Test message", SenderType.Tenant, channel);
            result.IsError.Should().BeFalse();
            result.Value.Channel.Should().Be(channel);
        }
    }

    [Fact]
    public void Create_MultipleMessages_ShouldHaveDifferentIds()
    {
        // Act
        var message1 = Message.Create("Message 1", SenderType.Tenant, Channel.SMS).Value;
        var message2 = Message.Create("Message 2", SenderType.Tenant, Channel.SMS).Value;

        // Assert
        message1.Id.Should().NotBe(message2.Id);
    }

    [Fact]
    public void SentAt_ShouldBeSetToCurrentTime()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var message = Message.Create("Test", SenderType.Tenant, Channel.SMS).Value;
        var afterCreate = DateTime.UtcNow;

        // Assert
        message.SentAt.Should().BeOnOrAfter(beforeCreate);
        message.SentAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void IsRead_InitiallyFalse()
    {
        // Act
        var message = Message.Create("Test", SenderType.Tenant, Channel.SMS).Value;

        // Assert
        message.IsRead.Should().BeFalse();
    }

    [Fact]
    public void ReadAt_InitiallyNull()
    {
        // Act
        var message = Message.Create("Test", SenderType.Tenant, Channel.SMS).Value;

        // Assert
        message.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Content_PreservesWhitespace()
    {
        // Arrange
        var contentWithSpaces = "  Test  Message  ";

        // Act
        var message = Message.Create(contentWithSpaces, SenderType.Tenant, Channel.SMS).Value;

        // Assert
        message.Content.Should().Be(contentWithSpaces);
    }

    [Fact]
    public void Content_PreservesLineBreaks()
    {
        // Arrange
        var contentWithLineBreaks = "Line 1\nLine 2\nLine 3";

        // Act
        var message = Message.Create(contentWithLineBreaks, SenderType.Tenant, Channel.SMS).Value;

        // Assert
        message.Content.Should().Be(contentWithLineBreaks);
    }

    [Fact]
    public void Content_PreservesSpecialCharacters()
    {
        // Arrange
        var contentWithSpecialChars = "Hello! @#$%^&*() 😊 <>&\"'";

        // Act
        var message = Message.Create(contentWithSpecialChars, SenderType.AI, Channel.WebChat).Value;

        // Assert
        message.Content.Should().Be(contentWithSpecialChars);
    }

    [Fact]
    public void Equality_SameReference_ShouldBeEqual()
    {
        // Arrange
        var message = Message.Create("Test", SenderType.Tenant, Channel.SMS).Value;

        // Assert
        message.Should().Be(message);
        message.Equals(message).Should().BeTrue();
    }
}
