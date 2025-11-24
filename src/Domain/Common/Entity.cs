namespace DoorX.Domain.Common;

/// <summary>
/// Clase base abstracta para todas las entidades del dominio.
/// Una entidad se define por su identidad, no por sus atributos.
/// </summary>
/// <typeparam name="TId">El tipo del identificador único de la entidad.</typeparam>
/// <remarks>
/// Dos entidades son iguales si tienen el mismo Id, independientemente de sus otros atributos.
/// </remarks>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Identificador único de la entidad.
    /// </summary>
    public TId Id { get; protected init; }

    /// <summary>
    /// Constructor protegido para inicializar el Id.
    /// </summary>
    /// <param name="id">Identificador único de la entidad.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Constructor sin parámetros para Entity Framework y serializadores.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    protected Entity()
    {
    }
#pragma warning restore CS8618

    #region Equality

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (other.GetType() != GetType())
            return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((Entity<TId>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }

    #endregion
}
