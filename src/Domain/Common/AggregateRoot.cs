using DoorX.Domain.Common.Interfaces;

namespace DoorX.Domain.Common;

/// <summary>
/// Clase base abstracta para todos los Aggregate Roots del dominio.
/// Un Aggregate Root es la entidad raíz de un agregado que garantiza la consistencia del mismo.
/// </summary>
/// <typeparam name="TId">El tipo del identificador único del aggregate root.</typeparam>
/// <remarks>
/// Los Aggregate Roots:
/// - Son el único punto de entrada para modificar el agregado
/// - Garantizan las invariantes del negocio
/// - Pueden generar y almacenar domain events
/// - Son los únicos que pueden ser referenciados directamente desde fuera del agregado
/// - Incluyen auditoría automática (CreatedOnUtc, ModifiedOnUtc) heredada de AuditableEntity
/// </remarks>
public abstract class AggregateRoot<TId> : AuditableEntity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Eventos de dominio generados por este aggregate root.
    /// </summary>
    /// <remarks>
    /// Esta colección es de solo lectura desde fuera del aggregate.
    /// Los eventos se agregan mediante el método protegido AddDomainEvent.
    /// </remarks>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Constructor protegido para inicializar el Id.
    /// </summary>
    /// <param name="id">Identificador único del aggregate root.</param>
    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// Constructor sin parámetros para Entity Framework y serializadores.
    /// </summary>
    protected AggregateRoot() : base()
    {
    }

    /// <summary>
    /// Agrega un domain event a la colección de eventos del aggregate.
    /// </summary>
    /// <param name="domainEvent">El evento de dominio a agregar.</param>
    /// <remarks>
    /// Los eventos agregados aquí serán publicados después de que se persista el aggregate.
    /// Esto garantiza que los eventos solo se publiquen si la transacción es exitosa.
    /// </remarks>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Limpia todos los domain events del aggregate.
    /// </summary>
    /// <remarks>
    /// Este método típicamente es llamado por la infraestructura después de publicar los eventos.
    /// </remarks>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
