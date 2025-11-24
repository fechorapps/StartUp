using DoorX.Domain.Properties.ValueObjects;

namespace DoorX.Domain.UnitTests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Act
        var result = Address.Create(
            "123 Main St",
            "Apt 4B",
            "Miami",
            "FL",
            "33101",
            "USA");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Street.Should().Be("123 Main St");
        result.Value.Unit.Should().Be("Apt 4B");
        result.Value.City.Should().Be("Miami");
        result.Value.State.Should().Be("FL");
        result.Value.ZipCode.Should().Be("33101");
        result.Value.Country.Should().Be("USA");
    }

    [Fact]
    public void Create_WithoutUnit_ShouldReturnSuccess()
    {
        // Act
        var result = Address.Create(
            "123 Main St",
            null,
            "Miami",
            "FL",
            "33101");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Unit.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutCountry_ShouldDefaultToUSA()
    {
        // Act
        var result = Address.Create(
            "123 Main St",
            null,
            "Miami",
            "FL",
            "33101");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Country.Should().Be("USA");
    }

    [Theory]
    [InlineData("", "Apt 1", "Miami", "FL", "33101")]
    [InlineData("  ", "Apt 1", "Miami", "FL", "33101")]
    [InlineData(null, "Apt 1", "Miami", "FL", "33101")]
    public void Create_WithEmptyStreet_ShouldReturnError(string? street, string? unit, string city, string state, string zipCode)
    {
        // Act
        var result = Address.Create(street!, unit, city, state, zipCode);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Address.Street");
    }

    [Theory]
    [InlineData("123 Main St", "Apt 1", "", "FL", "33101")]
    [InlineData("123 Main St", "Apt 1", null, "FL", "33101")]
    public void Create_WithEmptyCity_ShouldReturnError(string street, string? unit, string? city, string state, string zipCode)
    {
        // Act
        var result = Address.Create(street, unit, city!, state, zipCode);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Address.City");
    }

    [Fact]
    public void GetFullAddress_WithUnit_ShouldIncludeUnit()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Apt 4B", "Miami", "FL", "33101", "USA").Value;

        // Act
        var fullAddress = address.GetFullAddress();

        // Assert
        fullAddress.Should().Be("123 Main St Apt 4B, Miami, FL 33101, USA");
    }

    [Fact]
    public void GetFullAddress_WithoutUnit_ShouldExcludeUnit()
    {
        // Arrange
        var address = Address.Create("123 Main St", null, "Miami", "FL", "33101", "USA").Value;

        // Act
        var fullAddress = address.GetFullAddress();

        // Assert
        fullAddress.Should().Be("123 Main St, Miami, FL 33101, USA");
    }

    [Fact]
    public void ToString_ShouldReturnFullAddress()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Apt 4B", "Miami", "FL", "33101", "USA").Value;

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Be(address.GetFullAddress());
    }

    [Fact]
    public void RecordEquality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Apt 4B", "Miami", "FL", "33101", "USA").Value;
        var address2 = Address.Create("123 Main St", "Apt 4B", "Miami", "FL", "33101", "USA").Value;

        // Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_DifferentStreet_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Apt 4B", "Miami", "FL", "33101", "USA").Value;
        var address2 = Address.Create("456 Oak Ave", "Apt 4B", "Miami", "FL", "33101", "USA").Value;

        // Assert
        address1.Should().NotBe(address2);
    }
}
