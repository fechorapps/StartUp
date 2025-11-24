using DoorX.Domain.Common;

namespace Domain.UnitTests.Common.TestHelpers;

/// <summary>
/// Concrete implementation of Entity for testing purposes.
/// </summary>
internal sealed class TestEntity : Entity<Guid>
{
    public string Name { get; private set; }

    public TestEntity(Guid id, string name) : base(id)
    {
        Name = name;
    }

    // Parameterless constructor for testing serialization scenarios
    private TestEntity() : base()
    {
        Name = string.Empty;
    }

    public void ChangeName(string newName)
    {
        Name = newName;
        UpdateModifiedDate();
    }

    public void TriggerModifiedDateUpdate()
    {
        UpdateModifiedDate();
    }
}

/// <summary>
/// Another concrete entity type for testing type-based equality.
/// </summary>
internal sealed class AnotherTestEntity : Entity<Guid>
{
    public AnotherTestEntity(Guid id) : base(id)
    {
    }

    private AnotherTestEntity() : base()
    {
    }
}
