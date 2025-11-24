namespace DoorX.Domain.Common;

/// <summary>
/// Clase base abstracta para entidades del dominio que requieren auditoría.
/// Extiende Entity agregando propiedades de seguimiento temporal (CreatedOnUtc, ModifiedOnUtc).
/// </summary>
/// <typeparam name="TId">El tipo del identificador único de la entidad.</typeparam>
/// <remarks>
/// Usa esta clase cuando necesites rastrear cuándo se creó y modificó una entidad.
/// Si solo necesitas identidad sin auditoría, usa Entity directamente.
/// </remarks>
public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Fecha y hora UTC de creación de la entidad.
    /// </summary>
    public DateTime CreatedOnUtc { get; protected init; }

    /// <summary>
    /// Fecha y hora UTC de la última modificación de la entidad.
    /// </summary>
    public DateTime? ModifiedOnUtc { get; protected set; }

    /// <summary>
    /// Constructor protegido para inicializar el Id y la fecha de creación.
    /// </summary>
    /// <param name="id">Identificador único de la entidad.</param>
    protected AuditableEntity(TId id) : base(id)
    {
        CreatedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor sin parámetros para Entity Framework y serializadores.
    /// </summary>
    protected AuditableEntity() : base()
    {
    }

    /// <summary>
    /// Actualiza la fecha de modificación a la fecha y hora actual UTC.
    /// </summary>
    protected void UpdateModifiedDate()
    {
        ModifiedOnUtc = DateTime.UtcNow;
    }
}
