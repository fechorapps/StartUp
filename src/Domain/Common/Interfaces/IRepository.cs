namespace DoorX.Domain.Common.Interfaces;

/// <summary>
/// Interface genérica base para todos los repositorios del dominio.
/// Define las operaciones CRUD básicas para un Aggregate Root.
/// </summary>
/// <typeparam name="TEntity">El tipo del aggregate root.</typeparam>
/// <typeparam name="TId">El tipo del identificador del aggregate root.</typeparam>
/// <remarks>
/// Los repositorios:
/// - Solo existen para Aggregate Roots, no para entidades hijas
/// - Las interfaces se definen en la capa de Dominio
/// - Las implementaciones van en la capa de Infrastructure
/// - Trabajan con agregados completos, no con partes de ellos
/// - Abstraen la persistencia del dominio
///
/// IMPORTANTE: Esta es una interface base genérica. Cada aggregate root debe
/// tener su propia interface específica que herede de esta y agregue métodos
/// especializados según las necesidades del negocio.
///
/// Ejemplo:
/// public interface IServiceRequestRepository : IRepository&lt;ServiceRequest, ServiceRequestId&gt;
/// {
///     Task&lt;IEnumerable&lt;ServiceRequest&gt;&gt; GetByTenantAsync(TenantId tenantId);
///     Task&lt;IEnumerable&lt;ServiceRequest&gt;&gt; GetOpenRequestsAsync();
/// }
/// </remarks>
public interface IRepository<TEntity, in TId>
    where TEntity : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    /// Obtiene un aggregate root por su identificador.
    /// </summary>
    /// <param name="id">El identificador único del aggregate root.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El aggregate root si existe, null en caso contrario.</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todos los aggregate roots.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Colección de todos los aggregate roots.</returns>
    /// <remarks>
    /// ADVERTENCIA: Usar con precaución. Para grandes volúmenes de datos,
    /// considera implementar paginación en interfaces específicas.
    /// </remarks>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega un nuevo aggregate root al repositorio.
    /// </summary>
    /// <param name="entity">El aggregate root a agregar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Este método solo marca la entidad para agregarse.
    /// Los cambios se persisten al llamar a UnitOfWork.SaveChangesAsync().
    /// </remarks>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un aggregate root existente.
    /// </summary>
    /// <param name="entity">El aggregate root a actualizar.</param>
    /// <remarks>
    /// Este método marca la entidad como modificada.
    /// Los cambios se persisten al llamar a UnitOfWork.SaveChangesAsync().
    /// </remarks>
    void Update(TEntity entity);

    /// <summary>
    /// Elimina un aggregate root del repositorio.
    /// </summary>
    /// <param name="entity">El aggregate root a eliminar.</param>
    /// <remarks>
    /// Este método marca la entidad para eliminación.
    /// Los cambios se persisten al llamar a UnitOfWork.SaveChangesAsync().
    ///
    /// CONSIDERACIÓN: En muchos casos, en lugar de eliminar físicamente,
    /// es mejor usar "soft delete" (marcar como eliminado) para mantener
    /// el historial y la trazabilidad.
    /// </remarks>
    void Delete(TEntity entity);
}
