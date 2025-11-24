using DoorX.Domain.Common;

namespace DoorX.Domain.Common.ValueObjects;

/// <summary>
/// Smart Enum representing a supported language for communication
/// </summary>
/// <remarks>
/// Persistence Strategy: MEMORY ONLY (Smart Enum - MVP)
/// - Limited set of supported languages initially
/// - Stored as VARCHAR(5) in database (ISO 639-1 code)
/// - Adding languages requires system translations
///
/// Future: Consider persistence if supporting 20+ languages
/// See docs/CATALOGS_PERSISTENCE.md for details
/// </remarks>
public sealed class Language : SmartEnum<Language>
{
    // Predefined languages (ID, ISO Code, Name, Native Name)
    public static readonly Language English = new(1, "en", "English", "English", "🇺🇸");
    public static readonly Language Spanish = new(2, "es", "Spanish", "Español", "🇪🇸");
    public static readonly Language French = new(3, "fr", "French", "Français", "🇫🇷");
    public static readonly Language Portuguese = new(4, "pt", "Portuguese", "Português", "🇵🇹");

    /// <summary>
    /// ISO 639-1 language code (2 letters)
    /// </summary>
    public string Code => Name;

    /// <summary>
    /// English name of the language
    /// </summary>
    public string EnglishName { get; }

    /// <summary>
    /// Native name of the language (how it's called in that language)
    /// </summary>
    public string NativeName { get; }

    /// <summary>
    /// Flag emoji for UI display
    /// </summary>
    public string Flag { get; }

    private Language(int id, string code, string englishName, string nativeName, string flag)
        : base(id, code)
    {
        EnglishName = englishName;
        NativeName = nativeName;
        Flag = flag;
    }

    /// <summary>
    /// Checks if this is the default system language
    /// </summary>
    public bool IsDefault() => this == English;

    /// <summary>
    /// Gets the display name based on current UI language
    /// For now returns native name, but could be localized later
    /// </summary>
    public string GetDisplayName() => NativeName;

    /// <summary>
    /// Gets the culture code for .NET CultureInfo
    /// </summary>
    public string GetCultureCode()
    {
        return this switch
        {
            var l when l == English => "en-US",
            var l when l == Spanish => "es-ES",
            var l when l == French => "fr-FR",
            var l when l == Portuguese => "pt-PT",
            _ => "en-US"
        };
    }

    /// <summary>
    /// Checks if this language is right-to-left
    /// </summary>
    public bool IsRightToLeft() => false; // None of our current languages are RTL

    /// <summary>
    /// Gets common greeting in this language
    /// </summary>
    public string GetGreeting()
    {
        return this switch
        {
            var l when l == English => "Hello",
            var l when l == Spanish => "Hola",
            var l when l == French => "Bonjour",
            var l when l == Portuguese => "Olá",
            _ => "Hello"
        };
    }

    /// <summary>
    /// Formats a date according to language conventions
    /// </summary>
    public string FormatDate(DateTime date)
    {
        return date.ToString("d", new System.Globalization.CultureInfo(GetCultureCode()));
    }
}
