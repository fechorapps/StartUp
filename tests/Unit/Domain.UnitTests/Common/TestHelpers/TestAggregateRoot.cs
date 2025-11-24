using DoorX.Domain.Common;
using DoorX.Domain.Common.Interfaces;

namespace Domain.UnitTests.Common.TestHelpers;

/// <summary>
/// Concrete implementation of AggregateRoot for testing purposes.
/// </summary>
internal sealed class TestAggregateRoot : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public int Version { get; private set; }

    public TestAggregateRoot(Guid id, string name) : base(id)
    {
        Name = name;
        Version = 1;
    }

    private TestAggregateRoot() : base()
    {
        Name = string.Empty;
        Version = 0;
    }

    public void ChangeName(string newName)
    {
        Name = newName;
        Version++;
        UpdateModifiedDate();
        AddDomainEvent(new TestNameChangedEvent(Id, newName));
    }

    public void PerformAction()
    {
        Version++;
        AddDomainEvent(new TestActionPerformedEvent(Id));
    }

    public void AddTestEvent(IDomainEvent domainEvent)
    {
        AddDomainEvent(domainEvent);
    }
}

/// <summary>
/// Test domain event for name changes.
/// </summary>
internal sealed record TestNameChangedEvent(Guid AggregateId, string NewName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

/// <summary>
/// Test domain event for generic actions.
/// </summary>
internal sealed record TestActionPerformedEvent(Guid AggregateId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

/// <summary>
/// Custom domain event for testing.
/// </summary>
internal sealed record CustomTestEvent(string Message) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
