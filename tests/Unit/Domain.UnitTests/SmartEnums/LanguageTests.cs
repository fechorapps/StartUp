using DoorX.Domain.Common.ValueObjects;

namespace DoorX.Domain.UnitTests.SmartEnums;

public class LanguageTests
{
    [Fact]
    public void GetAll_ShouldReturnAllLanguages()
    {
        // Act
        var languages = Language.GetAll();

        // Assert
        languages.Should().HaveCount(4);
        languages.Should().Contain(Language.English);
        languages.Should().Contain(Language.Spanish);
        languages.Should().Contain(Language.French);
        languages.Should().Contain(Language.Portuguese);
    }

    [Theory]
    [InlineData(1, "en")]
    [InlineData(2, "es")]
    [InlineData(3, "fr")]
    [InlineData(4, "pt")]
    public void FromId_WithValidId_ShouldReturnCorrectLanguage(int id, string expectedCode)
    {
        // Act
        var language = Language.FromId(id);

        // Assert
        language.Should().NotBeNull();
        language!.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var language = Language.FromId(999);

        // Assert
        language.Should().BeNull();
    }

    [Theory]
    [InlineData("en", 1)]
    [InlineData("es", 2)]
    [InlineData("fr", 3)]
    [InlineData("pt", 4)]
    public void FromName_WithValidCode_ShouldReturnCorrectLanguage(string code, int expectedId)
    {
        // Act
        var language = Language.FromName(code);

        // Assert
        language.Should().NotBeNull();
        language!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidCode_ShouldReturnNull()
    {
        // Act
        var language = Language.FromName("xx");

        // Assert
        language.Should().BeNull();
    }

    [Fact]
    public void FromName_IsCaseInsensitive()
    {
        // Act
        var lowerCase = Language.FromName("en");
        var upperCase = Language.FromName("EN");
        var mixedCase = Language.FromName("En");

        // Assert
        lowerCase.Should().Be(Language.English);
        upperCase.Should().Be(Language.English);
        mixedCase.Should().Be(Language.English);
    }

    [Fact]
    public void IsDefault_OnlyForEnglish_ShouldReturnTrue()
    {
        // Assert
        Language.English.IsDefault().Should().BeTrue();
        Language.Spanish.IsDefault().Should().BeFalse();
        Language.French.IsDefault().Should().BeFalse();
        Language.Portuguese.IsDefault().Should().BeFalse();
    }

    [Theory]
    [InlineData("en", "en-US")]
    [InlineData("es", "es-ES")]
    [InlineData("fr", "fr-FR")]
    [InlineData("pt", "pt-PT")]
    public void GetCultureCode_ShouldReturnCorrectValue(string code, string expectedCultureCode)
    {
        // Arrange
        var language = Language.FromName(code);

        // Act
        var cultureCode = language!.GetCultureCode();

        // Assert
        cultureCode.Should().Be(expectedCultureCode);
    }

    [Fact]
    public void IsRightToLeft_ForAllLanguages_ShouldReturnFalse()
    {
        // Act & Assert
        foreach (var language in Language.GetAll())
        {
            language.IsRightToLeft().Should().BeFalse(
                $"{language.EnglishName} is not a right-to-left language");
        }
    }

    [Theory]
    [InlineData("en", "Hello")]
    [InlineData("es", "Hola")]
    [InlineData("fr", "Bonjour")]
    [InlineData("pt", "Olá")]
    public void GetGreeting_ShouldReturnCorrectGreeting(string code, string expectedGreeting)
    {
        // Arrange
        var language = Language.FromName(code);

        // Act
        var greeting = language!.GetGreeting();

        // Assert
        greeting.Should().Be(expectedGreeting);
    }

    [Fact]
    public void GetDisplayName_ShouldReturnNativeName()
    {
        // Act & Assert
        Language.English.GetDisplayName().Should().Be("English");
        Language.Spanish.GetDisplayName().Should().Be("Español");
        Language.French.GetDisplayName().Should().Be("Français");
        Language.Portuguese.GetDisplayName().Should().Be("Português");
    }

    [Fact]
    public void EnglishName_ShouldNotBeEmpty()
    {
        // Act & Assert
        foreach (var language in Language.GetAll())
        {
            language.EnglishName.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void NativeName_ShouldNotBeEmpty()
    {
        // Act & Assert
        foreach (var language in Language.GetAll())
        {
            language.NativeName.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Flag_ShouldNotBeEmpty()
    {
        // Act & Assert
        foreach (var language in Language.GetAll())
        {
            language.Flag.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Code_ShouldBeTwoLetters()
    {
        // Act & Assert
        foreach (var language in Language.GetAll())
        {
            language.Code.Should().HaveLength(2);
            language.Code.Should().MatchRegex("^[a-z]{2}$",
                $"{language.EnglishName} should have a valid ISO 639-1 code");
        }
    }

    [Fact]
    public void FormatDate_ShouldFormatAccordingToCulture()
    {
        // Arrange
        var testDate = new DateTime(2024, 11, 24);

        // Act
        var englishDate = Language.English.FormatDate(testDate);
        var spanishDate = Language.Spanish.FormatDate(testDate);
        var frenchDate = Language.French.FormatDate(testDate);
        var portugueseDate = Language.Portuguese.FormatDate(testDate);

        // Assert
        englishDate.Should().NotBeNullOrWhiteSpace();
        spanishDate.Should().NotBeNullOrWhiteSpace();
        frenchDate.Should().NotBeNullOrWhiteSpace();
        portugueseDate.Should().NotBeNullOrWhiteSpace();

        // Different cultures format dates differently
        englishDate.Should().NotBe(spanishDate);
    }

    [Fact]
    public void Equality_SameLanguage_ShouldBeEqual()
    {
        // Arrange
        var language1 = Language.English;
        var language2 = Language.FromId(1);

        // Assert
        language2.Should().NotBeNull();
        language1.Should().Be(language2!);
        language1.Equals(language2!).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentLanguages_ShouldNotBeEqual()
    {
        // Arrange
        var language1 = Language.English;
        var language2 = Language.Spanish;

        // Assert
        language1.Should().NotBe(language2);
        (language1 == language2).Should().BeFalse();
    }

    [Fact]
    public void AllLanguages_ShouldHaveUniqueIds()
    {
        // Act
        var languages = Language.GetAll();
        var ids = languages.Select(l => l.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllLanguages_ShouldHaveUniqueCodes()
    {
        // Act
        var languages = Language.GetAll();
        var codes = languages.Select(l => l.Code).ToList();

        // Assert
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllLanguages_ShouldHaveUniqueEnglishNames()
    {
        // Act
        var languages = Language.GetAll();
        var names = languages.Select(l => l.EnglishName).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllLanguages_ShouldHaveUniqueNativeNames()
    {
        // Act
        var languages = Language.GetAll();
        var names = languages.Select(l => l.NativeName).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllLanguages_ShouldHaveValidCultureCodes()
    {
        // Act & Assert
        foreach (var language in Language.GetAll())
        {
            var cultureCode = language.GetCultureCode();
            cultureCode.Should().MatchRegex(@"^[a-z]{2}-[A-Z]{2}$",
                $"{language.EnglishName} should have a valid culture code");
        }
    }

    [Fact]
    public void ToString_ShouldReturnCode()
    {
        // Act & Assert
        Language.English.ToString().Should().Be("en");
        Language.Spanish.ToString().Should().Be("es");
        Language.French.ToString().Should().Be("fr");
        Language.Portuguese.ToString().Should().Be("pt");
    }
}
