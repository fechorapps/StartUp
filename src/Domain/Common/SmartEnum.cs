using System.Reflection;

namespace DoorX.Domain.Common;

/// <summary>
/// Base class for implementing Smart Enums (type-safe enums with behavior and metadata)
/// </summary>
/// <typeparam name="TEnum">The enum type inheriting from this class</typeparam>
/// <remarks>
/// Smart Enums provide:
/// - Type safety
/// - Rich behavior and metadata
/// - Strongly-typed values
/// - Compile-time safety
/// - Better IDE support than string constants
///
/// Example:
/// <code>
/// public class Priority : SmartEnum&lt;Priority&gt;
/// {
///     public static readonly Priority High = new(1, "High", 24);
///     public static readonly Priority Low = new(2, "Low", 168);
///
///     public int ExpectedHours { get; }
///
///     private Priority(int id, string name, int expectedHours) : base(id, name)
///     {
///         ExpectedHours = expectedHours;
///     }
/// }
/// </code>
/// </remarks>
public abstract class SmartEnum<TEnum> : IEquatable<SmartEnum<TEnum>>, IComparable<SmartEnum<TEnum>>
    where TEnum : SmartEnum<TEnum>
{
    private static readonly Lazy<Dictionary<int, TEnum>> _enumsById =
        new(() => GetAllOptions().ToDictionary(e => e.Id));

    private static readonly Lazy<Dictionary<string, TEnum>> _enumsByName =
        new(() => GetAllOptions().ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Unique identifier for this enum value (used for database storage)
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Display name for this enum value
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Protected constructor for derived classes
    /// </summary>
    protected SmartEnum(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Id = id;
        Name = name;
    }

    /// <summary>
    /// Gets all defined enum values
    /// </summary>
    public static IEnumerable<TEnum> GetAll() => _enumsById.Value.Values;

    /// <summary>
    /// Gets an enum by its ID
    /// </summary>
    public static TEnum? FromId(int id)
    {
        _enumsById.Value.TryGetValue(id, out var enumValue);
        return enumValue;
    }

    /// <summary>
    /// Gets an enum by its name (case-insensitive)
    /// </summary>
    public static TEnum? FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        _enumsByName.Value.TryGetValue(name, out var enumValue);
        return enumValue;
    }

    /// <summary>
    /// Tries to get an enum by its ID
    /// </summary>
    public static bool TryFromId(int id, out TEnum? enumValue)
    {
        enumValue = FromId(id);
        return enumValue != null;
    }

    /// <summary>
    /// Tries to get an enum by its name
    /// </summary>
    public static bool TryFromName(string name, out TEnum? enumValue)
    {
        enumValue = FromName(name);
        return enumValue != null;
    }

    /// <summary>
    /// Checks if a given ID is valid
    /// </summary>
    public static bool IsDefined(int id) => _enumsById.Value.ContainsKey(id);

    /// <summary>
    /// Checks if a given name is valid
    /// </summary>
    public static bool IsDefined(string name) =>
        !string.IsNullOrWhiteSpace(name) && _enumsByName.Value.ContainsKey(name);

    private static IEnumerable<TEnum> GetAllOptions()
    {
        var enumType = typeof(TEnum);
        return enumType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == enumType)
            .Select(f => (TEnum)f.GetValue(null)!)
            .OrderBy(e => e.Id);
    }

    #region Equality and Comparison

    public bool Equals(SmartEnum<TEnum>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return GetType() == other.GetType() && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is SmartEnum<TEnum> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public int CompareTo(SmartEnum<TEnum>? other)
    {
        if (other is null)
            return 1;

        return Id.CompareTo(other.Id);
    }

    public static bool operator ==(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right)
    {
        return !Equals(left, right);
    }

    public static bool operator <(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right)
    {
        return left is null ? right is not null : left.CompareTo(right) < 0;
    }

    public static bool operator <=(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    public static bool operator >(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right)
    {
        return left?.CompareTo(right) > 0;
    }

    public static bool operator >=(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right)
    {
        return left is null ? right is null : left.CompareTo(right) >= 0;
    }

    #endregion

    public override string ToString() => Name;

    /// <summary>
    /// Implicit conversion to string (for serialization)
    /// </summary>
    public static implicit operator string(SmartEnum<TEnum> smartEnum) => smartEnum.Name;

    /// <summary>
    /// Implicit conversion to int (for database storage)
    /// </summary>
    public static implicit operator int(SmartEnum<TEnum> smartEnum) => smartEnum.Id;
}
