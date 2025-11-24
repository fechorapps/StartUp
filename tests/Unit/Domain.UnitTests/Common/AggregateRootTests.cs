using Domain.UnitTests.Common.TestHelpers;
using DoorX.Domain.Common.Interfaces;

namespace Domain.UnitTests.Common;

/// <summary>
/// Comprehensive unit tests for AggregateRoot&lt;TId&gt; base class.
/// Tests cover domain event management, inheritance from Entity, and aggregate behavior.
/// </summary>
public class AggregateRootTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithId_ShouldInheritEntityBehavior()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var aggregate = new TestAggregateRoot(id, "Test");

        // Assert
        aggregate.Id.Should().Be(id);
        aggregate.CreatedOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        aggregate.ModifiedOnUtc.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyDomainEvents()
    {
        // Arrange & Act
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Assert
        aggregate.DomainEvents.Should().NotBeNull();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Domain Event Management Tests

    [Fact]
    public void AddDomainEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");
        var domainEvent = new CustomTestEvent("Test Event");

        // Act
        aggregate.AddTestEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents.Should().Contain(domainEvent);
    }

    [Fact]
    public void AddDomainEvent_MultipleEvents_ShouldAddAllToCollection()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");
        var event1 = new CustomTestEvent("Event 1");
        var event2 = new CustomTestEvent("Event 2");
        var event3 = new CustomTestEvent("Event 3");

        // Act
        aggregate.AddTestEvent(event1);
        aggregate.AddTestEvent(event2);
        aggregate.AddTestEvent(event3);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(3);
        aggregate.DomainEvents.Should().ContainInOrder(event1, event2, event3);
    }

    [Fact]
    public void DomainEvents_ShouldBeReadOnly()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");
        aggregate.AddTestEvent(new CustomTestEvent("Test"));

        // Act
        var events = aggregate.DomainEvents;

        // Assert
        events.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
        events.Should().HaveCount(1);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");
        aggregate.AddTestEvent(new CustomTestEvent("Event 1"));
        aggregate.AddTestEvent(new CustomTestEvent("Event 2"));
        aggregate.AddTestEvent(new CustomTestEvent("Event 3"));

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_WithNoEvents_ShouldNotThrow()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Act
        var act = () => aggregate.ClearDomainEvents();

        // Assert
        act.Should().NotThrow();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");
        aggregate.AddTestEvent(new CustomTestEvent("Test"));

        // Act
        aggregate.ClearDomainEvents();
        var act = () => aggregate.ClearDomainEvents();

        // Assert
        act.Should().NotThrow();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void ChangeName_ShouldRaiseDomainEvent()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Original");

        // Act
        aggregate.ChangeName("Updated");

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents.Should().ContainSingle(e => e is TestNameChangedEvent);
    }

    [Fact]
    public void ChangeName_ShouldIncludeCorrectDataInEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = new TestAggregateRoot(id, "Original");
        var newName = "Updated Name";

        // Act
        aggregate.ChangeName(newName);

        // Assert
        var domainEvent = aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TestNameChangedEvent>().Subject;

        domainEvent.AggregateId.Should().Be(id);
        domainEvent.NewName.Should().Be(newName);
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChangeName_ShouldUpdateModifiedDate()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Original");
        var beforeChange = DateTime.UtcNow;

        // Act
        aggregate.ChangeName("Updated");
        var afterChange = DateTime.UtcNow;

        // Assert
        aggregate.ModifiedOnUtc.Should().NotBeNull();
        aggregate.ModifiedOnUtc.Should().BeOnOrAfter(beforeChange);
        aggregate.ModifiedOnUtc.Should().BeOnOrBefore(afterChange);
    }

    [Fact]
    public void PerformAction_ShouldRaiseDomainEvent()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Act
        aggregate.PerformAction();

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents.Should().ContainSingle(e => e is TestActionPerformedEvent);
    }

    [Fact]
    public void MultipleActions_ShouldAccumulateEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Act
        aggregate.ChangeName("Updated1");
        aggregate.PerformAction();
        aggregate.ChangeName("Updated2");

        // Assert
        aggregate.DomainEvents.Should().HaveCount(3);
        aggregate.DomainEvents.OfType<TestNameChangedEvent>().Should().HaveCount(2);
        aggregate.DomainEvents.OfType<TestActionPerformedEvent>().Should().HaveCount(1);
    }

    #endregion

    #region Domain Event Properties Tests

    [Fact]
    public void DomainEvent_EventId_ShouldBeUnique()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Act
        aggregate.ChangeName("Name1");
        aggregate.ChangeName("Name2");
        aggregate.ChangeName("Name3");

        // Assert
        var eventIds = aggregate.DomainEvents.Select(e => e.EventId).ToList();
        eventIds.Should().OnlyHaveUniqueItems();
        eventIds.Should().AllSatisfy(id => id.Should().NotBeEmpty());
    }

    [Fact]
    public void DomainEvent_OccurredOnUtc_ShouldBeInUtc()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Act
        aggregate.ChangeName("Updated");

        // Assert
        var domainEvent = aggregate.DomainEvents.Single();
        domainEvent.OccurredOnUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void DomainEvent_OccurredOnUtc_ShouldBeRecentTime()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");
        var beforeAction = DateTime.UtcNow;

        // Act
        aggregate.PerformAction();
        var afterAction = DateTime.UtcNow;

        // Assert
        var domainEvent = aggregate.DomainEvents.Single();
        domainEvent.OccurredOnUtc.Should().BeOnOrAfter(beforeAction);
        domainEvent.OccurredOnUtc.Should().BeOnOrBefore(afterAction);
    }

    #endregion

    #region Entity Inheritance Tests

    [Fact]
    public void AggregateRoot_ShouldInheritEntityEquality()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate1 = new TestAggregateRoot(id, "Name1");
        var aggregate2 = new TestAggregateRoot(id, "Name2");

        // Act
        var result = aggregate1.Equals(aggregate2);

        // Assert
        result.Should().BeTrue(); // Same ID means equal
    }

    [Fact]
    public void AggregateRoot_ShouldInheritEntityHashCode()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate1 = new TestAggregateRoot(id, "Name1");
        var aggregate2 = new TestAggregateRoot(id, "Name2");

        // Act
        var hashCode1 = aggregate1.GetHashCode();
        var hashCode2 = aggregate2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void AggregateRoot_WithDifferentIds_ShouldNotBeEqual()
    {
        // Arrange
        var aggregate1 = new TestAggregateRoot(Guid.NewGuid(), "Test");
        var aggregate2 = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Act
        var result = aggregate1.Equals(aggregate2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AggregateRoot_EqualityOperators_ShouldWork()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate1 = new TestAggregateRoot(id, "Name1");
        var aggregate2 = new TestAggregateRoot(id, "Name2");
        var aggregate3 = new TestAggregateRoot(Guid.NewGuid(), "Name1");

        // Act & Assert
        (aggregate1 == aggregate2).Should().BeTrue();
        (aggregate1 != aggregate3).Should().BeTrue();
    }

    #endregion

    #region Event Workflow Simulation Tests

    [Fact]
    public void EventWorkflow_AddEventsAndClear_ShouldSimulatePublishPattern()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Act - Simulate business operations
        aggregate.ChangeName("Step1");
        aggregate.PerformAction();
        aggregate.ChangeName("Step2");

        // Assert - Events accumulated
        aggregate.DomainEvents.Should().HaveCount(3);

        // Act - Simulate event publishing and clearing
        var eventsToPublish = aggregate.DomainEvents.ToList();
        aggregate.ClearDomainEvents();

        // Assert - Events cleared after publishing
        eventsToPublish.Should().HaveCount(3);
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void EventWorkflow_MultipleOperationsCycles_ShouldWorkCorrectly()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Act - Cycle 1
        aggregate.ChangeName("Cycle1");
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.ClearDomainEvents();

        // Act - Cycle 2
        aggregate.PerformAction();
        aggregate.PerformAction();
        aggregate.DomainEvents.Should().HaveCount(2);
        aggregate.ClearDomainEvents();

        // Act - Cycle 3
        aggregate.ChangeName("Cycle3");
        aggregate.PerformAction();

        // Assert
        aggregate.DomainEvents.Should().HaveCount(2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AggregateRoot_WithManyEvents_ShouldHandleCorrectly()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");

        // Act - Add many events
        for (int i = 0; i < 100; i++)
        {
            aggregate.AddTestEvent(new CustomTestEvent($"Event {i}"));
        }

        // Assert
        aggregate.DomainEvents.Should().HaveCount(100);
        aggregate.DomainEvents.Select(e => e.EventId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AggregateRoot_DomainEventsCollection_ShouldPreserveOrder()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");
        var events = new List<IDomainEvent>
        {
            new CustomTestEvent("First"),
            new CustomTestEvent("Second"),
            new CustomTestEvent("Third")
        };

        // Act
        foreach (var evt in events)
        {
            aggregate.AddTestEvent(evt);
        }

        // Assert
        aggregate.DomainEvents.Should().ContainInOrder(events);
    }

    [Fact]
    public void AggregateRoot_AfterClearing_CanAddNewEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");
        aggregate.AddTestEvent(new CustomTestEvent("Initial"));
        aggregate.ClearDomainEvents();

        // Act
        aggregate.AddTestEvent(new CustomTestEvent("After Clear"));

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        var evt = aggregate.DomainEvents.Single().Should().BeOfType<CustomTestEvent>().Subject;
        evt.Message.Should().Be("After Clear");
    }

    [Fact]
    public void AggregateRoot_Version_ShouldTrackChanges()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid(), "Test");
        var initialVersion = aggregate.Version;

        // Act
        aggregate.ChangeName("Updated");
        var afterNameChange = aggregate.Version;

        aggregate.PerformAction();
        var afterAction = aggregate.Version;

        // Assert
        initialVersion.Should().Be(1);
        afterNameChange.Should().Be(2);
        afterAction.Should().Be(3);
    }

    #endregion

    #region Concurrency and Isolation Tests

    [Fact]
    public void DomainEvents_FromDifferentAggregates_ShouldBeIsolated()
    {
        // Arrange
        var aggregate1 = new TestAggregateRoot(Guid.NewGuid(), "Aggregate1");
        var aggregate2 = new TestAggregateRoot(Guid.NewGuid(), "Aggregate2");

        // Act
        aggregate1.ChangeName("Updated1");
        aggregate2.PerformAction();
        aggregate2.PerformAction();

        // Assert
        aggregate1.DomainEvents.Should().HaveCount(1);
        aggregate2.DomainEvents.Should().HaveCount(2);
    }

    [Fact]
    public void ClearDomainEvents_OnOneAggregate_ShouldNotAffectOthers()
    {
        // Arrange
        var aggregate1 = new TestAggregateRoot(Guid.NewGuid(), "Aggregate1");
        var aggregate2 = new TestAggregateRoot(Guid.NewGuid(), "Aggregate2");

        aggregate1.ChangeName("Updated1");
        aggregate2.ChangeName("Updated2");

        // Act
        aggregate1.ClearDomainEvents();

        // Assert
        aggregate1.DomainEvents.Should().BeEmpty();
        aggregate2.DomainEvents.Should().HaveCount(1);
    }

    #endregion
}
