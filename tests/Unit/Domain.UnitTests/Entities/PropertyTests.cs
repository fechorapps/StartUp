using DoorX.Domain.Properties.Entities;
using DoorX.Domain.Properties.ValueObjects;

namespace DoorX.Domain.UnitTests.Entities;

public class PropertyTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnProperty()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Apt 4B", "Miami", "FL", "33101").Value;

        // Act
        var result = Property.Create(
            "Sunset Apartments - Unit 4B",
            address,
            PropertyType.Apartment);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Sunset Apartments - Unit 4B");
        result.Value.Address.Should().Be(address);
        result.Value.PropertyType.Should().Be(PropertyType.Apartment);
        result.Value.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldReturnError(string? name)
    {
        // Arrange
        var address = Address.Create("123 Main St", null, "Miami", "FL", "33101").Value;

        // Act
        var result = Property.Create(name!, address, PropertyType.Apartment);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Property.Name");
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateProperty()
    {
        // Arrange
        var property = CreateValidProperty();
        var newAddress = Address.Create("456 Oak Ave", null, "Tampa", "FL", "33602").Value;

        // Act
        var result = property.Update("New Name", newAddress, PropertyType.House);

        // Assert
        result.IsError.Should().BeFalse();
        property.Name.Should().Be("New Name");
        property.Address.Should().Be(newAddress);
        property.PropertyType.Should().Be(PropertyType.House);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var property = CreateValidProperty();

        // Act
        property.Deactivate();

        // Assert
        property.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var property = CreateValidProperty();
        property.Deactivate();

        // Act
        property.Activate();

        // Assert
        property.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetExternalPmsId_ShouldUpdateId()
    {
        // Arrange
        var property = CreateValidProperty();
        var externalId = "PMS-PROP-789";

        // Act
        property.SetExternalPmsId(externalId);

        // Assert
        property.ExternalPmsId.Should().Be(externalId);
    }

    private Property CreateValidProperty()
    {
        var address = Address.Create("123 Main St", "Apt 4B", "Miami", "FL", "33101").Value;
        return Property.Create("Test Property", address, PropertyType.Apartment).Value;
    }
}
