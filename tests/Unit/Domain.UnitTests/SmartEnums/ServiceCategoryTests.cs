using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.UnitTests.SmartEnums;

public class ServiceCategoryTests
{
    [Fact]
    public void GetAll_ShouldReturnAllServiceCategories()
    {
        // Act
        var categories = ServiceCategory.GetAll();

        // Assert
        categories.Should().HaveCount(7);
        categories.Should().Contain(ServiceCategory.Plumbing);
        categories.Should().Contain(ServiceCategory.Electrical);
        categories.Should().Contain(ServiceCategory.HVAC);
        categories.Should().Contain(ServiceCategory.Appliance);
        categories.Should().Contain(ServiceCategory.PestControl);
        categories.Should().Contain(ServiceCategory.Cleaning);
        categories.Should().Contain(ServiceCategory.GeneralMaintenance);
    }

    [Theory]
    [InlineData(1, "Plumbing")]
    [InlineData(2, "Electrical")]
    [InlineData(3, "HVAC")]
    [InlineData(4, "Appliance")]
    [InlineData(5, "PestControl")]
    [InlineData(6, "Cleaning")]
    [InlineData(7, "GeneralMaintenance")]
    public void FromId_WithValidId_ShouldReturnCorrectCategory(int id, string expectedName)
    {
        // Act
        var category = ServiceCategory.FromId(id);

        // Assert
        category.Should().NotBeNull();
        category!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var category = ServiceCategory.FromId(999);

        // Assert
        category.Should().BeNull();
    }

    [Theory]
    [InlineData("Plumbing", 1)]
    [InlineData("Electrical", 2)]
    [InlineData("HVAC", 3)]
    [InlineData("Appliance", 4)]
    [InlineData("PestControl", 5)]
    [InlineData("Cleaning", 6)]
    [InlineData("GeneralMaintenance", 7)]
    public void FromName_WithValidName_ShouldReturnCorrectCategory(string name, int expectedId)
    {
        // Act
        var category = ServiceCategory.FromName(name);

        // Assert
        category.Should().NotBeNull();
        category!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var category = ServiceCategory.FromName("NonExistent");

        // Assert
        category.Should().BeNull();
    }

    [Fact]
    public void FromName_IsCaseInsensitive()
    {
        // Act
        var lowerCase = ServiceCategory.FromName("plumbing");
        var upperCase = ServiceCategory.FromName("PLUMBING");
        var correctCase = ServiceCategory.FromName("Plumbing");

        // Assert
        lowerCase.Should().NotBeNull();
        upperCase.Should().NotBeNull();
        correctCase.Should().NotBeNull();

        lowerCase.Should().Be(ServiceCategory.Plumbing);
        upperCase.Should().Be(ServiceCategory.Plumbing);
        correctCase.Should().Be(ServiceCategory.Plumbing);
    }

    [Theory]
    [InlineData("Electrical", true)]
    [InlineData("HVAC", true)]
    [InlineData("PestControl", true)]
    [InlineData("Plumbing", false)]
    [InlineData("Appliance", false)]
    [InlineData("Cleaning", false)]
    [InlineData("GeneralMaintenance", false)]
    public void RequiresCertification_ShouldReturnCorrectValue(string categoryName, bool expectedResult)
    {
        // Arrange
        var category = ServiceCategory.FromName(categoryName);

        // Act
        var requiresCertification = category!.RequiresCertification();

        // Assert
        requiresCertification.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("Electrical", "High (potential safety issue)")]
    [InlineData("HVAC", "High (comfort issue)")]
    [InlineData("Plumbing", "High (water damage risk)")]
    [InlineData("PestControl", "Normal")]
    [InlineData("Appliance", "Normal")]
    [InlineData("Cleaning", "Low")]
    [InlineData("GeneralMaintenance", "Normal")]
    public void GetTypicalPriority_ShouldReturnCorrectPriority(string categoryName, string expectedPriority)
    {
        // Arrange
        var category = ServiceCategory.FromName(categoryName);

        // Act
        var priority = category!.GetTypicalPriority();

        // Assert
        priority.Should().Be(expectedPriority);
    }

    [Fact]
    public void Description_ShouldNotBeEmpty()
    {
        // Act & Assert
        foreach (var category in ServiceCategory.GetAll())
        {
            category.Description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Equality_SameCategory_ShouldBeEqual()
    {
        // Arrange
        var category1 = ServiceCategory.Plumbing;
        var category2 = ServiceCategory.FromId(1);

        // Assert
        category2.Should().NotBeNull();
        category1.Should().Be(category2!);
        category1.Equals(category2!).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCategories_ShouldNotBeEqual()
    {
        // Arrange
        var category1 = ServiceCategory.Plumbing;
        var category2 = ServiceCategory.Electrical;

        // Assert
        category1.Should().NotBe(category2);
        (category1 == category2).Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnName()
    {
        // Act
        var result = ServiceCategory.Plumbing.ToString();

        // Assert
        result.Should().Be("Plumbing");
    }

    [Fact]
    public void CompareTo_ShouldOrderById()
    {
        // Arrange
        var categories = new[]
        {
            ServiceCategory.GeneralMaintenance,
            ServiceCategory.Plumbing,
            ServiceCategory.HVAC,
            ServiceCategory.Electrical
        };

        // Act
        var sorted = categories.OrderBy(c => c).ToList();

        // Assert
        sorted[0].Should().Be(ServiceCategory.Plumbing);     // ID 1
        sorted[1].Should().Be(ServiceCategory.Electrical);   // ID 2
        sorted[2].Should().Be(ServiceCategory.HVAC);         // ID 3
        sorted[3].Should().Be(ServiceCategory.GeneralMaintenance); // ID 7
    }

    [Fact]
    public void AllCategories_ShouldHaveUniqueIds()
    {
        // Act
        var categories = ServiceCategory.GetAll();
        var ids = categories.Select(c => c.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllCategories_ShouldHaveUniqueNames()
    {
        // Act
        var categories = ServiceCategory.GetAll();
        var names = categories.Select(c => c.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void HighPriorityCategories_ShouldBeElectricalHVACAndPlumbing()
    {
        // Act
        var highPriorityCategories = ServiceCategory.GetAll()
            .Where(c => c.GetTypicalPriority().StartsWith("High"))
            .ToList();

        // Assert
        highPriorityCategories.Should().HaveCount(3);
        highPriorityCategories.Should().Contain(ServiceCategory.Electrical);
        highPriorityCategories.Should().Contain(ServiceCategory.HVAC);
        highPriorityCategories.Should().Contain(ServiceCategory.Plumbing);
    }

    [Fact]
    public void CertifiedCategories_ShouldBeThree()
    {
        // Act
        var certifiedCategories = ServiceCategory.GetAll()
            .Where(c => c.RequiresCertification())
            .ToList();

        // Assert
        certifiedCategories.Should().HaveCount(3);
        certifiedCategories.Should().Contain(ServiceCategory.Electrical);
        certifiedCategories.Should().Contain(ServiceCategory.HVAC);
        certifiedCategories.Should().Contain(ServiceCategory.PestControl);
    }
}
