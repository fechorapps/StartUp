using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.Entities;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.UnitTests.Entities;

public class VendorBidTests
{
    private readonly VendorId _vendorId = VendorId.CreateUnique();

    [Fact]
    public void Create_WithMinimumRequiredData_ShouldReturnBid()
    {
        // Arrange
        var cost = Money.Create(100, "USD").Value;

        // Act
        var result = VendorBid.Create(_vendorId, cost);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.VendorId.Should().Be(_vendorId);
        result.Value.EstimatedCost.Should().Be(cost);
        result.Value.ProposedDate.Should().BeNull();
        result.Value.Notes.Should().BeNull();
        result.Value.IsAccepted.Should().BeFalse();
        result.Value.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithAllData_ShouldReturnBid()
    {
        // Arrange
        var cost = Money.Create(250, "USD").Value;
        var proposedDate = DateTime.UtcNow.AddDays(2);
        var notes = "I can fix this quickly";

        // Act
        var result = VendorBid.Create(_vendorId, cost, proposedDate, notes);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.VendorId.Should().Be(_vendorId);
        result.Value.EstimatedCost.Should().Be(cost);
        result.Value.ProposedDate.Should().Be(proposedDate);
        result.Value.Notes.Should().Be(notes);
    }

    [Fact]
    public void Create_WithProposedDateOnly_ShouldSucceed()
    {
        // Arrange
        var cost = Money.Create(150, "USD").Value;
        var proposedDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = VendorBid.Create(_vendorId, cost, proposedDate);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.ProposedDate.Should().Be(proposedDate);
        result.Value.Notes.Should().BeNull();
    }

    [Fact]
    public void Create_WithNotesOnly_ShouldSucceed()
    {
        // Arrange
        var cost = Money.Create(200, "USD").Value;
        var notes = "Available anytime";

        // Act
        var result = VendorBid.Create(_vendorId, cost, notes: notes);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Notes.Should().Be(notes);
        result.Value.ProposedDate.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleBids_ShouldHaveDifferentIds()
    {
        // Arrange
        var cost = Money.Create(100, "USD").Value;

        // Act
        var bid1 = VendorBid.Create(_vendorId, cost).Value;
        var bid2 = VendorBid.Create(_vendorId, cost).Value;

        // Assert
        bid1.Id.Should().NotBe(bid2.Id);
    }

    [Fact]
    public void Update_UnacceptedBid_ShouldSucceed()
    {
        // Arrange
        var bid = VendorBid.Create(_vendorId, Money.Create(100, "USD").Value).Value;
        var newCost = Money.Create(120, "USD").Value;
        var newDate = DateTime.UtcNow.AddDays(3);
        var newNotes = "Updated estimate";

        // Act
        var result = bid.Update(newCost, newDate, newNotes);

        // Assert
        result.IsError.Should().BeFalse();
        bid.EstimatedCost.Should().Be(newCost);
        bid.ProposedDate.Should().Be(newDate);
        bid.Notes.Should().Be(newNotes);
    }

    [Fact]
    public void Update_CanRemoveProposedDate()
    {
        // Arrange
        var bid = VendorBid.Create(
            _vendorId,
            Money.Create(100, "USD").Value,
            DateTime.UtcNow.AddDays(1),
            "Notes").Value;

        // Act
        var result = bid.Update(Money.Create(150, "USD").Value, null, "Updated notes");

        // Assert
        result.IsError.Should().BeFalse();
        bid.ProposedDate.Should().BeNull();
    }

    [Fact]
    public void Update_CanRemoveNotes()
    {
        // Arrange
        var bid = VendorBid.Create(
            _vendorId,
            Money.Create(100, "USD").Value,
            DateTime.UtcNow.AddDays(1),
            "Original notes").Value;

        // Act
        var result = bid.Update(Money.Create(150, "USD").Value, DateTime.UtcNow.AddDays(2), null);

        // Assert
        result.IsError.Should().BeFalse();
        bid.Notes.Should().BeNull();
    }

    [Fact]
    public void Update_CanChangeOnlyCost()
    {
        // Arrange
        var originalDate = DateTime.UtcNow.AddDays(1);
        var originalNotes = "Original notes";
        var bid = VendorBid.Create(
            _vendorId,
            Money.Create(100, "USD").Value,
            originalDate,
            originalNotes).Value;

        var newCost = Money.Create(200, "USD").Value;

        // Act
        var result = bid.Update(newCost, originalDate, originalNotes);

        // Assert
        result.IsError.Should().BeFalse();
        bid.EstimatedCost.Should().Be(newCost);
        bid.ProposedDate.Should().Be(originalDate);
        bid.Notes.Should().Be(originalNotes);
    }

    [Fact]
    public void SubmittedAt_ShouldBeSetOnCreation()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var bid = VendorBid.Create(_vendorId, Money.Create(100, "USD").Value).Value;
        var afterCreate = DateTime.UtcNow;

        // Assert
        bid.SubmittedAt.Should().BeOnOrAfter(beforeCreate);
        bid.SubmittedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void IsAccepted_InitiallyFalse()
    {
        // Act
        var bid = VendorBid.Create(_vendorId, Money.Create(100, "USD").Value).Value;

        // Assert
        bid.IsAccepted.Should().BeFalse();
    }

    [Fact]
    public void VendorId_ShouldBeImmutable()
    {
        // Arrange
        var originalVendorId = VendorId.CreateUnique();
        var bid = VendorBid.Create(originalVendorId, Money.Create(100, "USD").Value).Value;

        // Assert
        bid.VendorId.Should().Be(originalVendorId);
    }

    [Fact]
    public void Notes_CanContainLongText()
    {
        // Arrange
        var longNotes = string.Join(" ", Enumerable.Repeat("This is a detailed note.", 50));
        var cost = Money.Create(100, "USD").Value;

        // Act
        var result = VendorBid.Create(_vendorId, cost, notes: longNotes);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Notes.Should().Be(longNotes);
    }

    [Fact]
    public void Notes_CanContainSpecialCharacters()
    {
        // Arrange
        var specialNotes = "Cost includes: labor ($50), parts ($30), tax (10%) & service fee";
        var cost = Money.Create(100, "USD").Value;

        // Act
        var result = VendorBid.Create(_vendorId, cost, notes: specialNotes);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Notes.Should().Be(specialNotes);
    }

    [Fact]
    public void ProposedDate_CanBeInFuture()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddMonths(1);
        var cost = Money.Create(100, "USD").Value;

        // Act
        var result = VendorBid.Create(_vendorId, cost, futureDate);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.ProposedDate.Should().Be(futureDate);
    }

    [Fact]
    public void ProposedDate_CanBeInPast()
    {
        // Arrange - Some edge cases might require past dates (e.g., emergency repairs already started)
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var cost = Money.Create(100, "USD").Value;

        // Act
        var result = VendorBid.Create(_vendorId, cost, pastDate);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.ProposedDate.Should().Be(pastDate);
    }

    [Fact]
    public void Equality_SameId_ShouldBeEqual()
    {
        // Arrange
        var bid = VendorBid.Create(_vendorId, Money.Create(100, "USD").Value).Value;

        // Assert
        bid.Should().Be(bid);
        (bid == bid).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentIds_ShouldNotBeEqual()
    {
        // Arrange
        var cost = Money.Create(100, "USD").Value;
        var bid1 = VendorBid.Create(_vendorId, cost).Value;
        var bid2 = VendorBid.Create(_vendorId, cost).Value;

        // Assert
        bid1.Should().NotBe(bid2);
        (bid1 == bid2).Should().BeFalse();
    }
}
