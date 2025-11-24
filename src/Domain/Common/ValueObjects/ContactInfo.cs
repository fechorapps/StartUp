using ErrorOr;
using System.Text.RegularExpressions;

namespace DoorX.Domain.Common.ValueObjects;

/// <summary>
/// Value Object representing contact information (email and phone)
/// </summary>
public partial record ContactInfo
{
    private static readonly Regex EmailRegex = GenerateEmailRegex();
    private static readonly Regex PhoneRegex = GeneratePhoneRegex();

    public string Email { get; init; }
    public string? PhoneNumber { get; init; }

    private ContactInfo(string email, string? phoneNumber)
    {
        Email = email;
        PhoneNumber = phoneNumber;
    }

    public static ErrorOr<ContactInfo> Create(string email, string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation("ContactInfo.Email", "Email is required");

        if (!EmailRegex.IsMatch(email))
            return Error.Validation("ContactInfo.Email", "Invalid email format");

        if (!string.IsNullOrWhiteSpace(phoneNumber) && !PhoneRegex.IsMatch(phoneNumber))
            return Error.Validation("ContactInfo.PhoneNumber", "Invalid phone number format");

        return new ContactInfo(email.ToLower(), phoneNumber);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex GenerateEmailRegex();

    [GeneratedRegex(@"^\+?[\d\s\-\(\)]+$")]
    private static partial Regex GeneratePhoneRegex();

    public override string ToString() => $"{Email}{(string.IsNullOrWhiteSpace(PhoneNumber) ? "" : $" | {PhoneNumber}")}";
}
