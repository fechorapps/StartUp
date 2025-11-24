using DoorX.Domain.Common;
using DoorX.Domain.Properties.ValueObjects;
using DoorX.Domain.Tenants.ValueObjects;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.Events;
using DoorX.Domain.WorkOrders.ValueObjects;
using ErrorOr;

namespace DoorX.Domain.WorkOrders.Entities;

/// <summary>
/// Aggregate Root representing a maintenance work order/service request
/// </summary>
/// <remarks>
/// WorkOrder is the central aggregate in the DoorX domain.
/// It represents the complete lifecycle of a maintenance request from creation to completion.
/// Business Rules:
/// - Maximum 5 vendor bids per work order
/// - Only one vendor can be assigned at a time
/// - Status transitions must follow the valid workflow
/// - Cannot modify cancelled or closed work orders
/// </remarks>
public sealed class WorkOrder : AggregateRoot<WorkOrderId>
{
    private const int MaxBidsAllowed = 5;
    private readonly List<VendorBid> _bids = new();

    private WorkOrder(
        WorkOrderId id,
        TenantId tenantId,
        PropertyId propertyId,
        string issueDescription,
        ServiceCategory category,
        Priority priority) : base(id)
    {
        TenantId = tenantId;
        PropertyId = propertyId;
        IssueDescription = issueDescription;
        Category = category;
        Priority = priority;
        Status = WorkOrderStatus.Open;
    }

    /// <summary>
    /// Tenant who reported the issue
    /// </summary>
    public TenantId TenantId { get; private set; }

    /// <summary>
    /// Property where the issue is located
    /// </summary>
    public PropertyId PropertyId { get; private set; }

    /// <summary>
    /// Description of the maintenance issue
    /// </summary>
    public string IssueDescription { get; private set; }

    /// <summary>
    /// Category of service needed
    /// </summary>
    public ServiceCategory Category { get; private set; }

    /// <summary>
    /// Priority/urgency level
    /// </summary>
    public Priority Priority { get; private set; }

    /// <summary>
    /// Current status in the workflow
    /// </summary>
    public WorkOrderStatus Status { get; private set; }

    /// <summary>
    /// Assigned vendor (if any)
    /// </summary>
    public VendorId? AssignedVendorId { get; private set; }

    /// <summary>
    /// Scheduled date/time for the work (if scheduled)
    /// </summary>
    public DateTime? ScheduledFor { get; private set; }

    /// <summary>
    /// When the work was completed (if completed)
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Vendor bids received (read-only collection)
    /// </summary>
    public IReadOnlyCollection<VendorBid> Bids => _bids.AsReadOnly();

    /// <summary>
    /// Optional external reference ID from PMS
    /// </summary>
    public string? ExternalPmsId { get; private set; }

    /// <summary>
    /// Factory method to create a new WorkOrder
    /// </summary>
    public static ErrorOr<WorkOrder> Create(
        TenantId tenantId,
        PropertyId propertyId,
        string issueDescription,
        ServiceCategory category,
        Priority priority,
        string? externalPmsId = null)
    {
        if (string.IsNullOrWhiteSpace(issueDescription))
            return Error.Validation("WorkOrder.IssueDescription", "Issue description is required");

        var workOrder = new WorkOrder(
            WorkOrderId.CreateUnique(),
            tenantId,
            propertyId,
            issueDescription,
            category,
            priority)
        {
            ExternalPmsId = externalPmsId
        };

        workOrder.AddDomainEvent(new WorkOrderCreatedEvent(workOrder.Id, tenantId, propertyId, category, priority));

        return workOrder;
    }

    /// <summary>
    /// Transitions the work order to a new status
    /// </summary>
    public ErrorOr<Success> TransitionTo(WorkOrderStatus newStatus)
    {
        if (Status.IsFinalState())
            return Error.Validation("WorkOrder.Status", $"Cannot transition from final state: {Status}");

        if (!Status.CanTransitionTo(newStatus))
            return Error.Validation("WorkOrder.Status", $"Invalid transition from {Status} to {newStatus}");

        var oldStatus = Status;
        Status = newStatus;

        AddDomainEvent(new WorkOrderStatusChangedEvent(Id, oldStatus, newStatus));

        return Result.Success;
    }

    /// <summary>
    /// Adds a vendor bid to the work order
    /// </summary>
    public ErrorOr<Success> AddBid(VendorBid bid)
    {
        if (Status == WorkOrderStatus.Cancelled)
            return Error.Validation("WorkOrder.AddBid", "Cannot add bids to a cancelled work order");

        if (Status == WorkOrderStatus.Closed)
            return Error.Validation("WorkOrder.AddBid", "Cannot add bids to a closed work order");

        if (_bids.Count >= MaxBidsAllowed)
            return Error.Validation("WorkOrder.AddBid", $"Maximum {MaxBidsAllowed} bids allowed per work order");

        if (_bids.Any(b => b.VendorId == bid.VendorId))
            return Error.Conflict("WorkOrder.AddBid", "Vendor has already submitted a bid");

        _bids.Add(bid);

        AddDomainEvent(new VendorBidReceivedEvent(Id, bid.VendorId, bid.EstimatedCost));

        return Result.Success;
    }

    /// <summary>
    /// Assigns a vendor to the work order by accepting their bid
    /// </summary>
    public ErrorOr<Success> AssignVendor(VendorId vendorId, DateTime scheduledFor)
    {
        if (Status == WorkOrderStatus.Cancelled)
            return Error.Validation("WorkOrder.AssignVendor", "Cannot assign vendor to a cancelled work order");

        if (Status == WorkOrderStatus.Closed)
            return Error.Validation("WorkOrder.AssignVendor", "Cannot assign vendor to a closed work order");

        var bid = _bids.FirstOrDefault(b => b.VendorId == vendorId);
        if (bid is null)
            return Error.NotFound("WorkOrder.AssignVendor", "No bid found from this vendor");

        if (scheduledFor <= DateTime.UtcNow)
            return Error.Validation("WorkOrder.ScheduledFor", "Scheduled date must be in the future");

        AssignedVendorId = vendorId;
        ScheduledFor = scheduledFor;
        bid.Accept();

        var transitionResult = TransitionTo(WorkOrderStatus.Scheduled);
        if (transitionResult.IsError)
            return transitionResult.Errors;

        AddDomainEvent(new VendorAssignedEvent(Id, vendorId, scheduledFor));

        return Result.Success;
    }

    /// <summary>
    /// Marks the work order as in progress
    /// </summary>
    public ErrorOr<Success> StartWork()
    {
        if (AssignedVendorId is null)
            return Error.Validation("WorkOrder.StartWork", "Cannot start work without an assigned vendor");

        var transitionResult = TransitionTo(WorkOrderStatus.InProgress);
        if (transitionResult.IsError)
            return transitionResult.Errors;

        AddDomainEvent(new WorkStartedEvent(Id, AssignedVendorId));

        return Result.Success;
    }

    /// <summary>
    /// Marks the work order as completed
    /// </summary>
    public ErrorOr<Success> CompleteWork()
    {
        if (AssignedVendorId is null)
            return Error.Validation("WorkOrder.CompleteWork", "Cannot complete work without an assigned vendor");

        var transitionResult = TransitionTo(WorkOrderStatus.Completed);
        if (transitionResult.IsError)
            return transitionResult.Errors;

        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new WorkCompletedEvent(Id, AssignedVendorId, CompletedAt.Value));

        return Result.Success;
    }

    /// <summary>
    /// Closes the work order (tenant confirmed satisfaction)
    /// </summary>
    public ErrorOr<Success> Close()
    {
        if (Status != WorkOrderStatus.Completed)
            return Error.Validation("WorkOrder.Close", "Can only close completed work orders");

        var transitionResult = TransitionTo(WorkOrderStatus.Closed);
        if (transitionResult.IsError)
            return transitionResult.Errors;

        AddDomainEvent(new WorkOrderClosedEvent(Id, TenantId));

        return Result.Success;
    }

    /// <summary>
    /// Cancels the work order
    /// </summary>
    public ErrorOr<Success> Cancel(string reason)
    {
        if (Status == WorkOrderStatus.Closed)
            return Error.Validation("WorkOrder.Cancel", "Cannot cancel a closed work order");

        if (Status == WorkOrderStatus.Cancelled)
            return Error.Validation("WorkOrder.Cancel", "Work order is already cancelled");

        var transitionResult = TransitionTo(WorkOrderStatus.Cancelled);
        if (transitionResult.IsError)
            return transitionResult.Errors;

        AddDomainEvent(new WorkOrderCancelledEvent(Id, reason));

        return Result.Success;
    }

    /// <summary>
    /// Updates the issue description
    /// </summary>
    public ErrorOr<Success> UpdateDescription(string newDescription)
    {
        if (Status.IsFinalState())
            return Error.Validation("WorkOrder.UpdateDescription", "Cannot update description of a finalized work order");

        if (string.IsNullOrWhiteSpace(newDescription))
            return Error.Validation("WorkOrder.IssueDescription", "Issue description is required");

        IssueDescription = newDescription;
        return Result.Success;
    }

    /// <summary>
    /// Updates the priority
    /// </summary>
    public ErrorOr<Success> UpdatePriority(Priority newPriority)
    {
        if (Status.IsFinalState())
            return Error.Validation("WorkOrder.UpdatePriority", "Cannot update priority of a finalized work order");

        if (Priority != newPriority)
        {
            var oldPriority = Priority;
            Priority = newPriority;
            AddDomainEvent(new WorkOrderPriorityChangedEvent(Id, oldPriority, newPriority));
        }

        return Result.Success;
    }

    /// <summary>
    /// Sets the external PMS reference ID
    /// </summary>
    public void SetExternalPmsId(string externalPmsId)
    {
        ExternalPmsId = externalPmsId;
    }

#pragma warning disable CS8618
    private WorkOrder() : base() { }
#pragma warning restore CS8618
}
