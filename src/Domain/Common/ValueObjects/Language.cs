using ErrorOr;

namespace DoorX.Domain.Common.ValueObjects;

/// <summary>
/// Value Object representing a supported language for communication
/// </summary>
public record Language
{
    private static readonly HashSet<string> SupportedLanguages = new()
    {
        "en", "es", "fr", "pt"
    };

    public string Code { get; init; }
    public string Name { get; init; }

    private Language(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public static ErrorOr<Language> Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Error.Validation("Language.Code", "Language code is required");

        var normalizedCode = code.ToLower();
        if (!SupportedLanguages.Contains(normalizedCode))
            return Error.Validation("Language.Code", $"Unsupported language. Supported languages: {string.Join(", ", SupportedLanguages)}");

        var name = normalizedCode switch
        {
            "en" => "English",
            "es" => "Spanish",
            "fr" => "French",
            "pt" => "Portuguese",
            _ => normalizedCode
        };

        return new Language(normalizedCode, name);
    }

    public static Language English => new("en", "English");
    public static Language Spanish => new("es", "Spanish");
    public static Language French => new("fr", "French");
    public static Language Portuguese => new("pt", "Portuguese");

    public override string ToString() => Name;
}
