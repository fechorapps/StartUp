using DoorX.Domain.Conversations.ValueObjects;

namespace DoorX.Domain.UnitTests.SmartEnums;

public class ChannelTests
{
    [Fact]
    public void GetAll_ShouldReturnAllChannels()
    {
        // Act
        var channels = Channel.GetAll();

        // Assert
        channels.Should().HaveCount(4);
        channels.Should().Contain(Channel.SMS);
        channels.Should().Contain(Channel.WhatsApp);
        channels.Should().Contain(Channel.WebChat);
        channels.Should().Contain(Channel.Email);
    }

    [Theory]
    [InlineData(1, "SMS")]
    [InlineData(2, "WhatsApp")]
    [InlineData(3, "WebChat")]
    [InlineData(4, "Email")]
    public void FromId_WithValidId_ShouldReturnCorrectChannel(int id, string expectedName)
    {
        // Act
        var channel = Channel.FromId(id);

        // Assert
        channel.Should().NotBeNull();
        channel!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var channel = Channel.FromId(999);

        // Assert
        channel.Should().BeNull();
    }

    [Theory]
    [InlineData("SMS", 1)]
    [InlineData("WhatsApp", 2)]
    [InlineData("WebChat", 3)]
    [InlineData("Email", 4)]
    public void FromName_WithValidName_ShouldReturnCorrectChannel(string name, int expectedId)
    {
        // Act
        var channel = Channel.FromName(name);

        // Assert
        channel.Should().NotBeNull();
        channel!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var channel = Channel.FromName("NonExistent");

        // Assert
        channel.Should().BeNull();
    }

    [Fact]
    public void FromName_IsCaseInsensitive()
    {
        // Act
        var lowerCase = Channel.FromName("sms");
        var upperCase = Channel.FromName("SMS");
        var mixedCase = Channel.FromName("SmS");

        // Assert
        lowerCase.Should().Be(Channel.SMS);
        upperCase.Should().Be(Channel.SMS);
        mixedCase.Should().Be(Channel.SMS);
    }

    [Theory]
    [InlineData("WhatsApp", true)]
    [InlineData("WebChat", true)]
    [InlineData("SMS", false)]
    [InlineData("Email", false)]
    public void SupportsRealtime_ShouldReturnCorrectValue(string channelName, bool expectedResult)
    {
        // Arrange
        var channel = Channel.FromName(channelName);

        // Act
        var supportsRealtime = channel!.SupportsRealtime();

        // Assert
        supportsRealtime.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("WhatsApp", true)]
    [InlineData("Email", true)]
    [InlineData("WebChat", true)]
    [InlineData("SMS", false)]
    public void SupportsRichMedia_ShouldReturnCorrectValue(string channelName, bool expectedResult)
    {
        // Arrange
        var channel = Channel.FromName(channelName);

        // Act
        var supportsRichMedia = channel!.SupportsRichMedia();

        // Assert
        supportsRichMedia.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("SMS", true)]
    [InlineData("WhatsApp", true)]
    [InlineData("WebChat", false)]
    [InlineData("Email", false)]
    public void RequiresPhoneNumber_ShouldReturnCorrectValue(string channelName, bool expectedResult)
    {
        // Arrange
        var channel = Channel.FromName(channelName);

        // Act
        var requiresPhone = channel!.RequiresPhoneNumber();

        // Assert
        requiresPhone.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("SMS", 15)]
    [InlineData("WhatsApp", 5)]
    [InlineData("WebChat", 2)]
    [InlineData("Email", 120)]
    public void GetTypicalResponseTime_ShouldReturnCorrectValue(string channelName, int expectedMinutes)
    {
        // Arrange
        var channel = Channel.FromName(channelName);

        // Act
        var responseTime = channel!.GetTypicalResponseTime();

        // Assert
        responseTime.Should().Be(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Theory]
    [InlineData("SMS", 1600)]
    [InlineData("WhatsApp", 65536)]
    [InlineData("WebChat", 10000)]
    [InlineData("Email", 100000)]
    public void GetMaxMessageLength_ShouldReturnCorrectValue(string channelName, int expectedLength)
    {
        // Arrange
        var channel = Channel.FromName(channelName);

        // Act
        var maxLength = channel!.GetMaxMessageLength();

        // Assert
        maxLength.Should().Be(expectedLength);
    }

    [Fact]
    public void Description_ShouldNotBeEmpty()
    {
        // Act & Assert
        foreach (var channel in Channel.GetAll())
        {
            channel.Description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Icon_ShouldNotBeEmpty()
    {
        // Act & Assert
        foreach (var channel in Channel.GetAll())
        {
            channel.Icon.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Equality_SameChannel_ShouldBeEqual()
    {
        // Arrange
        var channel1 = Channel.SMS;
        var channel2 = Channel.FromId(1);

        // Assert
        channel2.Should().NotBeNull();
        channel1.Should().Be(channel2);
        (channel1 == channel2!).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentChannels_ShouldNotBeEqual()
    {
        // Arrange
        var channel1 = Channel.SMS;
        var channel2 = Channel.WhatsApp;

        // Assert
        channel1.Should().NotBe(channel2);
        (channel1 == channel2).Should().BeFalse();
    }

    [Fact]
    public void AllChannels_ShouldHaveUniqueIds()
    {
        // Act
        var channels = Channel.GetAll();
        var ids = channels.Select(c => c.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllChannels_ShouldHaveUniqueNames()
    {
        // Act
        var channels = Channel.GetAll();
        var names = channels.Select(c => c.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void RealtimeChannels_ShouldBeWhatsAppAndWebChat()
    {
        // Act
        var realtimeChannels = Channel.GetAll()
            .Where(c => c.SupportsRealtime())
            .ToList();

        // Assert
        realtimeChannels.Should().HaveCount(2);
        realtimeChannels.Should().Contain(Channel.WhatsApp);
        realtimeChannels.Should().Contain(Channel.WebChat);
    }

    [Fact]
    public void PhoneBasedChannels_ShouldBeSMSAndWhatsApp()
    {
        // Act
        var phoneChannels = Channel.GetAll()
            .Where(c => c.RequiresPhoneNumber())
            .ToList();

        // Assert
        phoneChannels.Should().HaveCount(2);
        phoneChannels.Should().Contain(Channel.SMS);
        phoneChannels.Should().Contain(Channel.WhatsApp);
    }

    [Fact]
    public void WebChat_ShouldHaveFastestResponseTime()
    {
        // Arrange
        var allResponseTimes = Channel.GetAll()
            .Select(c => c.GetTypicalResponseTime())
            .ToList();

        // Act
        var webChatResponseTime = Channel.WebChat.GetTypicalResponseTime();

        // Assert
        webChatResponseTime.Should().Be(allResponseTimes.Min());
    }

    [Fact]
    public void Email_ShouldHaveMaxMessageLength()
    {
        // Arrange
        var allMaxLengths = Channel.GetAll()
            .Select(c => c.GetMaxMessageLength())
            .ToList();

        // Act
        var emailMaxLength = Channel.Email.GetMaxMessageLength();

        // Assert
        emailMaxLength.Should().Be(allMaxLengths.Max());
    }
}
