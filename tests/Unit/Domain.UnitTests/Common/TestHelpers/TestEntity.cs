using DoorX.Domain.Common;

namespace Domain.UnitTests.Common.TestHelpers;

/// <summary>
/// Concrete implementation of Entity for testing purposes.
/// Tests basic entity identity without audit properties.
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

    // Factory method to test parameterless constructor
    public static TestEntity CreateForSerialization()
    {
        return new TestEntity();
    }

    public void ChangeName(string newName)
    {
        Name = newName;
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

/// <summary>
/// Concrete implementation of AuditableEntity for testing purposes.
/// Tests entity with audit properties (CreatedOnUtc, ModifiedOnUtc).
/// </summary>
internal sealed class TestAuditableEntity : AuditableEntity<Guid>
{
    public string Name { get; private set; }

    public TestAuditableEntity(Guid id, string name) : base(id)
    {
        Name = name;
    }

    // Parameterless constructor for testing serialization scenarios
    private TestAuditableEntity() : base()
    {
        Name = string.Empty;
    }

    // Factory method to test parameterless constructor
    public static TestAuditableEntity CreateForSerialization()
    {
        return new TestAuditableEntity();
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
