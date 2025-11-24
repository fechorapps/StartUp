namespace DoorX.Domain.Common;

/// <summary>
/// Clase base abstracta para todos los Value Objects del dominio.
/// Un Value Object se define por sus atributos, no por una identidad.
/// </summary>
/// <remarks>
/// Características de los Value Objects:
/// - Son inmutables (no pueden cambiar después de creados)
/// - No tienen identidad propia
/// - La igualdad se determina por el valor de todos sus atributos
/// - Son intercambiables si tienen los mismos valores
/// - Ejemplo: Address, Money, DateRange, Email
///
/// RECOMENDACIÓN: En C# 9+, considera usar 'record' en lugar de heredar de esta clase
/// para Value Objects simples, ya que los records proporcionan inmutabilidad y
/// equality by value de forma automática.
///
/// Ejemplo con record:
/// public record Address(string Street, string City, string ZipCode);
/// </remarks>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Obtiene los componentes atómicos del value object para comparación.
    /// </summary>
    /// <returns>Enumeración de todos los valores que definen la igualdad.</returns>
    /// <remarks>
    /// Sobrescribe este método para devolver todos los valores que determinan
    /// si dos value objects son iguales.
    /// </remarks>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    #region Equality

    public bool Equals(ValueObject? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((ValueObject)obj);
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Where(x => x is not null)
            .Aggregate(1, (current, obj) =>
            {
                unchecked
                {
                    return (current * 23) + obj!.GetHashCode();
                }
            });
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }

    #endregion
}
