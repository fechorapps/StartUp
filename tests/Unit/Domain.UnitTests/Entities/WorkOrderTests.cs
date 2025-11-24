using DoorX.Domain.Properties.ValueObjects;
using DoorX.Domain.Tenants.ValueObjects;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.Entities;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.UnitTests.Entities;

public class WorkOrderTests
{
    private readonly TenantId _tenantId = TenantId.CreateUnique();
    private readonly PropertyId _propertyId = PropertyId.CreateUnique();
    private const string ValidDescription = "AC not cooling properly";

    [Fact]
    public void Create_WithValidData_ShouldReturnWorkOrder()
    {
        // Act
        var result = WorkOrder.Create(
            _tenantId,
            _propertyId,
            ValidDescription,
            ServiceCategory.HVAC,
            Priority.High);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.PropertyId.Should().Be(_propertyId);
        result.Value.IssueDescription.Should().Be(ValidDescription);
        result.Value.Category.Should().Be(ServiceCategory.HVAC);
        result.Value.Priority.Should().Be(Priority.High);
        result.Value.Status.Should().Be(WorkOrderStatus.Open);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyDescription_ShouldReturnError(string? description)
    {
        // Act
        var result = WorkOrder.Create(
            _tenantId,
            _propertyId,
            description!,
            ServiceCategory.HVAC,
            Priority.High);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("WorkOrder.IssueDescription");
    }

    [Fact]
    public void Create_ShouldPublishWorkOrderCreatedEvent()
    {
        // Act
        var result = WorkOrder.Create(
            _tenantId,
            _propertyId,
            ValidDescription,
            ServiceCategory.HVAC,
            Priority.High);

        // Assert
        result.Value.DomainEvents.Should().HaveCount(1);
        result.Value.DomainEvents.First().Should().BeOfType<WorkOrderCreatedEvent>();
    }

    [Fact]
    public void TransitionTo_FromOpenToCategorized_ShouldSucceed()
    {
        // Arrange
        var workOrder = CreateValidWorkOrder();

        // Act
        var result = workOrder.TransitionTo(WorkOrderStatus.Categorized);

        // Assert
        result.IsError.Should().BeFalse();
        workOrder.Status.Should().Be(WorkOrderStatus.Categorized);
    }

    [Fact]
    public void TransitionTo_InvalidTransition_ShouldReturnError()
    {
        // Arrange
        var workOrder = CreateValidWorkOrder();

        // Act
        var result = workOrder.TransitionTo(WorkOrderStatus.Completed);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("WorkOrder.Status");
    }

    [Fact]
    public void TransitionTo_FromFinalState_ShouldReturnError()
    {
        // Arrange
        var workOrder = CreateCompletedWorkOrder();
        workOrder.Close();

        // Act
        var result = workOrder.TransitionTo(WorkOrderStatus.Open);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void AddBid_WithValidBid_ShouldSucceed()
    {
        // Arrange
        var workOrder = CreateWorkOrderInBiddingState();
        var bid = VendorBid.Create(
            VendorId.CreateUnique(),
            Money.Create(150, "USD").Value).Value;

        // Act
        var result = workOrder.AddBid(bid);

        // Assert
        result.IsError.Should().BeFalse();
        workOrder.Bids.Should().HaveCount(1);
    }

    [Fact]
    public void AddBid_MoreThan5Bids_ShouldReturnError()
    {
        // Arrange
        var workOrder = CreateWorkOrderInBiddingState();

        // Add 5 bids
        for (int i = 0; i < 5; i++)
        {
            var bid = VendorBid.Create(
                VendorId.CreateUnique(),
                Money.Create(100 + i, "USD").Value).Value;
            workOrder.AddBid(bid);
        }

        // Act - Try to add 6th bid
        var sixthBid = VendorBid.Create(
            VendorId.CreateUnique(),
            Money.Create(200, "USD").Value).Value;
        var result = workOrder.AddBid(sixthBid);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("WorkOrder.AddBid");
        workOrder.Bids.Should().HaveCount(5);
    }

    [Fact]
    public void AddBid_DuplicateVendor_ShouldReturnError()
    {
        // Arrange
        var workOrder = CreateWorkOrderInBiddingState();
        var vendorId = VendorId.CreateUnique();

        var firstBid = VendorBid.Create(vendorId, Money.Create(100, "USD").Value).Value;
        workOrder.AddBid(firstBid);

        // Act - Try to add second bid from same vendor
        var secondBid = VendorBid.Create(vendorId, Money.Create(150, "USD").Value).Value;
        var result = workOrder.AddBid(secondBid);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void AssignVendor_WithValidBid_ShouldSucceed()
    {
        // Arrange
        var workOrder = CreateWorkOrderInBiddingState();
        var vendorId = VendorId.CreateUnique();
        var bid = VendorBid.Create(vendorId, Money.Create(150, "USD").Value).Value;
        workOrder.AddBid(bid);
        var scheduledFor = DateTime.UtcNow.AddDays(1);

        // Act
        var result = workOrder.AssignVendor(vendorId, scheduledFor);

        // Assert
        result.IsError.Should().BeFalse();
        workOrder.AssignedVendorId.Should().Be(vendorId);
        workOrder.ScheduledFor.Should().Be(scheduledFor);
        workOrder.Status.Should().Be(WorkOrderStatus.Scheduled);
    }

    [Fact]
    public void AssignVendor_WithoutBid_ShouldReturnError()
    {
        // Arrange
        var workOrder = CreateWorkOrderInBiddingState();
        var vendorId = VendorId.CreateUnique();
        var scheduledFor = DateTime.UtcNow.AddDays(1);

        // Act
        var result = workOrder.AssignVendor(vendorId, scheduledFor);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Cancel_WithReason_ShouldTransitionToCancelled()
    {
        // Arrange
        var workOrder = CreateValidWorkOrder();

        // Act
        var result = workOrder.Cancel("Tenant resolved issue themselves");

        // Assert
        result.IsError.Should().BeFalse();
        workOrder.Status.Should().Be(WorkOrderStatus.Cancelled);
    }

    [Fact]
    public void UpdatePriority_ShouldChangePriorityAndPublishEvent()
    {
        // Arrange
        var workOrder = CreateValidWorkOrder();
        var initialEventCount = workOrder.DomainEvents.Count;

        // Act
        var result = workOrder.UpdatePriority(Priority.Emergency);

        // Assert
        result.IsError.Should().BeFalse();
        workOrder.Priority.Should().Be(Priority.Emergency);
        workOrder.DomainEvents.Should().HaveCount(initialEventCount + 1);
    }

    private WorkOrder CreateValidWorkOrder()
    {
        return WorkOrder.Create(
            _tenantId,
            _propertyId,
            ValidDescription,
            ServiceCategory.HVAC,
            Priority.High).Value;
    }

    private WorkOrder CreateWorkOrderInBiddingState()
    {
        var workOrder = CreateValidWorkOrder();
        workOrder.TransitionTo(WorkOrderStatus.Categorized);
        workOrder.TransitionTo(WorkOrderStatus.VendorSearch);
        workOrder.TransitionTo(WorkOrderStatus.Bidding);
        return workOrder;
    }

    private WorkOrder CreateCompletedWorkOrder()
    {
        var workOrder = CreateWorkOrderInBiddingState();
        var vendorId = VendorId.CreateUnique();
        var bid = VendorBid.Create(vendorId, Money.Create(150, "USD").Value).Value;
        workOrder.AddBid(bid);
        workOrder.AssignVendor(vendorId, DateTime.UtcNow.AddDays(1));
        workOrder.StartWork();
        workOrder.CompleteWork();
        return workOrder;
    }
}
