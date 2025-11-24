using ErrorOr;

namespace DoorX.Domain.WorkOrders.ValueObjects;

/// <summary>
/// Value Object representing monetary amounts
/// </summary>
public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static ErrorOr<Money> Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            return Error.Validation("Money.Amount", "Amount cannot be negative");

        if (string.IsNullOrWhiteSpace(currency))
            return Error.Validation("Money.Currency", "Currency is required");

        return new Money(amount, currency.ToUpper());
    }

    public static Money Zero => new(0, "USD");

    public override string ToString() => $"{Currency} {Amount:F2}";
}
