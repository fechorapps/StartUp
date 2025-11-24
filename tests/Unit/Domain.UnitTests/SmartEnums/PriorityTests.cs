using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.UnitTests.SmartEnums;

public class PriorityTests
{
    [Fact]
    public void GetAll_ShouldReturnAllPriorities()
    {
        // Act
        var priorities = Priority.GetAll().ToList();

        // Assert
        priorities.Should().HaveCount(4);
        priorities.Should().Contain(Priority.Emergency);
        priorities.Should().Contain(Priority.High);
        priorities.Should().Contain(Priority.Normal);
        priorities.Should().Contain(Priority.Low);
    }

    [Theory]
    [InlineData(1, "Emergency")]
    [InlineData(2, "High")]
    [InlineData(3, "Normal")]
    [InlineData(4, "Low")]
    public void FromId_WithValidId_ShouldReturnCorrectPriority(int id, string expectedName)
    {
        // Act
        var priority = Priority.FromId(id);

        // Assert
        priority.Should().NotBeNull();
        priority!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var priority = Priority.FromId(999);

        // Assert
        priority.Should().BeNull();
    }

    [Theory]
    [InlineData("Emergency", 1)]
    [InlineData("High", 2)]
    [InlineData("Normal", 3)]
    [InlineData("Low", 4)]
    [InlineData("emergency", 1)] // Case insensitive
    [InlineData("HIGH", 2)]
    public void FromName_WithValidName_ShouldReturnCorrectPriority(string name, int expectedId)
    {
        // Act
        var priority = Priority.FromName(name);

        // Assert
        priority.Should().NotBeNull();
        priority!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var priority = Priority.FromName("InvalidPriority");

        // Assert
        priority.Should().BeNull();
    }

    [Fact]
    public void Emergency_IsEmergency_ShouldReturnTrue()
    {
        // Assert
        Priority.Emergency.IsEmergency().Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(NonEmergencyPriorities))]
    public void NonEmergencyPriorities_IsEmergency_ShouldReturnFalse(Priority priority)
    {
        // Assert
        priority.IsEmergency().Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(UrgentPriorities))]
    public void EmergencyAndHigh_IsUrgent_ShouldReturnTrue(Priority priority)
    {
        // Assert
        priority.IsUrgent().Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(NonUrgentPriorities))]
    public void NormalAndLow_IsUrgent_ShouldReturnFalse(Priority priority)
    {
        // Assert
        priority.IsUrgent().Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 24)]
    [InlineData(2, 48)]
    [InlineData(3, 120)]
    [InlineData(4, 168)]
    public void GetExpectedResponseTime_ShouldReturnCorrectTimeSpan(int priorityId, int expectedHours)
    {
        // Arrange
        var priority = Priority.FromId(priorityId)!;

        // Act
        var responseTime = priority.GetExpectedResponseTime();

        // Assert
        responseTime.Should().Be(TimeSpan.FromHours(expectedHours));
    }

    [Fact]
    public void GetExpectedCompletionDate_ShouldAddExpectedHoursToCreatedDate()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 10, 0, 0);
        var priority = Priority.Emergency;

        // Act
        var completionDate = priority.GetExpectedCompletionDate(createdAt);

        // Assert
        completionDate.Should().Be(createdAt.AddHours(24));
    }

    [Fact]
    public void IsOverdue_WhenPastExpectedCompletion_ShouldReturnTrue()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 10, 0, 0);
        var now = new DateTime(2024, 1, 3, 10, 0, 0); // 2 days later
        var priority = Priority.Emergency; // 24 hours

        // Act
        var isOverdue = priority.IsOverdue(createdAt, now);

        // Assert
        isOverdue.Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenBeforeExpectedCompletion_ShouldReturnFalse()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 10, 0, 0);
        var now = new DateTime(2024, 1, 1, 20, 0, 0); // 10 hours later
        var priority = Priority.Emergency; // 24 hours

        // Act
        var isOverdue = priority.IsOverdue(createdAt, now);

        // Assert
        isOverdue.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, "#DC2626")] // Emergency - Red
    [InlineData(2, "#F59E0B")] // High - Orange
    [InlineData(3, "#3B82F6")] // Normal - Blue
    [InlineData(4, "#10B981")] // Low - Green
    public void GetColorCode_ShouldReturnCorrectColor(int priorityId, string expectedColor)
    {
        // Arrange
        var priority = Priority.FromId(priorityId)!;

        // Act
        var color = priority.GetColorCode();

        // Assert
        color.Should().Be(expectedColor);
    }

    [Fact]
    public void Equality_SamePriority_ShouldBeEqual()
    {
        // Arrange
        var priority1 = Priority.Emergency;
        var priority2 = Priority.Emergency;

        // Assert
        priority1.Should().Be(priority2);
        (priority1 == priority2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentPriorities_ShouldNotBeEqual()
    {
        // Arrange
        var priority1 = Priority.Emergency;
        var priority2 = Priority.High;

        // Assert
        priority1.Should().NotBe(priority2);
        (priority1 != priority2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnName()
    {
        // Assert
        Priority.Emergency.ToString().Should().Be("Emergency");
        Priority.High.ToString().Should().Be("High");
    }

    public static IEnumerable<object[]> NonEmergencyPriorities()
    {
        yield return new object[] { Priority.High };
        yield return new object[] { Priority.Normal };
        yield return new object[] { Priority.Low };
    }

    public static IEnumerable<object[]> UrgentPriorities()
    {
        yield return new object[] { Priority.Emergency };
        yield return new object[] { Priority.High };
    }

    public static IEnumerable<object[]> NonUrgentPriorities()
    {
        yield return new object[] { Priority.Normal };
        yield return new object[] { Priority.Low };
    }
}
