# DoorX - Reglas de Negocio Críticas

> Documento que define las 10 reglas de negocio más críticas del sistema DoorX

## 📋 Índice

1. [Regla 1: Consistencia de Agregados](#regla-1-consistencia-de-agregados)
2. [Regla 2: Identidad de Entidades](#regla-2-identidad-de-entidades)
3. [Regla 3: Inmutabilidad de Value Objects](#regla-3-inmutabilidad-de-value-objects)
4. [Regla 4: Publicación de Domain Events](#regla-4-publicación-de-domain-events)
5. [Regla 5: Transiciones de Estado Válidas](#regla-5-transiciones-de-estado-válidas)
6. [Regla 6: Límite de Ofertas por Work Order](#regla-6-límite-de-ofertas-por-work-order)
7. [Regla 7: Asignación de Vendors](#regla-7-asignación-de-vendors)
8. [Regla 8: Auditoría Automática](#regla-8-auditoría-automática)
9. [Regla 9: Manejo de Errores sin Excepciones](#regla-9-manejo-de-errores-sin-excepciones)
10. [Regla 10: Separación de Responsabilidades por Bounded Context](#regla-10-separación-de-responsabilidades-por-bounded-context)

---

## Regla 1: Consistencia de Agregados

### 📌 Descripción
Un **Aggregate Root** es el único punto de entrada para modificar su agregado y debe garantizar que todas las invariantes del negocio se mantengan consistentes en todo momento.

### 🎯 Principio DDD
**Aggregate Pattern**: Los agregados son clusters de objetos de dominio que se pueden tratar como una unidad única para propósitos de cambios de datos.

### ✅ Reglas Específicas

1. **Solo el Aggregate Root puede ser accedido desde fuera del agregado**
   ```csharp
   // ✅ CORRECTO: Acceso a través del Aggregate Root
   var serviceRequest = await repository.GetByIdAsync(id);
   serviceRequest.AddBid(vendorBid);

   // ❌ INCORRECTO: Acceso directo a entidades hijas
   var bid = await bidRepository.GetByIdAsync(bidId); // NO existe bidRepository
   ```

2. **Las entidades hijas solo existen dentro del agregado**
   - Los `VendorBid` solo existen dentro de un `ServiceRequest`
   - Los `Message` solo existen dentro de una `Conversation`
   - No tienen repositorios independientes

3. **Las modificaciones deben pasar por métodos del Aggregate Root**
   ```csharp
   // ✅ CORRECTO: Método en el Aggregate Root
   public ErrorOr<Success> AddBid(VendorBid bid)
   {
       if (_bids.Count >= 5)
           return DomainErrors.ServiceRequest.MaxBidsReached;

       _bids.Add(bid);
       UpdateModifiedDate();
       AddDomainEvent(new VendorBidReceivedEvent(Id, bid.VendorId));
       return Result.Success;
   }

   // ❌ INCORRECTO: Exponer la lista para modificación directa
   public List<VendorBid> Bids { get; set; } // Nunca hacer esto
   ```

### 🔍 Ejemplo Práctico
```csharp
// Aggregate Root: ServiceRequest
public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    // Lista privada - NO expuesta para modificación
    private readonly List<VendorBid> _bids = new();

    // Propiedad de solo lectura
    public IReadOnlyCollection<VendorBid> Bids => _bids.AsReadOnly();

    // Método que garantiza invariantes
    public ErrorOr<Success> AddBid(VendorBid bid)
    {
        // Validación de estado
        if (Status == ServiceRequestStatus.Cancelled)
            return DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

        // Validación de regla de negocio
        if (_bids.Count >= 5)
            return DomainErrors.ServiceRequest.MaxBidsReached;

        // Modificación controlada
        _bids.Add(bid);
        UpdateModifiedDate();

        // Event de dominio
        AddDomainEvent(new VendorBidReceivedEvent(Id, bid.VendorId));

        return Result.Success;
    }
}
```

### ⚠️ Violaciones Comunes
- Exponer colecciones con `List<T>` en vez de `IReadOnlyCollection<T>`
- Crear repositorios para entidades hijas
- Permitir modificación directa de propiedades sin validación
- No validar invariantes antes de modificar el estado

---

## Regla 2: Identidad de Entidades

### 📌 Descripción
Las **entidades se definen por su identidad**, no por sus atributos. Dos entidades con el mismo `Id` son consideradas la misma entidad, independientemente de sus valores.

### 🎯 Principio DDD
**Entity Pattern**: Los objetos que tienen identidad conceptual deben ser modelados como entidades.

### ✅ Reglas Específicas

1. **La igualdad se determina solo por el Id**
   ```csharp
   var tenant1 = new Tenant(tenantId, "John Doe", "john@email.com");
   var tenant2 = new Tenant(tenantId, "Jane Doe", "jane@email.com");

   // Son iguales porque tienen el mismo Id
   tenant1 == tenant2 // true
   ```

2. **Los IDs deben ser fuertemente tipados usando records**
   ```csharp
   // ✅ CORRECTO: IDs fuertemente tipados
   public record ServiceRequestId(Guid Value);
   public record TenantId(Guid Value);
   public record VendorId(Guid Value);

   // ❌ INCORRECTO: Usar Guid directamente
   public Guid ServiceRequestId { get; set; } // No hacer esto
   ```

3. **El Id es inmutable (solo init)**
   ```csharp
   public abstract class Entity<TId>
   {
       public TId Id { get; protected init; } // init, NO set
   }
   ```

### 🔍 Ejemplo Práctico
```csharp
// Definición de IDs fuertemente tipados
public record TenantId(Guid Value);

// Entidad con identidad
public class Tenant : AggregateRoot<TenantId>
{
    public string Name { get; private set; }
    public string Email { get; private set; }

    private Tenant(TenantId id, string name, string email) : base(id)
    {
        Name = name;
        Email = email;
    }

    public static ErrorOr<Tenant> Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return DomainErrors.Tenant.InvalidEmail;

        var id = new TenantId(Guid.NewGuid());
        return new Tenant(id, name, email);
    }

    // Dos tenants con el mismo Id son iguales
    // Aunque tengan nombre o email diferente
}

// Uso
var tenant1 = Tenant.Create("John", "john@email.com").Value;
var tenant2 = await repository.GetByIdAsync(tenant1.Id);

// Son la misma entidad (mismo Id)
tenant1 == tenant2 // true (heredado de Entity<TId>)
```

### ⚠️ Violaciones Comunes
- Comparar entidades por sus atributos en vez de por Id
- Usar `Guid` directamente en vez de IDs fuertemente tipados
- Hacer el Id mutable (setter público)
- No implementar correctamente `Equals` y `GetHashCode`

---

## Regla 3: Inmutabilidad de Value Objects

### 📌 Descripción
Los **Value Objects son inmutables** y se definen por sus valores. Dos Value Objects con los mismos valores son intercambiables.

### 🎯 Principio DDD
**Value Object Pattern**: Objetos que describen características del dominio y son completamente intercambiables cuando sus valores son iguales.

### ✅ Reglas Específicas

1. **Value Objects no tienen identidad**
   ```csharp
   var address1 = new Address("123 Main St", "NYC", "NY", "10001");
   var address2 = new Address("123 Main St", "NYC", "NY", "10001");

   // Son iguales porque tienen los mismos valores
   address1 == address2 // true
   ```

2. **Todas las propiedades son de solo lectura**
   ```csharp
   // ✅ CORRECTO: Propiedades inmutables
   public record Address(string Street, string City, string State, string ZipCode);

   // ❌ INCORRECTO: Propiedades mutables
   public class Address
   {
       public string Street { get; set; } // NO hacer esto
   }
   ```

3. **Para modificar un Value Object, se crea uno nuevo**
   ```csharp
   // No se modifica el value object existente
   var newAddress = address with { Street = "456 Oak Ave" };
   ```

### 🔍 Ejemplo Práctico
```csharp
// ✅ Usando C# records (RECOMENDADO)
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

// Uso
var addressResult = Address.Create("123 Main St", "NYC", "NY", "10001");

if (addressResult.IsError)
    return addressResult.Errors;

var address = addressResult.Value;

// Para "cambiar" la dirección, se crea una nueva
var updatedAddress = address with { Street = "456 Oak Ave" };
```

### 🔍 Enumeraciones de Dominio
```csharp
// Value Objects para categorías
public record ServiceCategory
{
    public static readonly ServiceCategory Plumbing = new("Plumbing");
    public static readonly ServiceCategory Electrical = new("Electrical");
    public static readonly ServiceCategory HVAC = new("HVAC");
    public static readonly ServiceCategory Appliance = new("Appliance");

    public string Value { get; }

    private ServiceCategory(string value) => Value = value;
}

// Prioridades
public record Priority
{
    public static readonly Priority Emergency = new("Emergency", 1);
    public static readonly Priority High = new("High", 2);
    public static readonly Priority Normal = new("Normal", 3);
    public static readonly Priority Low = new("Low", 4);

    public string Name { get; }
    public int Level { get; }

    private Priority(string name, int level)
    {
        Name = name;
        Level = level;
    }
}
```

### ⚠️ Violaciones Comunes
- Crear Value Objects con propiedades mutables (`set`)
- Darle identidad (Id) a un Value Object
- No validar los valores en el factory method
- Usar clases mutables en vez de records inmutables

---

## Regla 4: Publicación de Domain Events

### 📌 Descripción
Los **Domain Events** representan hechos importantes que ocurrieron en el dominio y deben ser publicados **después** de que la transacción se complete exitosamente.

### 🎯 Principio DDD
**Domain Events Pattern**: Eventos que representan algo significativo que ocurrió en el dominio y que otros bounded contexts pueden necesitar saber.

### ✅ Reglas Específicas

1. **Los eventos se generan durante la operación del dominio**
   ```csharp
   public ErrorOr<ServiceRequest> Create(TenantId tenantId, PropertyId propertyId, string description)
   {
       var request = new ServiceRequest(id, tenantId, propertyId, description);

       // Evento generado inmediatamente
       request.AddDomainEvent(new ServiceRequestCreatedEvent(id, tenantId, DateTime.UtcNow));

       return request;
   }
   ```

2. **Los eventos se publican DESPUÉS de persistir exitosamente**
   ```csharp
   // En Application Layer / Handler
   var request = ServiceRequest.Create(tenantId, propertyId, description).Value;

   // 1. Guardar en la base de datos
   await repository.AddAsync(request);
   await unitOfWork.SaveChangesAsync();

   // 2. Solo después de guardar exitosamente, publicar eventos
   await eventPublisher.PublishAsync(request.DomainEvents);
   request.ClearDomainEvents();
   ```

3. **Los eventos son inmutables y usan tiempo pasado**
   ```csharp
   // ✅ CORRECTO: Nombres en pasado, inmutables (record)
   public record ServiceRequestCreatedEvent(
       ServiceRequestId ServiceRequestId,
       TenantId TenantId,
       DateTime OccurredOnUtc) : IDomainEvent
   {
       public Guid EventId { get; } = Guid.NewGuid();
   }

   // ❌ INCORRECTO: Nombre en presente
   public record ServiceRequestCreateEvent(...) // NO usar presente
   ```

### 🔍 Ejemplo Práctico
```csharp
// Domain Event
public record VendorAssignedEvent(
    ServiceRequestId ServiceRequestId,
    VendorId VendorId,
    DateTime AssignedOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

// En el Aggregate Root
public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    public ErrorOr<Success> AssignVendor(VendorId vendorId)
    {
        if (Status == ServiceRequestStatus.Cancelled)
            return DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

        _assignedVendor = new AssignedVendor(vendorId);
        Status = ServiceRequestStatus.Assigned;
        UpdateModifiedDate();

        // Generar el evento
        AddDomainEvent(new VendorAssignedEvent(Id, vendorId, DateTime.UtcNow));

        return Result.Success;
    }
}

// En Application Layer
public class AssignVendorCommandHandler
{
    private readonly IServiceRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _eventPublisher;

    public async Task<ErrorOr<Success>> Handle(AssignVendorCommand command)
    {
        var request = await _repository.GetByIdAsync(command.ServiceRequestId);

        if (request is null)
            return DomainErrors.ServiceRequest.NotFound;

        // Ejecutar lógica de dominio
        var result = request.AssignVendor(command.VendorId);

        if (result.IsError)
            return result.Errors;

        // 1. Persistir cambios
        await _unitOfWork.SaveChangesAsync();

        // 2. Publicar eventos (solo si SaveChanges fue exitoso)
        await _eventPublisher.PublishAsync(request.DomainEvents);

        // 3. Limpiar eventos
        request.ClearDomainEvents();

        return Result.Success;
    }
}
```

### ⚠️ Violaciones Comunes
- Publicar eventos ANTES de persistir (pueden perderse si falla la transacción)
- Usar nombres en presente (CreateEvent en vez de CreatedEvent)
- Hacer los eventos mutables
- No incluir `EventId` único
- No incluir `OccurredOnUtc` en UTC

---

## Regla 5: Transiciones de Estado Válidas

### 📌 Descripción
Las **transiciones de estado** deben ser explícitas y validadas. No todas las transiciones son válidas desde cualquier estado.

### 🎯 Principio DDD
**State Pattern**: El comportamiento de un objeto cambia según su estado, y ciertas operaciones solo son válidas en ciertos estados.

### ✅ Reglas Específicas

1. **Definir el flujo de estados del Work Order**
   ```
   Open → Categorized → VendorSearch → Bidding → Scheduled → InProgress → Completed → Closed
                                ↓
                            Cancelled (solo desde Open/Categorized/Bidding)
   ```

2. **Validar transiciones antes de cambiar el estado**
   ```csharp
   public ErrorOr<Success> AssignVendor(VendorId vendorId)
   {
       // Validar que el estado actual permita asignación
       if (Status == ServiceRequestStatus.Cancelled)
           return DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

       if (Status == ServiceRequestStatus.Completed)
           return DomainErrors.ServiceRequest.CannotAssignWhenCompleted;

       // Cambio de estado válido
       Status = ServiceRequestStatus.Assigned;
       _assignedVendor = vendorId;

       return Result.Success;
   }
   ```

3. **Las operaciones dependen del estado**
   ```csharp
   // Solo se puede cancelar si NO está completado
   public ErrorOr<Success> Cancel(string reason)
   {
       if (Status == ServiceRequestStatus.Completed)
           return DomainErrors.ServiceRequest.CannotCancelWhenCompleted;

       Status = ServiceRequestStatus.Cancelled;
       _cancellationReason = reason;
       AddDomainEvent(new ServiceRequestCancelledEvent(Id, reason));

       return Result.Success;
   }
   ```

### 🔍 Ejemplo Práctico
```csharp
public enum WorkOrderStatus
{
    Open = 1,
    Categorized = 2,
    VendorSearch = 3,
    Bidding = 4,
    Scheduled = 5,
    InProgress = 6,
    Completed = 7,
    Closed = 8,
    Cancelled = 99
}

public class WorkOrder : AggregateRoot<WorkOrderId>
{
    public WorkOrderStatus Status { get; private set; }

    public ErrorOr<Success> StartWork()
    {
        // Solo se puede iniciar desde Scheduled
        if (Status != WorkOrderStatus.Scheduled)
            return Error.Conflict(
                code: "WorkOrder.InvalidStatusTransition",
                description: $"Cannot start work from status {Status}");

        Status = WorkOrderStatus.InProgress;
        UpdateModifiedDate();
        AddDomainEvent(new WorkStartedEvent(Id, DateTime.UtcNow));

        return Result.Success;
    }

    public ErrorOr<Success> CompleteWork()
    {
        // Solo se puede completar desde InProgress
        if (Status != WorkOrderStatus.InProgress)
            return Error.Conflict(
                code: "WorkOrder.InvalidStatusTransition",
                description: $"Cannot complete work from status {Status}");

        Status = WorkOrderStatus.Completed;
        UpdateModifiedDate();
        AddDomainEvent(new WorkCompletedEvent(Id, DateTime.UtcNow));

        return Result.Success;
    }

    public ErrorOr<Success> Cancel(string reason)
    {
        // Solo se puede cancelar en estados tempranos
        var cancellableStatuses = new[]
        {
            WorkOrderStatus.Open,
            WorkOrderStatus.Categorized,
            WorkOrderStatus.Bidding
        };

        if (!cancellableStatuses.Contains(Status))
            return Error.Conflict(
                code: "WorkOrder.CannotCancelInCurrentStatus",
                description: $"Cannot cancel from status {Status}");

        Status = WorkOrderStatus.Cancelled;
        _cancellationReason = reason;
        AddDomainEvent(new WorkOrderCancelledEvent(Id, reason, DateTime.UtcNow));

        return Result.Success;
    }
}
```

### ⚠️ Violaciones Comunes
- Permitir cambios de estado sin validación
- No documentar el flujo de estados válido
- Hacer el Status público con setter
- No generar eventos de dominio en transiciones importantes

---

## Regla 6: Límite de Ofertas por Work Order

### 📌 Descripción
Un **Work Order** puede recibir un **máximo de 5 ofertas (bids)** de diferentes vendors para evitar saturación y mantener calidad.

### 🎯 Razón de Negocio
- Facilitar la decisión del Property Manager
- Evitar sobrecarga de cotizaciones
- Mantener competitividad sin exceso

### ✅ Reglas Específicas

1. **Máximo 5 ofertas por Work Order**
   ```csharp
   public ErrorOr<Success> AddBid(VendorBid bid)
   {
       if (_bids.Count >= 5)
           return DomainErrors.ServiceRequest.MaxBidsReached;

       _bids.Add(bid);
       return Result.Success;
   }
   ```

2. **Un vendor solo puede enviar una oferta por Work Order**
   ```csharp
   public ErrorOr<Success> AddBid(VendorBid bid)
   {
       if (_bids.Any(b => b.VendorId == bid.VendorId))
           return DomainErrors.Vendor.DuplicateBid;

       if (_bids.Count >= 5)
           return DomainErrors.ServiceRequest.MaxBidsReached;

       _bids.Add(bid);
       AddDomainEvent(new VendorBidReceivedEvent(Id, bid.VendorId, DateTime.UtcNow));

       return Result.Success;
   }
   ```

3. **No se aceptan ofertas si el Work Order ya tiene vendor asignado**
   ```csharp
   public ErrorOr<Success> AddBid(VendorBid bid)
   {
       if (Status == WorkOrderStatus.Scheduled || _assignedVendor is not null)
           return DomainErrors.ServiceRequest.AlreadyAssigned;

       // ... resto de validaciones
   }
   ```

### 🔍 Ejemplo Práctico
```csharp
public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    private readonly List<VendorBid> _bids = new();
    private const int MaxBidsAllowed = 5;

    public IReadOnlyCollection<VendorBid> Bids => _bids.AsReadOnly();

    public ErrorOr<Success> AddBid(VendorBid bid)
    {
        // 1. Validar que no esté ya asignado
        if (_assignedVendor is not null)
            return DomainErrors.ServiceRequest.AlreadyAssigned;

        // 2. Validar que no esté cancelado
        if (Status == ServiceRequestStatus.Cancelled)
            return DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

        // 3. Validar que el vendor no haya enviado oferta previamente
        if (_bids.Any(b => b.VendorId == bid.VendorId))
            return DomainErrors.Vendor.DuplicateBid;

        // 4. Validar límite de ofertas
        if (_bids.Count >= MaxBidsAllowed)
            return DomainErrors.ServiceRequest.MaxBidsReached;

        // 5. Agregar oferta
        _bids.Add(bid);
        UpdateModifiedDate();

        // 6. Generar evento
        AddDomainEvent(new VendorBidReceivedEvent(
            Id,
            bid.VendorId,
            bid.Amount,
            DateTime.UtcNow));

        return Result.Success;
    }
}
```

### ⚠️ Violaciones Comunes
- No validar el límite de ofertas
- Permitir ofertas duplicadas del mismo vendor
- Aceptar ofertas después de asignar un vendor
- No generar eventos cuando se rechaza una oferta por límite

---

## Regla 7: Asignación de Vendors

### 📌 Descripción
La **asignación de un vendor** debe cumplir requisitos específicos: estar calificado, disponible, dentro del área de servicio, y el Work Order debe estar en estado válido.

### 🎯 Razón de Negocio
- Garantizar calidad del servicio
- Cumplir con SLAs
- Evitar asignaciones inválidas que requieran reasignación

### ✅ Reglas Específicas

1. **El vendor debe estar calificado para la categoría de servicio**
   ```csharp
   public ErrorOr<Success> AssignVendor(Vendor vendor)
   {
       if (!vendor.ServiceCategories.Contains(this.Category))
           return DomainErrors.Vendor.NotQualified;

       // ... continuar asignación
   }
   ```

2. **El vendor debe cubrir el área de servicio (ZIP code)**
   ```csharp
   public ErrorOr<Success> AssignVendor(Vendor vendor, Property property)
   {
       if (!vendor.ServiceAreas.Contains(property.Address.ZipCode))
           return DomainErrors.Vendor.OutsideServiceArea;

       // ... continuar asignación
   }
   ```

3. **El vendor debe estar disponible**
   ```csharp
   public ErrorOr<Success> AssignVendor(Vendor vendor)
   {
       if (!vendor.IsAvailable)
           return DomainErrors.Vendor.NotAvailable;

       // ... continuar asignación
   }
   ```

4. **Un Work Order solo puede tener un vendor asignado a la vez**
   ```csharp
   public ErrorOr<Success> AssignVendor(VendorId vendorId)
   {
       if (_assignedVendor is not null)
           return DomainErrors.ServiceRequest.AlreadyAssigned;

       _assignedVendor = vendorId;
       Status = ServiceRequestStatus.Scheduled;

       return Result.Success;
   }
   ```

### 🔍 Ejemplo Práctico
```csharp
// En Application Layer - AssignVendorCommandHandler
public class AssignVendorCommandHandler
{
    public async Task<ErrorOr<Success>> Handle(AssignVendorCommand command)
    {
        // 1. Obtener entities
        var serviceRequest = await _serviceRequestRepository.GetByIdAsync(command.ServiceRequestId);
        var vendor = await _vendorRepository.GetByIdAsync(command.VendorId);
        var property = await _propertyRepository.GetByIdAsync(serviceRequest.PropertyId);

        if (serviceRequest is null)
            return DomainErrors.ServiceRequest.NotFound;
        if (vendor is null)
            return DomainErrors.Vendor.NotFound;
        if (property is null)
            return DomainErrors.Property.NotFound;

        // 2. Validar que el vendor esté calificado
        if (!vendor.IsQualifiedFor(serviceRequest.Category))
            return DomainErrors.Vendor.NotQualified;

        // 3. Validar área de servicio
        if (!vendor.CoversArea(property.Address.ZipCode))
            return DomainErrors.Vendor.OutsideServiceArea;

        // 4. Validar disponibilidad
        if (!vendor.IsAvailable)
            return DomainErrors.Vendor.NotAvailable;

        // 5. Asignar (el domain model valida su propio estado)
        var result = serviceRequest.AssignVendor(command.VendorId);

        if (result.IsError)
            return result.Errors;

        // 6. Persistir
        await _unitOfWork.SaveChangesAsync();

        return Result.Success;
    }
}

// En Domain - Vendor
public class Vendor : AggregateRoot<VendorId>
{
    private readonly List<ServiceCategory> _serviceCategories = new();
    private readonly List<string> _serviceAreas = new(); // ZIP codes

    public bool IsAvailable { get; private set; }
    public IReadOnlyCollection<ServiceCategory> ServiceCategories => _serviceCategories.AsReadOnly();

    public bool IsQualifiedFor(ServiceCategory category)
    {
        return _serviceCategories.Contains(category);
    }

    public bool CoversArea(string zipCode)
    {
        return _serviceAreas.Contains(zipCode);
    }
}
```

### ⚠️ Violaciones Comunes
- No validar calificaciones del vendor
- No verificar área de servicio
- Permitir múltiples vendors asignados simultáneamente
- No verificar disponibilidad antes de asignar

---

## Regla 8: Auditoría Automática

### 📌 Descripción
Todas las entidades auditables deben registrar automáticamente **cuándo fueron creadas** (`CreatedOnUtc`) y **cuándo fueron modificadas** (`ModifiedOnUtc`).

### 🎯 Razón de Negocio
- Trazabilidad completa de cambios
- Cumplimiento regulatorio
- Debugging y auditoría de operaciones
- Análisis de tiempos de respuesta

### ✅ Reglas Específicas

1. **CreatedOnUtc se establece automáticamente en la creación**
   ```csharp
   public abstract class AuditableEntity<TId> : Entity<TId>
   {
       public DateTime CreatedOnUtc { get; private init; }
       public DateTime? ModifiedOnUtc { get; private set; }

       protected AuditableEntity(TId id) : base(id)
       {
           CreatedOnUtc = DateTime.UtcNow; // Automático
       }
   }
   ```

2. **ModifiedOnUtc se actualiza en cada modificación**
   ```csharp
   protected void UpdateModifiedDate()
   {
       ModifiedOnUtc = DateTime.UtcNow;
   }
   ```

3. **Todos los Aggregate Roots heredan auditoría automáticamente**
   ```csharp
   // AggregateRoot hereda de AuditableEntity
   public abstract class AggregateRoot<TId> : AuditableEntity<TId>
   {
       // Auditoría incluida automáticamente
   }
   ```

4. **Usar UpdateModifiedDate() en todas las operaciones que modifiquen estado**
   ```csharp
   public ErrorOr<Success> AssignVendor(VendorId vendorId)
   {
       // Validaciones...

       _assignedVendor = vendorId;
       Status = ServiceRequestStatus.Assigned;

       UpdateModifiedDate(); // IMPORTANTE

       AddDomainEvent(new VendorAssignedEvent(Id, vendorId, DateTime.UtcNow));

       return Result.Success;
   }
   ```

### 🔍 Ejemplo Práctico
```csharp
// Clase base AuditableEntity
public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Fecha y hora de creación en UTC.
    /// Se establece automáticamente al crear la entidad.
    /// </summary>
    public DateTime CreatedOnUtc { get; private init; }

    /// <summary>
    /// Fecha y hora de última modificación en UTC.
    /// Null si nunca ha sido modificada.
    /// </summary>
    public DateTime? ModifiedOnUtc { get; private set; }

    protected AuditableEntity(TId id) : base(id)
    {
        CreatedOnUtc = DateTime.UtcNow;
    }

    protected AuditableEntity() : base()
    {
        CreatedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la fecha de modificación al momento actual.
    /// Debe ser llamado en todo método que modifique el estado de la entidad.
    /// </summary>
    protected void UpdateModifiedDate()
    {
        ModifiedOnUtc = DateTime.UtcNow;
    }
}

// Uso en Aggregate Root
public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    public ErrorOr<Success> AddBid(VendorBid bid)
    {
        // Validaciones...

        _bids.Add(bid);
        UpdateModifiedDate(); // Actualizar auditoría

        AddDomainEvent(new VendorBidReceivedEvent(Id, bid.VendorId, DateTime.UtcNow));

        return Result.Success;
    }

    public ErrorOr<Success> Cancel(string reason)
    {
        // Validaciones...

        Status = ServiceRequestStatus.Cancelled;
        _cancellationReason = reason;
        UpdateModifiedDate(); // Actualizar auditoría

        AddDomainEvent(new ServiceRequestCancelledEvent(Id, reason, DateTime.UtcNow));

        return Result.Success;
    }
}

// Query para análisis
// "¿Cuánto tiempo pasó desde la creación hasta la asignación?"
var timeToAssignment = serviceRequest.ModifiedOnUtc.Value - serviceRequest.CreatedOnUtc;
```

### ⚠️ Violaciones Comunes
- Olvidar llamar `UpdateModifiedDate()` en métodos que modifican estado
- Usar `DateTime.Now` en vez de `DateTime.UtcNow`
- Hacer las propiedades de auditoría públicas y mutables
- No usar auditoría en entidades importantes

---

## Regla 9: Manejo de Errores sin Excepciones

### 📌 Descripción
El dominio debe usar el **patrón ErrorOr** para comunicar errores de validación y lógica de negocio, **en vez de lanzar excepciones**.

### 🎯 Razón de Negocio
- Los errores de negocio son flujo normal, no excepcional
- Mejor rendimiento (sin stack unwinding)
- Type-safe: el compilador obliga a manejar errores
- Código más limpio y expresivo

### ✅ Reglas Específicas

1. **Los métodos de dominio retornan ErrorOr&lt;T&gt;**
   ```csharp
   // ✅ CORRECTO: Retorna ErrorOr<Success>
   public ErrorOr<Success> AssignVendor(VendorId vendorId)
   {
       if (Status == ServiceRequestStatus.Cancelled)
           return DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

       // ... lógica
       return Result.Success;
   }

   // ❌ INCORRECTO: Lanza excepción
   public void AssignVendor(VendorId vendorId)
   {
       if (Status == ServiceRequestStatus.Cancelled)
           throw new InvalidOperationException("Cannot assign when cancelled");
   }
   ```

2. **Usar DomainErrors centralizado**
   ```csharp
   // Todos los errores definidos en un solo lugar
   public static class DomainErrors
   {
       public static class ServiceRequest
       {
           public static Error NotFound => Error.NotFound(
               code: "ServiceRequest.NotFound",
               description: "La solicitud de servicio no fue encontrada.");

           public static Error CannotAssignWhenCancelled => Error.Conflict(
               code: "ServiceRequest.CannotAssignWhenCancelled",
               description: "No se puede asignar un contratista a una solicitud cancelada.");
       }
   }
   ```

3. **Propagar errores correctamente en Application Layer**
   ```csharp
   public async Task<ErrorOr<ServiceRequestResponse>> Handle(AssignVendorCommand command)
   {
       var request = await _repository.GetByIdAsync(command.ServiceRequestId);

       if (request is null)
           return DomainErrors.ServiceRequest.NotFound;

       // AssignVendor retorna ErrorOr<Success>
       var result = request.AssignVendor(command.VendorId);

       if (result.IsError)
           return result.Errors; // Propagar errores

       await _unitOfWork.SaveChangesAsync();

       return new ServiceRequestResponse(request.Id, request.Status);
   }
   ```

### 🔍 Ejemplo Práctico
```csharp
// Domain Layer
public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    public ErrorOr<Success> AssignVendor(VendorId vendorId)
    {
        // Validaciones que retornan errores específicos
        if (_assignedVendor is not null)
            return DomainErrors.ServiceRequest.AlreadyAssigned;

        if (Status == ServiceRequestStatus.Cancelled)
            return DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

        if (Status == ServiceRequestStatus.Completed)
            return DomainErrors.ServiceRequest.CannotAssignWhenCompleted;

        // Lógica de negocio
        _assignedVendor = vendorId;
        Status = ServiceRequestStatus.Assigned;
        UpdateModifiedDate();

        AddDomainEvent(new VendorAssignedEvent(Id, vendorId, DateTime.UtcNow));

        return Result.Success;
    }
}

// Application Layer
public class AssignVendorCommandHandler
{
    public async Task<ErrorOr<ServiceRequestResponse>> Handle(AssignVendorCommand command)
    {
        var request = await _repository.GetByIdAsync(command.ServiceRequestId);

        if (request is null)
            return DomainErrors.ServiceRequest.NotFound;

        // Llamar al método del dominio
        var result = request.AssignVendor(command.VendorId);

        // Verificar si hay errores
        if (result.IsError)
            return result.Errors; // Propagar errores

        // Si fue exitoso, persistir
        await _unitOfWork.SaveChangesAsync();

        return new ServiceRequestResponse(request.Id, request.Status);
    }
}

// API Layer (Controller)
[HttpPost("assign-vendor")]
public async Task<IActionResult> AssignVendor(AssignVendorCommand command)
{
    var result = await _mediator.Send(command);

    // Match pattern para convertir ErrorOr a IActionResult
    return result.Match(
        success => Ok(success),
        errors => Problem(errors));
}
```

### 🔍 Tipos de Errores
```csharp
// Error.Validation - Datos inválidos
if (string.IsNullOrWhiteSpace(description))
    return Error.Validation(code: "ServiceRequest.InvalidDescription", description: "...");

// Error.NotFound - Recurso no encontrado
if (request is null)
    return Error.NotFound(code: "ServiceRequest.NotFound", description: "...");

// Error.Conflict - Estado inválido para la operación
if (Status == ServiceRequestStatus.Cancelled)
    return Error.Conflict(code: "ServiceRequest.CannotAssignWhenCancelled", description: "...");

// Error.Failure - Fallo de operación
if (!externalServiceResponse.Success)
    return Error.Failure(code: "Integration.SyncFailed", description: "...");

// Error.Forbidden - Sin permisos
if (!user.CanApprove(request))
    return Error.Forbidden(code: "General.Unauthorized", description: "...");
```

### ⚠️ Violaciones Comunes
- Usar excepciones para control de flujo de negocio
- No manejar errores en Application Layer
- Retornar `void` en vez de `ErrorOr<Success>`
- No usar el DomainErrors centralizado
- Crear errores inline en vez de definirlos en DomainErrors

---

## Regla 10: Separación de Responsabilidades por Bounded Context

### 📌 Descripción
El sistema DoorX se organiza en **Bounded Contexts** independientes con responsabilidades bien definidas. Cada contexto maneja su propio dominio y no debe conocer detalles de implementación de otros contextos.

### 🎯 Principio DDD
**Bounded Context Pattern**: Límites explícitos dentro de los cuales un modelo de dominio particular es definido y aplicable.

### ✅ Bounded Contexts de DoorX

#### 1. **ServiceRequest Context**
**Responsabilidad:** Gestión del ciclo de vida de solicitudes de mantenimiento

**Entidades:**
- `ServiceRequest` (Aggregate Root)
- `VendorBid` (Entity)
- `ServiceCategory` (Value Object)
- `Priority` (Value Object)

**Reglas:**
- Creación y categorización de solicitudes
- Gestión de ofertas de vendors
- Asignación de vendor
- Transiciones de estado
- Límite de 5 ofertas por solicitud

#### 2. **PropertyManagement Context**
**Responsabilidad:** Gestión de propiedades e inquilinos

**Entidades:**
- `Property` (Aggregate Root)
- `Tenant` (Aggregate Root)
- `PropertyManager` (Aggregate Root)
- `Address` (Value Object)

**Reglas:**
- Una propiedad solo puede tener un tenant activo a la vez
- Los tenants activos pueden crear Work Orders
- Property Manager administra múltiples propiedades

#### 3. **ContractorManagement Context**
**Responsabilidad:** Gestión de vendors y sus capacidades

**Entidades:**
- `Vendor` (Aggregate Root)
- `ServiceArea` (Value Object)
- `Rating` (Value Object)

**Reglas:**
- Vendors tienen categorías de servicio específicas
- Vendors tienen áreas de servicio (ZIP codes)
- Rating se actualiza basado en trabajos completados

#### 4. **AIAssistant Context**
**Responsabilidad:** Gestión de conversaciones con IA

**Entidades:**
- `Conversation` (Aggregate Root)
- `Message` (Entity)
- `Participant` (Value Object)

**Reglas:**
- Una conversación está asociada a un Work Order
- Mensajes de Tenant, Vendor y Aimee (IA)
- Canales: SMS, WhatsApp, WebChat

#### 5. **IntegrationPlatform Context**
**Responsabilidad:** Sincronización con sistemas externos (PMS)

**Entidades:**
- `ExternalSystemConfig` (Aggregate Root)
- `SyncStatus` (Value Object)

**Reglas:**
- Solo sincroniza datos básicos (properties, tenants, vendors)
- NO maneja finanzas, rentas, leases
- Mantiene ExternalWorkOrderId para trazabilidad

### ✅ Reglas de Comunicación entre Contextos

1. **Comunicación mediante Domain Events**
   ```csharp
   // ServiceRequest Context genera evento
   public record ServiceRequestCreatedEvent(
       ServiceRequestId ServiceRequestId,
       PropertyId PropertyId,
       TenantId TenantId,
       DateTime OccurredOnUtc) : IDomainEvent;

   // AIAssistant Context escucha y reacciona
   public class ServiceRequestCreatedEventHandler
   {
       public async Task Handle(ServiceRequestCreatedEvent @event)
       {
           // Crear conversación automáticamente
           var conversation = Conversation.Create(@event.ServiceRequestId, @event.TenantId);
           await _conversationRepository.AddAsync(conversation);
       }
   }
   ```

2. **Referencias entre contextos solo por Id**
   ```csharp
   // ✅ CORRECTO: Solo guardar el Id
   public class ServiceRequest : AggregateRoot<ServiceRequestId>
   {
       public TenantId TenantId { get; private set; }
       public PropertyId PropertyId { get; private set; }
       public VendorId? AssignedVendorId { get; private set; }
   }

   // ❌ INCORRECTO: Guardar objetos completos de otros contextos
   public class ServiceRequest : AggregateRoot<ServiceRequestId>
   {
       public Tenant Tenant { get; set; } // NO hacer esto
       public Property Property { get; set; } // NO hacer esto
   }
   ```

3. **Consultas cross-context en Application Layer**
   ```csharp
   // Application Layer puede consultar múltiples contextos
   public class GetServiceRequestDetailsQueryHandler
   {
       private readonly IServiceRequestRepository _requestRepo;
       private readonly ITenantRepository _tenantRepo;
       private readonly IPropertyRepository _propertyRepo;

       public async Task<ServiceRequestDetailsDto> Handle(GetServiceRequestDetailsQuery query)
       {
           // Obtener de diferentes contextos
           var request = await _requestRepo.GetByIdAsync(query.Id);
           var tenant = await _tenantRepo.GetByIdAsync(request.TenantId);
           var property = await _propertyRepo.GetByIdAsync(request.PropertyId);

           // Combinar en DTO
           return new ServiceRequestDetailsDto
           {
               RequestId = request.Id,
               Description = request.Description,
               TenantName = tenant.Name,
               PropertyAddress = property.Address.ToString()
           };
       }
   }
   ```

### ✅ Límites de Responsabilidad

#### ✅ DoorX MANEJA:
- Work Orders y su ciclo de vida
- Comunicación tenant-vendor vía IA
- Búsqueda y asignación de vendors
- Categorización de servicios
- Coordinación de horarios

#### ❌ DoorX NO MANEJA:
- Rent (pagos de alquiler)
- Leases (contratos de arrendamiento)
- Security Deposits
- Owner financials
- Accounting
- Tenant screening
- Rent collection
- Late fees
- Evictions
- Insurance claims

### 🔍 Ejemplo de Organización
```
src/Domain/
├── ServiceRequest/          # Bounded Context 1
│   ├── Entities/
│   │   ├── ServiceRequest.cs
│   │   └── VendorBid.cs
│   ├── ValueObjects/
│   │   ├── ServiceCategory.cs
│   │   └── Priority.cs
│   ├── Events/
│   │   ├── ServiceRequestCreatedEvent.cs
│   │   └── VendorAssignedEvent.cs
│   └── Repositories/
│       └── IServiceRequestRepository.cs
│
├── PropertyManagement/      # Bounded Context 2
│   ├── Entities/
│   │   ├── Property.cs
│   │   ├── Tenant.cs
│   │   └── PropertyManager.cs
│   ├── ValueObjects/
│   │   └── Address.cs
│   └── Repositories/
│       ├── IPropertyRepository.cs
│       └── ITenantRepository.cs
│
├── ContractorManagement/    # Bounded Context 3
│   ├── Entities/
│   │   └── Vendor.cs
│   ├── ValueObjects/
│   │   ├── ServiceArea.cs
│   │   └── Rating.cs
│   └── Repositories/
│       └── IVendorRepository.cs
│
├── AIAssistant/            # Bounded Context 4
│   ├── Entities/
│   │   ├── Conversation.cs
│   │   └── Message.cs
│   └── Repositories/
│       └── IConversationRepository.cs
│
└── IntegrationPlatform/    # Bounded Context 5
    ├── Entities/
    │   └── ExternalSystemConfig.cs
    └── Repositories/
        └── IIntegrationRepository.cs
```

### ⚠️ Violaciones Comunes
- Mezclar responsabilidades de diferentes contextos
- Referenciar entidades completas entre contextos
- No usar Domain Events para comunicación entre contextos
- Intentar manejar funcionalidades fuera del alcance (ej: rent collection)
- Crear dependencias directas entre bounded contexts

---

## 📊 Resumen de Reglas Críticas

| # | Regla | Impacto | Validación |
|---|-------|---------|------------|
| 1 | Consistencia de Agregados | Alto | Solo modificar via Aggregate Root |
| 2 | Identidad de Entidades | Alto | Igualdad solo por Id |
| 3 | Inmutabilidad de Value Objects | Medio | Value objects con `record` |
| 4 | Publicación de Domain Events | Alto | Publicar después de persistir |
| 5 | Transiciones de Estado Válidas | Alto | Validar estado antes de cambiar |
| 6 | Límite de Ofertas (5 máx) | Medio | Validar al agregar bid |
| 7 | Asignación de Vendors | Alto | Validar calificación, área, disponibilidad |
| 8 | Auditoría Automática | Medio | Llamar UpdateModifiedDate() |
| 9 | Manejo de Errores (ErrorOr) | Alto | No usar excepciones para flujo |
| 10 | Bounded Context Separation | Alto | Comunicación por eventos e IDs |

---

## 🔗 Referencias

- [ARCHITECTURE.md](ARCHITECTURE.md) - Arquitectura del sistema
- [UBIQUITOUS_LANGUAGE.md](UBIQUITOUS_LANGUAGE.md) - Lenguaje ubicuo del dominio
- [/src/Domain/Common/README.md](../src/Domain/Common/README.md) - Guía de clases base DDD

---

**Última actualización:** 2024-11-24
**Versión:** 1.0.0
