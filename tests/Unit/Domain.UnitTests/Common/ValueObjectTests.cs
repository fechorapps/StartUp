using Domain.UnitTests.Common.TestHelpers;

namespace Domain.UnitTests.Common;

/// <summary>
/// Comprehensive unit tests for ValueObject base class.
/// Tests cover value-based equality, immutability, hash codes, and operators.
/// </summary>
public class ValueObjectTests
{
    #region Equality Tests - Value-Based Equality

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act
        var result = address1.Equals(address2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("456 Oak Ave", "Springfield", "12345");

        // Act
        var result = address1.Equals(address2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithOneComponentDifferent_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "54321");

        // Act
        var result = address1.Equals(address2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");

        // Act
        var result = address.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");

        // Act
        var result = address.Equals(address);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");
        var money = new Money(100m, "USD");

        // Act
        var result = address.Equals(money);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        object address2 = new Address("123 Main St", "Springfield", "12345");

        // Act
        var result = address1.Equals(address2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");

        // Act
        var result = address.Equals((object?)null);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Equality Tests - Nullable Components

    [Fact]
    public void Equals_WithNullableComponents_BothNull_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(100m, "USD", null);
        var money2 = new Money(100m, "USD", null);

        // Act
        var result = money1.Equals(money2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNullableComponents_OneNull_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(100m, "USD", "Payment");
        var money2 = new Money(100m, "USD", null);

        // Act
        var result = money1.Equals(money2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNullableComponents_BothNonNull_ShouldCompareValues()
    {
        // Arrange
        var money1 = new Money(100m, "USD", "Payment");
        var money2 = new Money(100m, "USD", "Payment");
        var money3 = new Money(100m, "USD", "Refund");

        // Act
        var result1 = money1.Equals(money2);
        var result2 = money1.Equals(money3);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void EqualityOperator_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act
        var result = address1 == address2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("456 Oak Ave", "Springfield", "12345");

        // Act
        var result = address1 == address2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_BothNull_ShouldReturnTrue()
    {
        // Arrange
        Address? address1 = null;
        Address? address2 = null;

        // Act
        var result = address1 == address2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        Address? address2 = null;

        // Act
        var result1 = address1 == address2;
        var result2 = address2 == address1;

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_WithDifferentValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("456 Oak Ave", "Springfield", "12345");

        // Act
        var result = address1 != address2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_WithSameValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act
        var result = address1 != address2;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HashCode Tests

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act
        var hashCode1 = address1.GetHashCode();
        var hashCode2 = address2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("456 Oak Ave", "Springfield", "12345");

        // Act
        var hashCode1 = address1.GetHashCode();
        var hashCode2 = address2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_CalledMultipleTimes_ShouldReturnSameValue()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");

        // Act
        var hashCode1 = address.GetHashCode();
        var hashCode2 = address.GetHashCode();
        var hashCode3 = address.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
        hashCode2.Should().Be(hashCode3);
    }

    [Fact]
    public void GetHashCode_WithNullComponents_ShouldNotThrow()
    {
        // Arrange
        var money = new Money(100m, "USD", null);

        // Act
        var act = () => money.GetHashCode();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GetHashCode_WithAllNullComponents_ShouldReturnConsistentValue()
    {
        // Arrange
        var money1 = new Money(100m, "USD", null);
        var money2 = new Money(100m, "USD", null);

        // Act
        var hashCode1 = money1.GetHashCode();
        var hashCode2 = money2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    #endregion

    #region Collection Behavior Tests

    [Fact]
    public void ValueObjects_InHashSet_ShouldUseValueEquality()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345"); // Same values
        var address3 = new Address("456 Oak Ave", "Springfield", "54321");

        // Act
        var hashSet = new HashSet<Address> { address1, address2, address3 };

        // Assert
        hashSet.Should().HaveCount(2); // address1 and address2 are equal by value
        hashSet.Should().Contain(address3);
    }

    [Fact]
    public void ValueObjects_InDictionary_ShouldUseValueAsKey()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act
        var dictionary = new Dictionary<Address, string>
        {
            [address1] = "Value1"
        };
        dictionary[address2] = "Value2"; // Should replace Value1 because they're equal

        // Assert
        dictionary.Should().HaveCount(1);
        dictionary[address1].Should().Be("Value2");
    }

    [Fact]
    public void ValueObjects_AsList_CanContainDuplicateValues()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act
        var list = new List<Address> { address1, address2 };

        // Assert
        list.Should().HaveCount(2);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void ValueObject_Properties_ShouldBeReadOnly()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");

        // Assert
        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("Springfield");
        address.ZipCode.Should().Be("12345");

        // Properties should be init-only or have no setters
        // This is enforced at compile time, but we verify the values don't change
        var originalStreet = address.Street;
        var originalCity = address.City;
        var originalZipCode = address.ZipCode;

        // After various operations, values should remain the same
        _ = address.GetHashCode();
        _ = address.Equals(new Address("Different", "Values", "00000"));

        address.Street.Should().Be(originalStreet);
        address.City.Should().Be(originalCity);
        address.ZipCode.Should().Be(originalZipCode);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ValueObject_WithEmptyStrings_ShouldBeEqual()
    {
        // Arrange
        var address1 = new Address("", "", "");
        var address2 = new Address("", "", "");

        // Act
        var result = address1.Equals(address2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValueObject_WithNoComponents_ShouldBeEqual()
    {
        // Arrange
        var empty1 = new EmptyValueObject();
        var empty2 = new EmptyValueObject();

        // Act
        var result = empty1.Equals(empty2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValueObject_WithNoComponents_ShouldHaveConsistentHashCode()
    {
        // Arrange
        var empty1 = new EmptyValueObject();
        var empty2 = new EmptyValueObject();

        // Act
        var hashCode1 = empty1.GetHashCode();
        var hashCode2 = empty2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
        hashCode1.Should().Be(1); // Expected base hash code for empty sequence
    }

    [Fact]
    public void ValueObject_WithDecimalValues_ShouldCompareCorrectly()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "USD");
        var money3 = new Money(100.51m, "USD");

        // Act
        var result1 = money1.Equals(money2);
        var result2 = money1.Equals(money3);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Fact]
    public void ValueObject_WithCaseSensitiveStrings_ShouldBeEqual()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "usd");

        // Act
        var result = money1.Equals(money2);

        // Assert
        result.Should().BeFalse(); // String comparison is case-sensitive
    }

    [Fact]
    public void ValueObject_ComplexEquality_WithMultipleComponents()
    {
        // Arrange
        var money1a = new Money(100m, "USD", "Payment 1");
        var money1b = new Money(100m, "USD", "Payment 1");
        var money2 = new Money(100m, "EUR", "Payment 1");
        var money3 = new Money(200m, "USD", "Payment 1");
        var money4 = new Money(100m, "USD", "Payment 2");

        // Act & Assert
        money1a.Equals(money1b).Should().BeTrue();  // All components match
        money1a.Equals(money2).Should().BeFalse();  // Different currency
        money1a.Equals(money3).Should().BeFalse();  // Different amount
        money1a.Equals(money4).Should().BeFalse();  // Different description
    }

    #endregion

    #region Reference Equality vs Value Equality

    [Fact]
    public void ValueObjects_WithSameValues_ShouldNotHaveSameReference()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act
        var areReferenceEqual = ReferenceEquals(address1, address2);
        var areValueEqual = address1.Equals(address2);

        // Assert
        areReferenceEqual.Should().BeFalse();
        areValueEqual.Should().BeTrue();
    }

    [Fact]
    public void ValueObject_SameReference_ShouldBeEqual()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");

        // Act
        var areReferenceEqual = ReferenceEquals(address, address);
        var areValueEqual = address.Equals(address);

        // Assert
        areReferenceEqual.Should().BeTrue();
        areValueEqual.Should().BeTrue();
    }

    #endregion
}
