using DoorX.Domain.WorkOrders.ValueObjects;

namespace DoorX.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_ShouldReturnMoney()
    {
        // Act
        var result = Money.Create(150.50m, "USD");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Amount.Should().Be(150.50m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithDefaultCurrency_ShouldUseUSD()
    {
        // Act
        var result = Money.Create(100m);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldReturnError()
    {
        // Act
        var result = Money.Create(-50m, "USD");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Money.Amount");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyCurrency_ShouldReturnError(string? currency)
    {
        // Act
        var result = Money.Create(100m, currency!);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Money.Currency");
    }

    [Fact]
    public void Create_ShouldNormalizeCurrencyToUpperCase()
    {
        // Act
        var result = Money.Create(100m, "usd");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Zero_ShouldReturnZeroUSD()
    {
        // Act
        var zero = Money.Zero;

        // Assert
        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("USD");
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var money = Money.Create(150.50m, "USD").Value;

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Be("USD 150.50");
    }

    [Fact]
    public void RecordEquality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(100m, "USD").Value;

        // Assert
        money1.Should().Be(money2);
    }

    [Fact]
    public void RecordEquality_DifferentAmounts_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(150m, "USD").Value;

        // Assert
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void RecordEquality_DifferentCurrencies_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(100m, "EUR").Value;

        // Assert
        money1.Should().NotBe(money2);
    }
}
