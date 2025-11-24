using Domain.UnitTests.Common.TestHelpers;

namespace Domain.UnitTests.Common;

/// <summary>
/// Comprehensive unit tests for AuditableEntity&lt;TId&gt; base class.
/// Tests cover audit properties (CreatedOnUtc, ModifiedOnUtc) and inheritance from Entity.
/// </summary>
public class AuditableEntityTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithId_ShouldSetIdCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestAuditableEntity(id, "Test");

        // Assert
        entity.Id.Should().Be(id);
    }

    [Fact]
    public void Constructor_WithId_ShouldSetCreatedOnUtc()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var entity = new TestAuditableEntity(Guid.NewGuid(), "Test");
        var afterCreation = DateTime.UtcNow;

        // Assert
        entity.CreatedOnUtc.Should().BeOnOrAfter(beforeCreation);
        entity.CreatedOnUtc.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Constructor_WithId_ShouldSetModifiedOnUtcToNull()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity(Guid.NewGuid(), "Test");

        // Assert
        entity.ModifiedOnUtc.Should().BeNull();
    }

    [Fact]
    public void ParameterlessConstructor_ShouldBeUsableForSerialization()
    {
        // Arrange & Act
        var entity = TestAuditableEntity.CreateForSerialization();

        // Assert
        entity.Should().NotBeNull();
        entity.ModifiedOnUtc.Should().BeNull();
    }

    #endregion

    #region Audit Properties Tests

    [Fact]
    public void UpdateModifiedDate_ShouldSetModifiedOnUtc()
    {
        // Arrange
        var entity = new TestAuditableEntity(Guid.NewGuid(), "Test");
        var beforeUpdate = DateTime.UtcNow;

        // Act
        entity.TriggerModifiedDateUpdate();
        var afterUpdate = DateTime.UtcNow;

        // Assert
        entity.ModifiedOnUtc.Should().NotBeNull();
        entity.ModifiedOnUtc.Should().BeOnOrAfter(beforeUpdate);
        entity.ModifiedOnUtc.Should().BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public void UpdateModifiedDate_CalledMultipleTimes_ShouldUpdateToLatestTime()
    {
        // Arrange
        var entity = new TestAuditableEntity(Guid.NewGuid(), "Test");

        // Act
        entity.TriggerModifiedDateUpdate();
        var firstModified = entity.ModifiedOnUtc;

        Thread.Sleep(10); // Ensure time difference

        entity.TriggerModifiedDateUpdate();
        var secondModified = entity.ModifiedOnUtc;

        // Assert
        firstModified.Should().NotBeNull();
        secondModified.Should().NotBeNull();
        secondModified!.Value.Should().BeAfter(firstModified!.Value);
    }

    [Fact]
    public void ChangeName_ShouldUpdateModifiedDate()
    {
        // Arrange
        var entity = new TestAuditableEntity(Guid.NewGuid(), "Original");
        var beforeChange = DateTime.UtcNow;

        // Act
        entity.ChangeName("Updated");
        var afterChange = DateTime.UtcNow;

        // Assert
        entity.ModifiedOnUtc.Should().NotBeNull();
        entity.ModifiedOnUtc.Should().BeOnOrAfter(beforeChange);
        entity.ModifiedOnUtc.Should().BeOnOrBefore(afterChange);
    }

    [Fact]
    public void Entity_CreatedOnUtc_ShouldBeInUtc()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity(Guid.NewGuid(), "Test");

        // Assert
        entity.CreatedOnUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Entity_ModifiedOnUtc_WhenSet_ShouldBeInUtc()
    {
        // Arrange
        var entity = new TestAuditableEntity(Guid.NewGuid(), "Test");

        // Act
        entity.TriggerModifiedDateUpdate();

        // Assert
        entity.ModifiedOnUtc.Should().NotBeNull();
        entity.ModifiedOnUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region Entity Inheritance Tests

    [Fact]
    public void AuditableEntity_ShouldInheritEntityEquality()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestAuditableEntity(id, "Name1");
        var entity2 = new TestAuditableEntity(id, "Name2");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeTrue(); // Same ID means equal
    }

    [Fact]
    public void AuditableEntity_ShouldInheritEntityHashCode()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestAuditableEntity(id, "Name1");
        var entity2 = new TestAuditableEntity(id, "Name2");

        // Act
        var hashCode1 = entity1.GetHashCode();
        var hashCode2 = entity2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void AuditableEntity_WithDifferentIds_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestAuditableEntity(Guid.NewGuid(), "Test");
        var entity2 = new TestAuditableEntity(Guid.NewGuid(), "Test");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AuditableEntity_EqualityOperators_ShouldWork()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestAuditableEntity(id, "Name1");
        var entity2 = new TestAuditableEntity(id, "Name2");
        var entity3 = new TestAuditableEntity(Guid.NewGuid(), "Name1");

        // Act & Assert
        (entity1 == entity2).Should().BeTrue();
        (entity1 != entity3).Should().BeTrue();
    }

    #endregion

    #region Collection Behavior Tests

    [Fact]
    public void AuditableEntities_InHashSet_ShouldUseIdentityEquality()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var entity1 = new TestAuditableEntity(id1, "Test1");
        var entity2 = new TestAuditableEntity(id1, "Test2"); // Same ID, different name
        var entity3 = new TestAuditableEntity(id2, "Test3");

        // Act
        var hashSet = new HashSet<TestAuditableEntity> { entity1, entity2, entity3 };

        // Assert
        hashSet.Should().HaveCount(2); // entity1 and entity2 are considered equal
        hashSet.Should().Contain(entity1);
        hashSet.Should().Contain(entity3);
    }

    [Fact]
    public void AuditableEntities_InDictionary_ShouldUseIdentityAsKey()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestAuditableEntity(id, "Test1");
        var entity2 = new TestAuditableEntity(id, "Test2");

        // Act
        var dictionary = new Dictionary<TestAuditableEntity, string>
        {
            [entity1] = "Value1"
        };
        dictionary[entity2] = "Value2"; // Should replace Value1

        // Assert
        dictionary.Should().HaveCount(1);
        dictionary[entity1].Should().Be("Value2");
    }

    #endregion

    #region Audit Workflow Tests

    [Fact]
    public void AuditWorkflow_CreateAndModify_ShouldTrackDatesCorrectly()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        var entity = new TestAuditableEntity(Guid.NewGuid(), "Original");
        var afterCreation = DateTime.UtcNow;

        // Assert - Creation
        entity.CreatedOnUtc.Should().BeOnOrAfter(beforeCreation);
        entity.CreatedOnUtc.Should().BeOnOrBefore(afterCreation);
        entity.ModifiedOnUtc.Should().BeNull();

        // Act - First modification
        Thread.Sleep(10); // Ensure time difference
        var beforeFirstModification = DateTime.UtcNow;
        entity.ChangeName("Modified1");
        var afterFirstModification = DateTime.UtcNow;

        // Assert - First modification
        entity.ModifiedOnUtc.Should().NotBeNull();
        entity.ModifiedOnUtc.Should().BeOnOrAfter(beforeFirstModification);
        entity.ModifiedOnUtc.Should().BeOnOrBefore(afterFirstModification);
        entity.ModifiedOnUtc.Should().BeAfter(entity.CreatedOnUtc);

        // Act - Second modification
        Thread.Sleep(10); // Ensure time difference
        var firstModifiedDate = entity.ModifiedOnUtc;
        var beforeSecondModification = DateTime.UtcNow;
        entity.ChangeName("Modified2");
        var afterSecondModification = DateTime.UtcNow;

        // Assert - Second modification
        entity.ModifiedOnUtc.Should().NotBeNull();
        entity.ModifiedOnUtc.Should().BeOnOrAfter(beforeSecondModification);
        entity.ModifiedOnUtc.Should().BeOnOrBefore(afterSecondModification);
        entity.ModifiedOnUtc.Should().BeAfter(firstModifiedDate!.Value);
    }

    [Fact]
    public void AuditWorkflow_MultipleEntities_ShouldHaveIndependentAuditDates()
    {
        // Arrange & Act
        var entity1 = new TestAuditableEntity(Guid.NewGuid(), "Entity1");
        Thread.Sleep(10);
        var entity2 = new TestAuditableEntity(Guid.NewGuid(), "Entity2");

        entity1.ChangeName("Modified1");
        Thread.Sleep(10);
        entity2.ChangeName("Modified2");

        // Assert
        entity1.CreatedOnUtc.Should().BeBefore(entity2.CreatedOnUtc);
        entity1.ModifiedOnUtc.Should().NotBeNull();
        entity2.ModifiedOnUtc.Should().NotBeNull();
        entity1.ModifiedOnUtc!.Value.Should().BeBefore(entity2.ModifiedOnUtc!.Value);
    }

    #endregion
}
