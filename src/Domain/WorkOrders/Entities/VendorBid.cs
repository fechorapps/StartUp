using DoorX.Domain.Common;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;
using ErrorOr;

namespace DoorX.Domain.WorkOrders.Entities;

/// <summary>
/// Entity representing a vendor's bid/quote for a work order
/// </summary>
/// <remarks>
/// VendorBid is a child entity within the WorkOrder aggregate.
/// It cannot exist independently and has no repository of its own.
/// </remarks>
public sealed class VendorBid : Entity<VendorBidId>
{
    private VendorBid(
        VendorBidId id,
        VendorId vendorId,
        Money estimatedCost,
        DateTime? proposedDate,
        string? notes) : base(id)
    {
        VendorId = vendorId;
        EstimatedCost = estimatedCost;
        ProposedDate = proposedDate;
        Notes = notes;
        SubmittedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Vendor who submitted the bid
    /// </summary>
    public VendorId VendorId { get; private set; }

    /// <summary>
    /// Estimated cost for the work
    /// </summary>
    public Money EstimatedCost { get; private set; }

    /// <summary>
    /// Proposed date/time for the work
    /// </summary>
    public DateTime? ProposedDate { get; private set; }

    /// <summary>
    /// Additional notes from the vendor
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// When the bid was submitted
    /// </summary>
    public DateTime SubmittedAt { get; private set; }

    /// <summary>
    /// Whether this bid was accepted
    /// </summary>
    public bool IsAccepted { get; private set; }

    /// <summary>
    /// Factory method to create a new VendorBid
    /// </summary>
    public static ErrorOr<VendorBid> Create(
        VendorId vendorId,
        Money estimatedCost,
        DateTime? proposedDate = null,
        string? notes = null)
    {
        var bid = new VendorBid(
            VendorBidId.CreateUnique(),
            vendorId,
            estimatedCost,
            proposedDate,
            notes);

        return bid;
    }

    /// <summary>
    /// Marks this bid as accepted
    /// </summary>
    internal void Accept()
    {
        IsAccepted = true;
    }

    /// <summary>
    /// Updates the bid information
    /// </summary>
    public ErrorOr<Success> Update(Money estimatedCost, DateTime? proposedDate, string? notes)
    {
        if (IsAccepted)
            return Error.Validation("VendorBid.Update", "Cannot update an accepted bid");

        EstimatedCost = estimatedCost;
        ProposedDate = proposedDate;
        Notes = notes;

        return Result.Success;
    }

#pragma warning disable CS8618
    private VendorBid() : base() { }
#pragma warning restore CS8618
}
