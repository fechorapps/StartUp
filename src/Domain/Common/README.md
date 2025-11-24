# Domain Common - Clases Base

Este directorio contiene las clases base fundamentales para implementar Domain-Driven Design (DDD) en DoorX.

## 📁 Estructura

```
Common/
├── Entity.cs              # Clase base para entidades con identidad
├── AggregateRoot.cs       # Clase base para aggregate roots
├── ValueObject.cs         # Clase base para value objects
├── Errors/
│   └── DomainErrors.cs    # Errores del dominio centralizados
└── Interfaces/
    ├── IDomainEvent.cs    # Interface para domain events
    └── IRepository.cs     # Interface genérica de repositorio
```

## 🎯 Guía de Uso

### 1. Entity&lt;TId&gt;

**Cuándo usar:** Para objetos del dominio que se definen por su identidad única.

**Características:**
- Tiene un `Id` único
- Dos entidades son iguales si tienen el mismo `Id`
- Incluye `CreatedOnUtc` y `ModifiedOnUtc`

**Ejemplo:**

```csharp
// Definir el Id como Value Object (recomendado)
public record ServiceRequestId(Guid Value);

// Crear una entidad
public class ServiceRequestMessage : Entity<Guid>
{
    public string Content { get; private set; }
    public Guid SenderId { get; private set; }

    private ServiceRequestMessage(Guid id, string content, Guid senderId)
        : base(id)
    {
        Content = content;
        SenderId = senderId;
    }

    public static ServiceRequestMessage Create(string content, Guid senderId)
    {
        return new ServiceRequestMessage(Guid.NewGuid(), content, senderId);
    }
}
```

### 2. AggregateRoot&lt;TId&gt;

**Cuándo usar:** Para la entidad raíz de un agregado que garantiza consistencia.

**Características:**
- Hereda de `Entity<TId>`
- Puede generar y almacenar domain events
- Es el único punto de entrada para modificar el agregado
- Solo los aggregate roots tienen repositorios

**Ejemplo:**

```csharp
public record ServiceRequestId(Guid Value);

public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    private readonly List<VendorBid> _bids = new();

    public TenantId TenantId { get; private set; }
    public PropertyId PropertyId { get; private set; }
    public string Description { get; private set; }
    public ServiceRequestStatus Status { get; private set; }
    public IReadOnlyCollection<VendorBid> Bids => _bids.AsReadOnly();

    // Constructor privado - fuerza usar factory method
    private ServiceRequest(
        ServiceRequestId id,
        TenantId tenantId,
        PropertyId propertyId,
        string description) : base(id)
    {
        TenantId = tenantId;
        PropertyId = propertyId;
        Description = description;
        Status = ServiceRequestStatus.Pending;
    }

    // Constructor sin parámetros para EF Core
    private ServiceRequest() : base() { }

    // Factory method con validación
    public static ErrorOr<ServiceRequest> Create(
        TenantId tenantId,
        PropertyId propertyId,
        string description)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(description))
            return DomainErrors.ServiceRequest.InvalidDescription;

        var id = new ServiceRequestId(Guid.NewGuid());
        var request = new ServiceRequest(id, tenantId, propertyId, description);

        // Generar domain event
        request.AddDomainEvent(new ServiceRequestCreatedEvent(
            id,
            tenantId,
            DateTime.UtcNow));

        return request;
    }

    // Método de negocio
    public ErrorOr<Success> AddBid(VendorBid bid)
    {
        if (Status == ServiceRequestStatus.Cancelled)
            return DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

        if (_bids.Count >= 5)
            return DomainErrors.ServiceRequest.MaxBidsReached;

        _bids.Add(bid);
        UpdateModifiedDate();

        AddDomainEvent(new VendorBidReceivedEvent(Id, bid.VendorId, DateTime.UtcNow));

        return Result.Success;
    }
}
```

### 3. ValueObject

**Cuándo usar:** Para objetos que se definen por sus valores, no por identidad.

**Características:**
- Son inmutables
- La igualdad se determina por todos sus valores
- No tienen identidad propia
- Son intercambiables si tienen los mismos valores

**Ejemplo con clase:**

```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }

    private Address(string street, string city, string state, string zipCode)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public static ErrorOr<Address> Create(string street, string city, string state, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street))
            return DomainErrors.Property.InvalidAddress;

        if (string.IsNullOrWhiteSpace(zipCode) || zipCode.Length != 5)
            return DomainErrors.Property.InvalidAddress;

        return new Address(street, city, state, zipCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
    }
}
```

**Ejemplo con record (RECOMENDADO para value objects simples):**

```csharp
// Más simple y conciso usando C# records
public record Address(string Street, string City, string State, string ZipCode)
{
    public static ErrorOr<Address> Create(string street, string city, string state, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street))
            return DomainErrors.Property.InvalidAddress;

        if (string.IsNullOrWhiteSpace(zipCode) || zipCode.Length != 5)
            return DomainErrors.Property.InvalidAddress;

        return new Address(street, city, state, zipCode);
    }
}

// IDs como value objects fuertemente tipados
public record ServiceRequestId(Guid Value);
public record TenantId(Guid Value);
public record PropertyId(Guid Value);
public record VendorId(Guid Value);
```

### 4. IDomainEvent

**Cuándo usar:** Para comunicar que algo importante sucedió en el dominio.

**Características:**
- Son inmutables
- Representan hechos del pasado (usar tiempo pasado en nombres)
- Se generan dentro de los aggregate roots
- Se publican después de persistir exitosamente

**Ejemplo:**

```csharp
public record ServiceRequestCreatedEvent(
    ServiceRequestId ServiceRequestId,
    TenantId TenantId,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record VendorAssignedEvent(
    ServiceRequestId ServiceRequestId,
    VendorId VendorId,
    DateTime AssignedOnUtc,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
```

### 5. IRepository&lt;TEntity, TId&gt;

**Cuándo usar:** Para definir el contrato de persistencia de un aggregate root.

**Características:**
- Solo existe para aggregate roots
- La interface se define en Domain
- La implementación va en Infrastructure
- Incluye métodos especializados por agregado

**Ejemplo:**

```csharp
// En Domain layer
public interface IServiceRequestRepository : IRepository<ServiceRequest, ServiceRequestId>
{
    // Métodos especializados para este agregado
    Task<IEnumerable<ServiceRequest>> GetByTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ServiceRequest>> GetOpenRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ServiceRequest>> GetByPropertyAsync(
        PropertyId propertyId,
        CancellationToken cancellationToken = default);
}

// Uso en Application layer
public class GetServiceRequestsQueryHandler
{
    private readonly IServiceRequestRepository _repository;

    public GetServiceRequestsQueryHandler(IServiceRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<IEnumerable<ServiceRequest>>> Handle(
        GetServiceRequestsByTenantQuery query,
        CancellationToken cancellationToken)
    {
        var requests = await _repository.GetByTenantAsync(
            query.TenantId,
            cancellationToken);

        return requests.ToList();
    }
}
```

### 6. DomainErrors

**Cuándo usar:** Para retornar errores de validación y lógica de negocio.

**Características:**
- Centraliza todos los errores del dominio
- Usa el patrón ErrorOr en vez de excepciones
- Organizado por bounded context
- Tipos: Validation, NotFound, Conflict, Failure, Forbidden, Unexpected

**Ejemplo:**

```csharp
public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    public ErrorOr<Success> AssignVendor(VendorId vendorId)
    {
        // Validaciones retornan errores específicos
        if (Status == ServiceRequestStatus.Cancelled)
            return DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

        if (Status == ServiceRequestStatus.Completed)
            return DomainErrors.ServiceRequest.CannotCancelWhenCompleted;

        if (_assignedVendor is not null)
            return DomainErrors.ServiceRequest.AlreadyAssigned;

        // Lógica de negocio
        _assignedVendor = new AssignedVendor(vendorId);
        Status = ServiceRequestStatus.Assigned;
        UpdateModifiedDate();

        AddDomainEvent(new VendorAssignedEvent(
            Id,
            vendorId,
            DateTime.UtcNow,
            DateTime.UtcNow));

        return Result.Success;
    }
}

// En Application layer - manejo de errores
public async Task<ErrorOr<ServiceRequestResponse>> Handle(
    AssignVendorCommand command,
    CancellationToken cancellationToken)
{
    var request = await _repository.GetByIdAsync(command.ServiceRequestId, cancellationToken);

    if (request is null)
        return DomainErrors.ServiceRequest.NotFound;

    // AssignVendor retorna ErrorOr<Success>
    var result = request.AssignVendor(command.VendorId);

    if (result.IsError)
        return result.Errors; // Propaga los errores

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return new ServiceRequestResponse(request.Id, request.Status);
}
```

## 🎨 Patrones de Diseño Aplicados

### 1. Factory Pattern

Usar factory methods estáticos en vez de constructores públicos:

```csharp
public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    // ❌ NO: Constructor público
    // public ServiceRequest(TenantId tenantId, ...) { }

    // ✅ SÍ: Constructor privado + Factory Method
    private ServiceRequest(...) { }

    public static ErrorOr<ServiceRequest> Create(...)
    {
        // Validaciones
        // Lógica de creación
        // Domain events
        return new ServiceRequest(...);
    }
}
```

### 2. ErrorOr Pattern

Retornar `ErrorOr<T>` en vez de lanzar excepciones:

```csharp
// ❌ NO: Excepciones para control de flujo
public void AssignVendor(VendorId vendorId)
{
    if (Status == ServiceRequestStatus.Cancelled)
        throw new InvalidOperationException("Cannot assign when cancelled");
}

// ✅ SÍ: ErrorOr para flujo de errores explícito
public ErrorOr<Success> AssignVendor(VendorId vendorId)
{
    if (Status == ServiceRequestStatus.Cancelled)
        return DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

    // ... lógica
    return Result.Success;
}
```

### 3. Encapsulación de Colecciones

Proteger las colecciones internas del agregado:

```csharp
public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    // ❌ NO: Lista pública mutable
    // public List<VendorBid> Bids { get; set; }

    // ✅ SÍ: Lista privada + propiedad readonly
    private readonly List<VendorBid> _bids = new();
    public IReadOnlyCollection<VendorBid> Bids => _bids.AsReadOnly();

    // ✅ SÍ: Métodos para modificar la colección
    public ErrorOr<Success> AddBid(VendorBid bid)
    {
        // Validaciones
        _bids.Add(bid);
        return Result.Success;
    }
}
```

## ✅ Checklist de Mejores Prácticas

### Para Entities/Aggregates:
- [ ] Usa constructores privados
- [ ] Implementa factory methods con validación
- [ ] Retorna `ErrorOr<T>` en vez de excepciones
- [ ] Encapsula colecciones (lista privada + IReadOnlyCollection)
- [ ] Genera domain events para cambios importantes
- [ ] Usa `UpdateModifiedDate()` al modificar el estado
- [ ] Implementa IDs fuertemente tipados (records)

### Para Value Objects:
- [ ] Son inmutables (solo getters o init)
- [ ] Usa `record` para value objects simples
- [ ] Implementa factory method con validación
- [ ] Retorna `ErrorOr<T>` para creación

### Para Domain Events:
- [ ] Usa nombres en pasado (Created, Assigned, Completed)
- [ ] Son inmutables (preferiblemente records)
- [ ] Incluye toda la información relevante
- [ ] Genera automáticamente `EventId` y `OccurredOnUtc`

### Para Repositories:
- [ ] Solo para aggregate roots
- [ ] Interface en Domain, implementación en Infrastructure
- [ ] Hereda de `IRepository<TEntity, TId>`
- [ ] Agrega métodos especializados según necesidad

### Para Errores:
- [ ] Usa `DomainErrors` centralizado
- [ ] Organiza por bounded context
- [ ] Usa el tipo correcto (Validation, NotFound, Conflict, etc.)
- [ ] Mensajes descriptivos y útiles

## 📚 Próximos Pasos

Ahora que tienes las clases base, puedes empezar a implementar:

1. **ServiceRequest Bounded Context:**
   - `ServiceRequest` (aggregate root)
   - `ServiceRequestId`, `ServiceType`, `Priority` (value objects)
   - `ServiceRequestCreatedEvent`, etc. (domain events)
   - `IServiceRequestRepository` (interface)

2. **PropertyManagement Bounded Context:**
   - `Property`, `Tenant`, `Landlord` (aggregate roots)
   - `Address`, `PropertyId`, `TenantId` (value objects)
   - Domain events y repositories

3. **Tests Unitarios:**
   - Tests de entidades
   - Tests de value objects
   - Tests de domain events
   - Tests de validaciones

## 🔗 Referencias

- [ErrorOr Library](https://github.com/amantinband/error-or)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Clean Architecture - Robert Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
