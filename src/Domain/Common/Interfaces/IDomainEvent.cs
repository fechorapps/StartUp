namespace DoorX.Domain.Common.Interfaces;

/// <summary>
/// Representa un evento que ocurrió en el dominio.
/// Los domain events capturan algo que sucedió en el pasado.
/// </summary>
/// <remarks>
/// Los domain events son inmutables y representan hechos que ya ocurrieron.
/// Se usan para comunicar cambios importantes en el estado del dominio.
/// </remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Identificador único del evento.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Fecha y hora UTC en que ocurrió el evento.
    /// </summary>
    DateTime OccurredOnUtc { get; }
}
