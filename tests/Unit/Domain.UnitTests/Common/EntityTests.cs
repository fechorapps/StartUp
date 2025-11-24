using Domain.UnitTests.Common.TestHelpers;

namespace Domain.UnitTests.Common;

/// <summary>
/// Comprehensive unit tests for Entity&lt;TId&gt; base class.
/// Tests cover equality, identity, hash codes, operators, and date tracking.
/// </summary>
public class EntityTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithId_ShouldSetIdCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestEntity(id, "Test");

        // Assert
        entity.Id.Should().Be(id);
    }

    [Fact]
    public void Constructor_WithId_ShouldSetCreatedOnUtc()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var entity = new TestEntity(Guid.NewGuid(), "Test");
        var afterCreation = DateTime.UtcNow;

        // Assert
        entity.CreatedOnUtc.Should().BeOnOrAfter(beforeCreation);
        entity.CreatedOnUtc.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Constructor_WithId_ShouldSetModifiedOnUtcToNull()
    {
        // Arrange & Act
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        // Assert
        entity.ModifiedOnUtc.Should().BeNull();
    }

    [Fact]
    public void ParameterlessConstructor_ShouldBeUsableForSerialization()
    {
        // Arrange & Act
        var entity = TestEntity.CreateForSerialization();

        // Assert
        entity.Should().NotBeNull();
        entity.ModifiedOnUtc.Should().BeNull();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Name1");
        var entity2 = new TestEntity(id, "Name2");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Name1");
        var entity2 = new TestEntity(Guid.NewGuid(), "Name1");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        // Act
        var result = entity.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        // Act
        var result = entity.Equals(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Test");
        var entity2 = new AnotherTestEntity(id);

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Name1");
        object entity2 = new TestEntity(id, "Name2");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        // Act
        var result = entity.Equals((object?)null);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void EqualityOperator_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Name1");
        var entity2 = new TestEntity(id, "Name2");

        // Act
        var result = entity1 == entity2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Test");
        var entity2 = new TestEntity(Guid.NewGuid(), "Test");

        // Act
        var result = entity1 == entity2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_BothNull_ShouldReturnTrue()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;

        // Act
        var result = entity1 == entity2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Test");
        TestEntity? entity2 = null;

        // Act
        var result1 = entity1 == entity2;
        var result2 = entity2 == entity1;

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_WithDifferentId_ShouldReturnTrue()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Test");
        var entity2 = new TestEntity(Guid.NewGuid(), "Test");

        // Act
        var result = entity1 != entity2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_WithSameId_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Name1");
        var entity2 = new TestEntity(id, "Name2");

        // Act
        var result = entity1 != entity2;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HashCode Tests

    [Fact]
    public void GetHashCode_WithSameId_ShouldReturnSameHashCode()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Name1");
        var entity2 = new TestEntity(id, "Name2");

        // Act
        var hashCode1 = entity1.GetHashCode();
        var hashCode2 = entity2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentId_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Test");
        var entity2 = new TestEntity(Guid.NewGuid(), "Test");

        // Act
        var hashCode1 = entity1.GetHashCode();
        var hashCode2 = entity2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_CalledMultipleTimes_ShouldReturnSameValue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        // Act
        var hashCode1 = entity.GetHashCode();
        var hashCode2 = entity.GetHashCode();
        var hashCode3 = entity.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
        hashCode2.Should().Be(hashCode3);
    }

    #endregion

    #region Modified Date Tests

    [Fact]
    public void UpdateModifiedDate_ShouldSetModifiedOnUtc()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");
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
        var entity = new TestEntity(Guid.NewGuid(), "Test");

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
        var entity = new TestEntity(Guid.NewGuid(), "Original");
        var beforeChange = DateTime.UtcNow;

        // Act
        entity.ChangeName("Updated");
        var afterChange = DateTime.UtcNow;

        // Assert
        entity.ModifiedOnUtc.Should().NotBeNull();
        entity.ModifiedOnUtc.Should().BeOnOrAfter(beforeChange);
        entity.ModifiedOnUtc.Should().BeOnOrBefore(afterChange);
    }

    #endregion

    #region Collection Behavior Tests

    [Fact]
    public void Entities_InHashSet_ShouldUseIdentityEquality()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var entity1 = new TestEntity(id1, "Test1");
        var entity2 = new TestEntity(id1, "Test2"); // Same ID, different name
        var entity3 = new TestEntity(id2, "Test3");

        // Act
        var hashSet = new HashSet<TestEntity> { entity1, entity2, entity3 };

        // Assert
        hashSet.Should().HaveCount(2); // entity1 and entity2 are considered equal
        hashSet.Should().Contain(entity1);
        hashSet.Should().Contain(entity3);
    }

    [Fact]
    public void Entities_InDictionary_ShouldUseIdentityAsKey()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Test1");
        var entity2 = new TestEntity(id, "Test2");

        // Act
        var dictionary = new Dictionary<TestEntity, string>
        {
            [entity1] = "Value1"
        };
        dictionary[entity2] = "Value2"; // Should replace Value1

        // Assert
        dictionary.Should().HaveCount(1);
        dictionary[entity1].Should().Be("Value2");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Entities_WithDefaultGuid_ShouldBeEqual()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.Empty, "Test1");
        var entity2 = new TestEntity(Guid.Empty, "Test2");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Entity_CreatedOnUtc_ShouldBeInUtc()
    {
        // Arrange & Act
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        // Assert
        entity.CreatedOnUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Entity_ModifiedOnUtc_WhenSet_ShouldBeInUtc()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        // Act
        entity.TriggerModifiedDateUpdate();

        // Assert
        entity.ModifiedOnUtc.Should().NotBeNull();
        entity.ModifiedOnUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion
}
