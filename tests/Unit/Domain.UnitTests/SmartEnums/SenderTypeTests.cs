using DoorX.Domain.Conversations.ValueObjects;

namespace DoorX.Domain.UnitTests.SmartEnums;

public class SenderTypeTests
{
    [Fact]
    public void GetAll_ShouldReturnAllSenderTypes()
    {
        // Act
        var senderTypes = SenderType.GetAll();

        // Assert
        senderTypes.Should().HaveCount(4);
        senderTypes.Should().Contain(SenderType.Tenant);
        senderTypes.Should().Contain(SenderType.Vendor);
        senderTypes.Should().Contain(SenderType.AI);
        senderTypes.Should().Contain(SenderType.PropertyManager);
    }

    [Theory]
    [InlineData(1, "Tenant")]
    [InlineData(2, "Vendor")]
    [InlineData(3, "AI")]
    [InlineData(4, "PropertyManager")]
    public void FromId_WithValidId_ShouldReturnCorrectSenderType(int id, string expectedName)
    {
        // Act
        var senderType = SenderType.FromId(id);

        // Assert
        senderType.Should().NotBeNull();
        senderType!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var senderType = SenderType.FromId(999);

        // Assert
        senderType.Should().BeNull();
    }

    [Theory]
    [InlineData("Tenant", 1)]
    [InlineData("Vendor", 2)]
    [InlineData("AI", 3)]
    [InlineData("PropertyManager", 4)]
    public void FromName_WithValidName_ShouldReturnCorrectSenderType(string name, int expectedId)
    {
        // Act
        var senderType = SenderType.FromName(name);

        // Assert
        senderType.Should().NotBeNull();
        senderType!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var senderType = SenderType.FromName("NonExistent");

        // Assert
        senderType.Should().BeNull();
    }

    [Fact]
    public void FromName_IsCaseInsensitive()
    {
        // Act
        var lowerCase = SenderType.FromName("tenant");
        var upperCase = SenderType.FromName("TENANT");
        var correctCase = SenderType.FromName("Tenant");

        // Assert
        lowerCase.Should().Be(SenderType.Tenant);
        upperCase.Should().Be(SenderType.Tenant);
        correctCase.Should().Be(SenderType.Tenant);
    }

    [Fact]
    public void IsAI_OnlyForAISender_ShouldReturnTrue()
    {
        // Assert
        SenderType.AI.IsAI().Should().BeTrue();
        SenderType.Tenant.IsAI().Should().BeFalse();
        SenderType.Vendor.IsAI().Should().BeFalse();
        SenderType.PropertyManager.IsAI().Should().BeFalse();
    }

    [Fact]
    public void IsHuman_ForAllExceptAI_ShouldReturnTrue()
    {
        // Assert
        SenderType.Tenant.IsHuman().Should().BeTrue();
        SenderType.Vendor.IsHuman().Should().BeTrue();
        SenderType.PropertyManager.IsHuman().Should().BeTrue();
        SenderType.AI.IsHuman().Should().BeFalse();
    }

    [Theory]
    [InlineData("Tenant", true)]
    [InlineData("PropertyManager", true)]
    [InlineData("Vendor", false)]
    [InlineData("AI", false)]
    public void CanCreateWorkOrders_ShouldReturnCorrectValue(string senderName, bool expectedResult)
    {
        // Arrange
        var sender = SenderType.FromName(senderName);

        // Act
        var canCreate = sender!.CanCreateWorkOrders();

        // Assert
        canCreate.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("Vendor", true)]
    [InlineData("Tenant", false)]
    [InlineData("AI", false)]
    [InlineData("PropertyManager", false)]
    public void CanSubmitBids_ShouldReturnCorrectValue(string senderName, bool expectedResult)
    {
        // Arrange
        var sender = SenderType.FromName(senderName);

        // Act
        var canSubmit = sender!.CanSubmitBids();

        // Assert
        canSubmit.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("PropertyManager", true)]
    [InlineData("Tenant", true)]
    [InlineData("Vendor", false)]
    [InlineData("AI", false)]
    public void CanApproveWork_ShouldReturnCorrectValue(string senderName, bool expectedResult)
    {
        // Arrange
        var sender = SenderType.FromName(senderName);

        // Act
        var canApprove = sender!.CanApproveWork();

        // Assert
        canApprove.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("Tenant", 5)]
    [InlineData("Vendor", 4)]
    [InlineData("PropertyManager", 3)]
    [InlineData("AI", 2)]
    public void GetNotificationPriority_ShouldReturnCorrectValue(string senderName, int expectedPriority)
    {
        // Arrange
        var sender = SenderType.FromName(senderName);

        // Act
        var priority = sender!.GetNotificationPriority();

        // Assert
        priority.Should().Be(expectedPriority);
    }

    [Fact]
    public void DisplayName_ShouldNotBeEmpty()
    {
        // Act & Assert
        foreach (var senderType in SenderType.GetAll())
        {
            senderType.DisplayName.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Icon_ShouldNotBeEmpty()
    {
        // Act & Assert
        foreach (var senderType in SenderType.GetAll())
        {
            senderType.Icon.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void ColorCode_ShouldBeValidHexColor()
    {
        // Act & Assert
        foreach (var senderType in SenderType.GetAll())
        {
            senderType.ColorCode.Should().MatchRegex(@"^#[0-9A-F]{6}$",
                $"{senderType.Name} should have a valid hex color code");
        }
    }

    [Fact]
    public void Equality_SameSenderType_ShouldBeEqual()
    {
        // Arrange
        var sender1 = SenderType.Tenant;
        var sender2 = SenderType.FromId(1);

        // Assert
        sender2.Should().NotBeNull();
        sender1.Should().Be(sender2!);
        sender1.Equals(sender2!).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentSenderTypes_ShouldNotBeEqual()
    {
        // Arrange
        var sender1 = SenderType.Tenant;
        var sender2 = SenderType.Vendor;

        // Assert
        sender1.Should().NotBe(sender2);
        (sender1 == sender2).Should().BeFalse();
    }

    [Fact]
    public void AllSenderTypes_ShouldHaveUniqueIds()
    {
        // Act
        var senderTypes = SenderType.GetAll();
        var ids = senderTypes.Select(s => s.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllSenderTypes_ShouldHaveUniqueNames()
    {
        // Act
        var senderTypes = SenderType.GetAll();
        var names = senderTypes.Select(s => s.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllSenderTypes_ShouldHaveUniqueColorCodes()
    {
        // Act
        var senderTypes = SenderType.GetAll();
        var colors = senderTypes.Select(s => s.ColorCode).ToList();

        // Assert
        colors.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Tenant_ShouldHaveHighestNotificationPriority()
    {
        // Arrange
        var allPriorities = SenderType.GetAll()
            .Select(s => s.GetNotificationPriority())
            .ToList();

        // Act
        var tenantPriority = SenderType.Tenant.GetNotificationPriority();

        // Assert
        tenantPriority.Should().Be(allPriorities.Max());
    }

    [Fact]
    public void AI_ShouldHaveLowestNotificationPriority()
    {
        // Arrange
        var allPriorities = SenderType.GetAll()
            .Select(s => s.GetNotificationPriority())
            .ToList();

        // Act
        var aiPriority = SenderType.AI.GetNotificationPriority();

        // Assert
        aiPriority.Should().Be(allPriorities.Min());
    }

    [Fact]
    public void OnlyVendor_CanSubmitBids()
    {
        // Act
        var canSubmitBids = SenderType.GetAll()
            .Where(s => s.CanSubmitBids())
            .ToList();

        // Assert
        canSubmitBids.Should().HaveCount(1);
        canSubmitBids.Should().Contain(SenderType.Vendor);
    }

    [Fact]
    public void HumanSenders_ShouldBeThree()
    {
        // Act
        var humanSenders = SenderType.GetAll()
            .Where(s => s.IsHuman())
            .ToList();

        // Assert
        humanSenders.Should().HaveCount(3);
        humanSenders.Should().NotContain(SenderType.AI);
    }
}
