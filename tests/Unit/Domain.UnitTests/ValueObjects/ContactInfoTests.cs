using DoorX.Domain.Common.ValueObjects;

namespace DoorX.Domain.UnitTests.ValueObjects;

public class ContactInfoTests
{
    [Fact]
    public void Create_WithValidEmailAndPhone_ShouldReturnSuccess()
    {
        // Act
        var result = ContactInfo.Create("john@example.com", "+1-305-555-1234");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("john@example.com");
        result.Value.PhoneNumber.Should().Be("+1-305-555-1234");
    }

    [Fact]
    public void Create_WithOnlyEmail_ShouldReturnSuccess()
    {
        // Act
        var result = ContactInfo.Create("john@example.com");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("john@example.com");
        result.Value.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void Create_EmailToLowerCase_ShouldNormalizeEmail()
    {
        // Act
        var result = ContactInfo.Create("John.Doe@EXAMPLE.COM");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("john.doe@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyEmail_ShouldReturnError(string? email)
    {
        // Act
        var result = ContactInfo.Create(email!);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ContactInfo.Email");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    [InlineData("john@")]
    [InlineData("john doe@example.com")]
    public void Create_WithInvalidEmail_ShouldReturnError(string email)
    {
        // Act
        var result = ContactInfo.Create(email);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ContactInfo.Email");
    }

    [Theory]
    [InlineData("+1-305-555-1234")]
    [InlineData("305-555-1234")]
    [InlineData("(305) 555-1234")]
    [InlineData("3055551234")]
    public void Create_WithValidPhoneNumber_ShouldReturnSuccess(string phoneNumber)
    {
        // Act
        var result = ContactInfo.Create("john@example.com", phoneNumber);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PhoneNumber.Should().Be(phoneNumber);
    }

    [Theory]
    [InlineData("abc-def-ghij")]
    public void Create_WithInvalidPhoneNumber_ShouldReturnError(string phoneNumber)
    {
        // Act
        var result = ContactInfo.Create("john@example.com", phoneNumber);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ContactInfo.PhoneNumber");
    }

    [Fact]
    public void ToString_WithPhoneNumber_ShouldIncludeBoth()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("john@example.com", "+1-305-555-1234").Value;

        // Act
        var result = contactInfo.ToString();

        // Assert
        result.Should().Be("john@example.com | +1-305-555-1234");
    }

    [Fact]
    public void ToString_WithoutPhoneNumber_ShouldOnlyShowEmail()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("john@example.com").Value;

        // Act
        var result = contactInfo.ToString();

        // Assert
        result.Should().Be("john@example.com");
    }
}
