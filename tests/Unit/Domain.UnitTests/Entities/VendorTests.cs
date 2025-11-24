using DoorX.Domain.Common.ValueObjects;
using DoorX.Domain.Vendors.Entities;
using DoorX.Domain.Vendors.ValueObjects;
using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.UnitTests.Entities;

public class VendorTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnVendor()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("vendor@company.com", "+1-305-555-1234").Value;

        // Act
        var result = Vendor.Create("ABC Plumbing", contactInfo);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.CompanyName.Should().Be("ABC Plumbing");
        result.Value.ContactInfo.Should().Be(contactInfo);
        result.Value.Rating.Should().Be(Rating.Unrated);
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsAvailable.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyCompanyName_ShouldReturnError(string? companyName)
    {
        // Arrange
        var contactInfo = ContactInfo.Create("vendor@company.com").Value;

        // Act
        var result = Vendor.Create(companyName!, contactInfo);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Vendor.CompanyName");
    }

    [Fact]
    public void AddServiceCategory_WithNewCategory_ShouldSucceed()
    {
        // Arrange
        var vendor = CreateValidVendor();

        // Act
        var result = vendor.AddServiceCategory(ServiceCategory.Plumbing);

        // Assert
        result.IsError.Should().BeFalse();
        vendor.ServiceCategories.Should().Contain(ServiceCategory.Plumbing);
    }

    [Fact]
    public void AddServiceCategory_DuplicateCategory_ShouldReturnError()
    {
        // Arrange
        var vendor = CreateValidVendor();
        vendor.AddServiceCategory(ServiceCategory.Plumbing);

        // Act
        var result = vendor.AddServiceCategory(ServiceCategory.Plumbing);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain("Vendor.ServiceCategory");
        vendor.ServiceCategories.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveServiceCategory_ExistingCategory_ShouldSucceed()
    {
        // Arrange
        var vendor = CreateValidVendor();
        vendor.AddServiceCategory(ServiceCategory.Plumbing);

        // Act
        var result = vendor.RemoveServiceCategory(ServiceCategory.Plumbing);

        // Assert
        result.IsError.Should().BeFalse();
        vendor.ServiceCategories.Should().NotContain(ServiceCategory.Plumbing);
    }

    [Fact]
    public void RemoveServiceCategory_NonExistentCategory_ShouldReturnError()
    {
        // Arrange
        var vendor = CreateValidVendor();

        // Act
        var result = vendor.RemoveServiceCategory(ServiceCategory.Plumbing);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain("Vendor.ServiceCategory");
    }

    [Fact]
    public void AddServiceArea_WithNewArea_ShouldSucceed()
    {
        // Arrange
        var vendor = CreateValidVendor();
        var area = ServiceArea.Create("33101").Value;

        // Act
        var result = vendor.AddServiceArea(area);

        // Assert
        result.IsError.Should().BeFalse();
        vendor.ServiceAreas.Should().Contain(area);
    }

    [Fact]
    public void AddServiceArea_DuplicateZipCode_ShouldReturnError()
    {
        // Arrange
        var vendor = CreateValidVendor();
        var area = ServiceArea.Create("33101").Value;
        vendor.AddServiceArea(area);

        // Act
        var duplicateArea = ServiceArea.Create("33101").Value;
        var result = vendor.AddServiceArea(duplicateArea);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain("Vendor.ServiceArea");
    }

    [Fact]
    public void CanService_WithMatchingCategoryAndArea_ShouldReturnTrue()
    {
        // Arrange
        var vendor = CreateValidVendor();
        vendor.AddServiceCategory(ServiceCategory.HVAC);
        vendor.AddServiceArea(ServiceArea.Create("33101").Value);

        // Act
        var canService = vendor.CanService(ServiceCategory.HVAC, "33101");

        // Assert
        canService.Should().BeTrue();
    }

    [Fact]
    public void CanService_WithoutMatchingCategory_ShouldReturnFalse()
    {
        // Arrange
        var vendor = CreateValidVendor();
        vendor.AddServiceCategory(ServiceCategory.Plumbing);
        vendor.AddServiceArea(ServiceArea.Create("33101").Value);

        // Act
        var canService = vendor.CanService(ServiceCategory.HVAC, "33101");

        // Assert
        canService.Should().BeFalse();
    }

    [Fact]
    public void CanService_WithoutMatchingArea_ShouldReturnFalse()
    {
        // Arrange
        var vendor = CreateValidVendor();
        vendor.AddServiceCategory(ServiceCategory.HVAC);
        vendor.AddServiceArea(ServiceArea.Create("33101").Value);

        // Act
        var canService = vendor.CanService(ServiceCategory.HVAC, "90210");

        // Assert
        canService.Should().BeFalse();
    }

    [Fact]
    public void CanService_WhenNotActive_ShouldReturnFalse()
    {
        // Arrange
        var vendor = CreateValidVendor();
        vendor.AddServiceCategory(ServiceCategory.HVAC);
        vendor.AddServiceArea(ServiceArea.Create("33101").Value);
        vendor.Deactivate();

        // Act
        var canService = vendor.CanService(ServiceCategory.HVAC, "33101");

        // Assert
        canService.Should().BeFalse();
    }

    [Fact]
    public void CanService_WhenNotAvailable_ShouldReturnFalse()
    {
        // Arrange
        var vendor = CreateValidVendor();
        vendor.AddServiceCategory(ServiceCategory.HVAC);
        vendor.AddServiceArea(ServiceArea.Create("33101").Value);
        vendor.SetAvailability(false);

        // Act
        var canService = vendor.CanService(ServiceCategory.HVAC, "33101");

        // Assert
        canService.Should().BeFalse();
    }

    [Fact]
    public void UpdateRating_WithValidRating_ShouldUpdate()
    {
        // Arrange
        var vendor = CreateValidVendor();
        var newRating = Rating.Create(4.5m, 10).Value;

        // Act
        var result = vendor.UpdateRating(newRating);

        // Assert
        result.IsError.Should().BeFalse();
        vendor.Rating.Should().Be(newRating);
    }

    [Fact]
    public void Deactivate_ShouldSetBothIsActiveAndIsAvailableToFalse()
    {
        // Arrange
        var vendor = CreateValidVendor();

        // Act
        vendor.Deactivate();

        // Assert
        vendor.IsActive.Should().BeFalse();
        vendor.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var vendor = CreateValidVendor();
        vendor.Deactivate();

        // Act
        vendor.Activate();

        // Assert
        vendor.IsActive.Should().BeTrue();
    }

    private Vendor CreateValidVendor()
    {
        var contactInfo = ContactInfo.Create("vendor@company.com", "+1-305-555-1234").Value;
        return Vendor.Create("ABC Services", contactInfo).Value;
    }
}
