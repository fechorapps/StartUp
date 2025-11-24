using DoorX.Domain.Common.ValueObjects;
using DoorX.Domain.Properties.ValueObjects;
using DoorX.Domain.Tenants.Entities;
using DoorX.Domain.Tenants.ValueObjects;

namespace DoorX.Domain.UnitTests.Entities;

public class TenantTests
{
    private readonly PropertyId _propertyId = PropertyId.CreateUnique();

    [Fact]
    public void Create_WithValidData_ShouldReturnTenant()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("john@example.com", "+1-305-555-1234").Value;

        // Act
        var result = Tenant.Create(
            "John Doe",
            contactInfo,
            _propertyId,
            Language.English);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.FullName.Should().Be("John Doe");
        result.Value.ContactInfo.Should().Be(contactInfo);
        result.Value.PropertyId.Should().Be(_propertyId);
        result.Value.PreferredLanguage.Should().Be(Language.English);
        result.Value.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldReturnError(string? fullName)
    {
        // Arrange
        var contactInfo = ContactInfo.Create("john@example.com").Value;

        // Act
        var result = Tenant.Create(
            fullName!,
            contactInfo,
            _propertyId,
            Language.English);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Tenant.FullName");
    }

    [Fact]
    public void UpdateContactInfo_WithValidInfo_ShouldUpdate()
    {
        // Arrange
        var tenant = CreateValidTenant();
        var newContactInfo = ContactInfo.Create("newemail@example.com", "+1-555-999-8888").Value;

        // Act
        var result = tenant.UpdateContactInfo(newContactInfo);

        // Assert
        result.IsError.Should().BeFalse();
        tenant.ContactInfo.Should().Be(newContactInfo);
    }

    [Fact]
    public void UpdatePreferredLanguage_ShouldChangeLanguage()
    {
        // Arrange
        var tenant = CreateValidTenant();

        // Act
        tenant.UpdatePreferredLanguage(Language.Spanish);

        // Assert
        tenant.PreferredLanguage.Should().Be(Language.Spanish);
    }

    [Fact]
    public void MoveTo_WithNewPropertyId_ShouldUpdateProperty()
    {
        // Arrange
        var tenant = CreateValidTenant();
        var newPropertyId = PropertyId.CreateUnique();

        // Act
        tenant.MoveTo(newPropertyId);

        // Assert
        tenant.PropertyId.Should().Be(newPropertyId);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var tenant = CreateValidTenant();

        // Act
        tenant.Deactivate();

        // Assert
        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var tenant = CreateValidTenant();
        tenant.Deactivate();

        // Act
        tenant.Activate();

        // Assert
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetExternalPmsId_ShouldUpdateId()
    {
        // Arrange
        var tenant = CreateValidTenant();
        var externalId = "PMS-12345";

        // Act
        tenant.SetExternalPmsId(externalId);

        // Assert
        tenant.ExternalPmsId.Should().Be(externalId);
    }

    private Tenant CreateValidTenant()
    {
        var contactInfo = ContactInfo.Create("john@example.com", "+1-305-555-1234").Value;
        return Tenant.Create("John Doe", contactInfo, _propertyId, Language.English).Value;
    }
}
