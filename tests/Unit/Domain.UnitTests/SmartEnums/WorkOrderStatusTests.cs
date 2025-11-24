using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.UnitTests.SmartEnums;

public class WorkOrderStatusTests
{
    [Fact]
    public void GetAll_ShouldReturn9Statuses()
    {
        // Act
        var statuses = WorkOrderStatus.GetAll().ToList();

        // Assert
        statuses.Should().HaveCount(9);
    }

    [Theory]
    [InlineData(1, "Open")]
    [InlineData(5, "Scheduled")]
    [InlineData(8, "Closed")]
    [InlineData(9, "Cancelled")]
    public void FromId_WithValidId_ShouldReturnCorrectStatus(int id, string expectedName)
    {
        // Act
        var status = WorkOrderStatus.FromId(id);

        // Assert
        status.Should().NotBeNull();
        status!.Name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("Open", "Categorized", true)]
    [InlineData("Open", "Cancelled", true)]
    [InlineData("Open", "Scheduled", false)] // Can't skip states
    [InlineData("Categorized", "VendorSearch", true)]
    [InlineData("Bidding", "Scheduled", true)]
    [InlineData("Bidding", "VendorSearch", true)] // Can go back
    [InlineData("InProgress", "Completed", true)]
    [InlineData("Completed", "Closed", true)]
    [InlineData("Closed", "Open", false)] // Final state
    [InlineData("Cancelled", "Open", false)] // Final state
    public void CanTransitionTo_ShouldValidateTransitions(string fromName, string toName, bool expected)
    {
        // Arrange
        var fromStatus = WorkOrderStatus.FromName(fromName)!;
        var toStatus = WorkOrderStatus.FromName(toName)!;

        // Act
        var canTransition = fromStatus.CanTransitionTo(toStatus);

        // Assert
        canTransition.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(FinalStates))]
    public void IsFinalState_ForClosedAndCancelled_ShouldReturnTrue(WorkOrderStatus status)
    {
        // Assert
        status.IsFinalState().Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ActiveStates))]
    public void IsFinalState_ForActiveStates_ShouldReturnFalse(WorkOrderStatus status)
    {
        // Assert
        status.IsFinalState().Should().BeFalse();
    }

    [Fact]
    public void IsActive_ForFinalStates_ShouldReturnFalse()
    {
        // Assert
        WorkOrderStatus.Closed.IsActive().Should().BeFalse();
        WorkOrderStatus.Cancelled.IsActive().Should().BeFalse();
    }

    [Fact]
    public void AllowsVendorAssignment_OnlyBidding_ShouldReturnTrue()
    {
        // Assert
        WorkOrderStatus.Bidding.AllowsVendorAssignment().Should().BeTrue();
        WorkOrderStatus.Open.AllowsVendorAssignment().Should().BeFalse();
        WorkOrderStatus.Scheduled.AllowsVendorAssignment().Should().BeFalse();
    }

    [Fact]
    public void AllowsModifications_ForActiveStat es_ShouldReturnTrue()
    {
        // Assert
        WorkOrderStatus.Open.AllowsModifications().Should().BeTrue();
        WorkOrderStatus.Bidding.AllowsModifications().Should().BeTrue();
        WorkOrderStatus.Closed.AllowsModifications().Should().BeFalse();
    }

    [Theory]
    [InlineData("Open", 0)]
    [InlineData("Categorized", 10)]
    [InlineData("Scheduled", 50)]
    [InlineData("Completed", 90)]
    [InlineData("Closed", 100)]
    public void GetProgressPercentage_ShouldReturnCorrectValue(string statusName, int expectedPercentage)
    {
        // Arrange
        var status = WorkOrderStatus.FromName(statusName)!;

        // Act
        var percentage = status.GetProgressPercentage();

        // Assert
        percentage.Should().Be(expectedPercentage);
    }

    public static IEnumerable<object[]> FinalStates()
    {
        yield return new object[] { WorkOrderStatus.Closed };
        yield return new object[] { WorkOrderStatus.Cancelled };
    }

    public static IEnumerable<object[]> ActiveStates()
    {
        yield return new object[] { WorkOrderStatus.Open };
        yield return new object[] { WorkOrderStatus.Categorized };
        yield return new object[] { WorkOrderStatus.VendorSearch };
        yield return new object[] { WorkOrderStatus.Bidding };
        yield return new object[] { WorkOrderStatus.Scheduled };
        yield return new object[] { WorkOrderStatus.InProgress };
        yield return new object[] { WorkOrderStatus.Completed };
    }
}
