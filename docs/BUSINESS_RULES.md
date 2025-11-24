# DoorX - Reglas de Negocio Críticas

> Documento que define las reglas de negocio críticas del sistema DoorX

## 📋 Índice

### Reglas de Dominio (DDD)
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

### Reglas de Integraciones y Funcionalidades
11. [Regla 11: Sincronización con PMS (Property Management Systems)](#regla-11-sincronización-con-pms-property-management-systems)
12. [Regla 12: Categorización Automática por IA (Aimee)](#regla-12-categorización-automática-por-ia-aimee)
13. [Regla 13: Comunicación Multi-Canal](#regla-13-comunicación-multi-canal)
14. [Regla 14: Priorización de Work Orders](#regla-14-priorización-de-work-orders)
15. [Regla 15: Notificaciones y Alertas](#regla-15-notificaciones-y-alertas)
16. [Regla 16: Manejo de Fallos en Integraciones](#regla-16-manejo-de-fallos-en-integraciones)
17. [Regla 17: Rate Limiting para APIs Externas](#regla-17-rate-limiting-para-apis-externas)
18. [Regla 18: Idempotencia en Sincronizaciones](#regla-18-idempotencia-en-sincronizaciones)
19. [Regla 19: Gestión de Conversaciones con IA](#regla-19-gestión-de-conversaciones-con-ia)
20. [Regla 20: Validación de Datos Externos](#regla-20-validación-de-datos-externos)

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

## Regla 11: Sincronización con PMS (Property Management Systems)

### 📌 Descripción
DoorX se integra con **Property Management Systems externos** (Buildium, Hostify, AppFolio) para obtener datos de propiedades, tenants y vendors, pero **NO maneja funcionalidades financieras**.

### 🎯 Razón de Negocio
- Evitar duplicación de datos maestros
- Mantener una única fuente de verdad para entidades core (en el PMS)
- Sincronizar solo lo necesario para gestión de Work Orders
- No competir con funcionalidades del PMS

### ✅ Reglas Específicas

1. **Sincronización unidireccional de datos maestros (PMS → DoorX)**
   ```csharp
   // DoorX OBTIENE de PMS (READ):
   - Properties (dirección, tipo, amenidades)
   - Tenants (nombre, contacto, property asignada)
   - Vendors (nombre, contacto, categorías de servicio)

   // DoorX ENVÍA a PMS (WRITE):
   - Work Orders creados y su estado
   - Notas de servicio completado
   - Costos de servicio (para tracking, no facturación)
   ```

2. **DoorX NO maneja funcionalidades financieras**
   ```csharp
   // ❌ DoorX NO MANEJA:
   - Rent collection (cobro de renta)
   - Lease management (contratos)
   - Security deposits
   - Late fees
   - Tenant screening
   - Owner financials
   - Accounting
   - Invoicing/Billing
   ```

3. **Mantener ExternalId para trazabilidad**
   ```csharp
   public class Property : AggregateRoot<PropertyId>
   {
       public string? ExternalSystemId { get; private set; } // "Buildium-PROP-123"
       public ExternalSystemType? ExternalSystem { get; private set; } // Buildium, Hostify, AppFolio

       public void SetExternalReference(string externalId, ExternalSystemType system)
       {
           ExternalSystemId = externalId;
           ExternalSystem = system;
           UpdateModifiedDate();
       }
   }
   ```

4. **Sincronización programada (no en tiempo real)**
   ```csharp
   // Frecuencia de sincronización
   - Properties: Cada 24 horas (datos raramente cambian)
   - Tenants: Cada 12 horas
   - Vendors: Cada 24 horas
   - Work Order status: Cada 1 hora (o vía webhook si disponible)
   ```

### 🔍 Ejemplo Práctico

```csharp
// Domain - ExternalSystemConfig
public class ExternalSystemConfig : AggregateRoot<ExternalSystemConfigId>
{
    public PropertyManagerId PropertyManagerId { get; private set; }
    public ExternalSystemType SystemType { get; private set; } // Buildium, Hostify, AppFolio
    public string ApiKey { get; private set; }
    public string BaseUrl { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public SyncStatus Status { get; private set; }

    public ErrorOr<Success> UpdateLastSync(DateTime syncTime, SyncStatus status)
    {
        LastSyncAt = syncTime;
        Status = status;
        UpdateModifiedDate();

        AddDomainEvent(new ExternalSystemSyncCompletedEvent(
            Id,
            SystemType,
            status,
            syncTime));

        return Result.Success;
    }
}

// Application - Sync Handler
public class SyncPropertiesFromPmsCommandHandler
{
    private readonly IPmsProviderFactory _providerFactory;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IExternalSystemConfigRepository _configRepository;

    public async Task<ErrorOr<SyncResult>> Handle(SyncPropertiesCommand command)
    {
        // 1. Obtener configuración del PMS
        var config = await _configRepository.GetByPropertyManagerAsync(command.PropertyManagerId);

        if (config is null)
            return DomainErrors.Integration.ProviderNotConfigured;

        // 2. Obtener provider apropiado (Buildium, Hostify, etc.)
        var provider = _providerFactory.Create(config.SystemType);

        try
        {
            // 3. Obtener properties del PMS
            var externalProperties = await provider.GetPropertiesAsync(config);

            // 4. Sincronizar (crear o actualizar)
            var syncedCount = 0;
            foreach (var extProp in externalProperties)
            {
                var property = await _propertyRepository
                    .GetByExternalIdAsync(extProp.ExternalId);

                if (property is null)
                {
                    // Crear nueva
                    var newProperty = Property.CreateFromExternal(
                        extProp.Address,
                        extProp.Type,
                        extProp.ExternalId,
                        config.SystemType);

                    if (newProperty.IsError)
                        continue;

                    await _propertyRepository.AddAsync(newProperty.Value);
                }
                else
                {
                    // Actualizar existente
                    property.UpdateFromExternal(extProp.Address, extProp.Type);
                }

                syncedCount++;
            }

            // 5. Actualizar última sincronización
            config.UpdateLastSync(DateTime.UtcNow, SyncStatus.Success);
            await _unitOfWork.SaveChangesAsync();

            return new SyncResult(syncedCount, externalProperties.Count);
        }
        catch (Exception ex)
        {
            config.UpdateLastSync(DateTime.UtcNow, SyncStatus.Failed);
            await _unitOfWork.SaveChangesAsync();

            return DomainErrors.Integration.SyncFailed;
        }
    }
}

// Infrastructure - Provider abstraction
public interface IPmsProvider
{
    Task<IEnumerable<ExternalProperty>> GetPropertiesAsync(ExternalSystemConfig config);
    Task<IEnumerable<ExternalTenant>> GetTenantsAsync(ExternalSystemConfig config);
    Task<ErrorOr<Success>> CreateWorkOrderAsync(WorkOrder workOrder, ExternalSystemConfig config);
    Task<ErrorOr<Success>> UpdateWorkOrderStatusAsync(WorkOrderId id, WorkOrderStatus status, ExternalSystemConfig config);
}

// Implementaciones específicas
public class BuildiumProvider : IPmsProvider { }
public class HostifyProvider : IPmsProvider { }
public class AppFolioProvider : IPmsProvider { }
```

### ⚠️ Violaciones Comunes
- Intentar manejar pagos o facturación (debe estar en el PMS)
- Sincronización en tiempo real (usar polling o webhooks programados)
- Modificar datos maestros en DoorX sin sincronizar con PMS
- No manejar fallos de sincronización
- Exponer credenciales del PMS en logs

---

## Regla 12: Categorización Automática por IA (Aimee)

### 📌 Descripción
**Aimee** (asistente IA basado en OpenAI GPT-4) debe **categorizar automáticamente** los Work Orders basándose en la descripción del tenant, y extraer información relevante como prioridad y urgencia.

### 🎯 Razón de Negocio
- Reducir tiempo de respuesta
- Eliminar categorización manual
- Mejorar precisión en asignación de vendors
- Escalabilidad sin aumentar staff

### ✅ Reglas Específicas

1. **Categorización automática cuando el tenant reporta un problema**
   ```csharp
   // Flujo:
   Tenant: "El aire acondicionado no funciona y hace mucho calor"

   // Aimee analiza y determina:
   - ServiceCategory: HVAC
   - Priority: High (por condiciones de temperatura)
   - UrgencyLevel: 24-48 horas
   - RequiredSkills: ["HVAC Technician", "AC Repair"]
   ```

2. **Usar OpenAI Assistants API con funciones estructuradas**
   ```csharp
   // Application - AI Service
   public class AiCategorizationService
   {
       private readonly IOpenAiClient _openAiClient;

       public async Task<ErrorOr<WorkOrderCategorization>> CategorizeAsync(string description)
       {
           var systemPrompt = @"
               Eres Aimee, un asistente experto en mantenimiento de propiedades.
               Analiza la descripción del problema y extrae:
               - Categoría de servicio (Plumbing, Electrical, HVAC, Appliance, etc.)
               - Nivel de prioridad (Emergency, High, Normal, Low)
               - Tiempo de respuesta sugerido
               - Habilidades requeridas del vendor
           ";

           var response = await _openAiClient.GetCompletionAsync(
               systemPrompt,
               description,
               functionCalls: new[]
               {
                   new FunctionDefinition
                   {
                       Name = "categorize_work_order",
                       Parameters = new
                       {
                           category = "string",
                           priority = "string",
                           responseTime = "string",
                           skills = "array"
                       }
                   }
               });

           if (response.IsError)
               return DomainErrors.Integration.ProviderUnavailable;

           return new WorkOrderCategorization
           {
               Category = ParseCategory(response.FunctionArguments.category),
               Priority = ParsePriority(response.FunctionArguments.priority),
               SuggestedResponseTime = response.FunctionArguments.responseTime,
               RequiredSkills = response.FunctionArguments.skills
           };
       }
   }
   ```

3. **Permitir override manual de categorización**
   ```csharp
   public class WorkOrder : AggregateRoot<WorkOrderId>
   {
       public ServiceCategory Category { get; private set; }
       public Priority Priority { get; private set; }
       public bool IsAiCategorized { get; private set; }

       public ErrorOr<Success> OverrideCategory(ServiceCategory newCategory, PropertyManagerId overriddenBy)
       {
           if (Status != WorkOrderStatus.Open && Status != WorkOrderStatus.Categorized)
               return Error.Conflict(
                   code: "WorkOrder.CannotRecategorize",
                   description: "Cannot recategorize work order in current status");

           var oldCategory = Category;
           Category = newCategory;
           IsAiCategorized = false;
           UpdateModifiedDate();

           AddDomainEvent(new WorkOrderRecategorizedEvent(
               Id,
               oldCategory,
               newCategory,
               overriddenBy,
               DateTime.UtcNow));

           return Result.Success;
       }
   }
   ```

4. **Aprendizaje de re-categorizaciones**
   ```csharp
   // Cuando un Property Manager cambia la categoría:
   // - Registrar el cambio
   // - Usar para fine-tuning del modelo (futuro)
   public record WorkOrderRecategorizedEvent(
       WorkOrderId WorkOrderId,
       ServiceCategory OldCategory,
       ServiceCategory NewCategory,
       PropertyManagerId OverriddenBy,
       DateTime OccurredOnUtc) : IDomainEvent
   {
       public Guid EventId { get; } = Guid.NewGuid();
   }
   ```

### 🔍 Ejemplo Práctico

```csharp
// Command Handler
public class CreateWorkOrderCommandHandler
{
    private readonly IAiCategorizationService _aiService;
    private readonly IWorkOrderRepository _repository;

    public async Task<ErrorOr<WorkOrderResponse>> Handle(CreateWorkOrderCommand command)
    {
        // 1. Categorizar con IA
        var categorizationResult = await _aiService.CategorizeAsync(command.Description);

        if (categorizationResult.IsError)
        {
            // Fallback: categoría por defecto si IA falla
            categorizationResult = new WorkOrderCategorization
            {
                Category = ServiceCategory.GeneralMaintenance,
                Priority = Priority.Normal,
                IsAiCategorized = false
            };
        }

        var categorization = categorizationResult.Value;

        // 2. Crear Work Order con categorización
        var workOrder = WorkOrder.Create(
            command.TenantId,
            command.PropertyId,
            command.Description,
            categorization.Category,
            categorization.Priority);

        if (workOrder.IsError)
            return workOrder.Errors;

        // 3. Marcar como categorizado por IA
        if (categorization.IsAiCategorized)
        {
            workOrder.Value.MarkAsAiCategorized(categorization.Confidence);
        }

        // 4. Guardar
        await _repository.AddAsync(workOrder.Value);
        await _unitOfWork.SaveChangesAsync();

        return new WorkOrderResponse(workOrder.Value.Id, workOrder.Value.Status);
    }
}
```

### ⚠️ Violaciones Comunes
- No tener fallback cuando IA falla
- No permitir override manual
- Bloquear creación de Work Order si IA no responde
- No registrar re-categorizaciones para mejora del modelo
- Exponer API keys de OpenAI en código o logs

---

## Regla 13: Comunicación Multi-Canal

### 📌 Descripción
DoorX debe soportar **comunicación a través de múltiples canales** (SMS, WhatsApp, Web Chat) y mantener una **conversación unificada** independientemente del canal usado.

### 🎯 Razón de Negocio
- Flexibilidad para usuarios (tenants prefieren diferentes canales)
- Trazabilidad completa de comunicación
- Integración con Twilio para SMS/WhatsApp

### ✅ Reglas Específicas

1. **Una conversación puede tener mensajes de múltiples canales**
   ```csharp
   public class Conversation : AggregateRoot<ConversationId>
   {
       private readonly List<Message> _messages = new();

       public WorkOrderId WorkOrderId { get; private set; }
       public TenantId TenantId { get; private set; }
       public VendorId? AssignedVendorId { get; private set; }
       public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

       public ErrorOr<Success> AddMessage(
           string content,
           ParticipantType sender,
           Channel channel,
           string? externalMessageId = null)
       {
           if (string.IsNullOrWhiteSpace(content))
               return Error.Validation(
                   code: "Conversation.EmptyMessage",
                   description: "Message content cannot be empty");

           var message = Message.Create(
               content,
               sender,
               channel,
               externalMessageId);

           _messages.Add(message);
           UpdateModifiedDate();

           AddDomainEvent(new MessageReceivedEvent(
               Id,
               WorkOrderId,
               message.Id,
               sender,
               channel,
               DateTime.UtcNow));

           return Result.Success;
       }
   }

   // Message entity
   public class Message : Entity<MessageId>
   {
       public string Content { get; private set; }
       public ParticipantType Sender { get; private set; } // Tenant, Vendor, AI
       public Channel Channel { get; private set; } // SMS, WhatsApp, WebChat
       public DateTime SentAt { get; private set; }
       public string? ExternalMessageId { get; private set; } // Twilio message SID
       public DeliveryStatus Status { get; private set; }

       public static Message Create(
           string content,
           ParticipantType sender,
           Channel channel,
           string? externalMessageId = null)
       {
           return new Message(
               new MessageId(Guid.NewGuid()),
               content,
               sender,
               channel,
               DateTime.UtcNow,
               externalMessageId,
               DeliveryStatus.Sent);
       }
   }
   ```

2. **Enums para canales y participantes**
   ```csharp
   public enum Channel
   {
       SMS = 1,
       WhatsApp = 2,
       WebChat = 3,
       Email = 4 // Futuro
   }

   public enum ParticipantType
   {
       Tenant = 1,
       Vendor = 2,
       AI = 3,
       PropertyManager = 4
   }

   public enum DeliveryStatus
   {
       Sent = 1,
       Delivered = 2,
       Read = 3,
       Failed = 4
   }
   ```

3. **Responder en el mismo canal que el mensaje recibido**
   ```csharp
   // Application - Message Handler
   public class ProcessIncomingMessageCommandHandler
   {
       private readonly IConversationRepository _conversationRepo;
       private readonly IMessagingService _messagingService;
       private readonly IAiAssistantService _aiService;

       public async Task<ErrorOr<Success>> Handle(ProcessIncomingMessageCommand command)
       {
           // 1. Encontrar o crear conversación
           var conversation = await _conversationRepo
               .GetByWorkOrderIdAsync(command.WorkOrderId);

           // 2. Agregar mensaje entrante
           conversation.AddMessage(
               command.Content,
               command.Sender,
               command.Channel,
               command.ExternalMessageId);

           // 3. Generar respuesta con IA
           var aiResponse = await _aiService.GenerateResponseAsync(
               conversation.Messages.ToList(),
               command.Content);

           if (aiResponse.IsError)
               return aiResponse.Errors;

           // 4. Agregar respuesta de IA
           conversation.AddMessage(
               aiResponse.Value,
               ParticipantType.AI,
               command.Channel); // MISMO canal

           await _unitOfWork.SaveChangesAsync();

           // 5. Enviar respuesta por el canal apropiado
           await _messagingService.SendAsync(
               aiResponse.Value,
               command.Channel,
               command.RecipientPhoneOrId);

           return Result.Success;
       }
   }
   ```

4. **Sincronizar estado de entrega con Twilio**
   ```csharp
   // Webhook handler para status callbacks de Twilio
   public class TwilioWebhookHandler
   {
       public async Task<IActionResult> HandleStatusCallback(TwilioStatusCallbackDto callback)
       {
           var conversation = await _conversationRepo
               .GetByExternalMessageIdAsync(callback.MessageSid);

           if (conversation is null)
               return NotFound();

           // Actualizar estado de delivery
           conversation.UpdateMessageDeliveryStatus(
               callback.MessageSid,
               MapTwilioStatus(callback.MessageStatus));

           await _unitOfWork.SaveChangesAsync();

           return Ok();
       }

       private DeliveryStatus MapTwilioStatus(string twilioStatus)
       {
           return twilioStatus switch
           {
               "delivered" => DeliveryStatus.Delivered,
               "read" => DeliveryStatus.Read,
               "failed" or "undelivered" => DeliveryStatus.Failed,
               _ => DeliveryStatus.Sent
           };
       }
   }
   ```

### 🔍 Ejemplo de Flujo Multi-Canal

```
Tenant (via SMS): "El fregadero está goteando"
  ↓
Aimee (via SMS): "Entiendo, un problema de plomería. ¿Está goteando constantemente?"
  ↓
Tenant (via WhatsApp): "Sí, todo el tiempo" [cambió de canal]
  ↓
Aimee (via WhatsApp): "Perfecto. Estoy buscando un plomero disponible..."
  ↓
Vendor asignado
  ↓
Aimee (via WhatsApp → Tenant): "John el plomero puede ir mañana a las 2PM, ¿funciona?"
Aimee (via SMS → Vendor): "Nuevo trabajo: Fregadero goteando en 123 Main St"
  ↓
Tenant (via Web Chat): "Sí, perfecto" [cambió a web]
  ↓
Conversación continúa sin interrupciones
```

### ⚠️ Violaciones Comunes
- Forzar un solo canal de comunicación
- No sincronizar estado de entrega
- Perder contexto cuando el usuario cambia de canal
- No guardar ExternalMessageId para trazabilidad
- No manejar fallos de envío (retry logic)

---

## Regla 14: Priorización de Work Orders

### 📌 Descripción
Los **Work Orders deben ser priorizados automáticamente** según criterios de urgencia, impacto y tipo de problema, con **SLAs (Service Level Agreements) específicos** por prioridad.

### 🎯 Razón de Negocio
- Garantizar seguridad de los tenants
- Cumplir con regulaciones de habitabilidad
- Optimizar asignación de recursos
- Medir tiempos de respuesta

### ✅ Reglas Específicas

1. **Niveles de prioridad con SLAs definidos**
   ```csharp
   public record Priority
   {
       public static readonly Priority Emergency = new(
           "Emergency",
           1,
           TimeSpan.FromHours(4),
           "Problemas de seguridad, sin agua/electricidad/calefacción");

       public static readonly Priority High = new(
           "High",
           2,
           TimeSpan.FromHours(24),
           "Problemas importantes que afectan habitabilidad");

       public static readonly Priority Normal = new(
           "Normal",
           3,
           TimeSpan.FromDays(3),
           "Reparaciones estándar");

       public static readonly Priority Low = new(
           "Low",
           4,
           TimeSpan.FromDays(7),
           "Mejoras cosméticas o no urgentes");

       public string Name { get; }
       public int Level { get; }
       public TimeSpan Sla { get; }
       public string Description { get; }

       private Priority(string name, int level, TimeSpan sla, string description)
       {
           Name = name;
           Level = level;
           Sla = sla;
           Description = description;
       }
   }
   ```

2. **Criterios de auto-priorización**
   ```csharp
   // Reglas de negocio para priorización automática
   public class WorkOrderPrioritizationService
   {
       public Priority DeterminePriority(ServiceCategory category, string description)
       {
           var lowerDesc = description.ToLower();

           // Emergency - Keywords de seguridad
           if (IsEmergency(category, lowerDesc))
               return Priority.Emergency;

           // High - Servicios esenciales
           if (IsHighPriority(category, lowerDesc))
               return Priority.High;

           // Low - Cosmético
           if (IsLowPriority(lowerDesc))
               return Priority.Low;

           // Default: Normal
           return Priority.Normal;
       }

       private bool IsEmergency(ServiceCategory category, string description)
       {
           var emergencyKeywords = new[]
           {
               "sin agua", "no water", "sin electricidad", "no power",
               "inundación", "flooding", "flood", "gas leak", "fuga de gas",
               "no heat", "sin calefacción", "humo", "smoke",
               "seguridad", "safety", "peligro", "danger"
           };

           return emergencyKeywords.Any(keyword => description.Contains(keyword)) ||
                  (category == ServiceCategory.Electrical && description.Contains("sparks")) ||
                  (category == ServiceCategory.Plumbing && description.Contains("burst"));
       }

       private bool IsHighPriority(ServiceCategory category, string description)
       {
           var highPriorityCategories = new[]
           {
               ServiceCategory.HVAC,      // AC/Heat
               ServiceCategory.Plumbing,  // Leaks
               ServiceCategory.Electrical // Power issues
           };

           return highPriorityCategories.Contains(category) &&
                  !IsEmergency(category, description);
       }

       private bool IsLowPriority(string description)
       {
           var lowPriorityKeywords = new[]
           {
               "paint", "pintura", "cosmetic", "cosmético",
               "scratched", "rayado", "dent", "abolladura"
           };

           return lowPriorityKeywords.Any(keyword => description.Contains(keyword));
       }
   }
   ```

3. **SLA tracking y alertas**
   ```csharp
   public class WorkOrder : AggregateRoot<WorkOrderId>
   {
       public Priority Priority { get; private set; }
       public DateTime CreatedOnUtc { get; private set; }
       public DateTime? ResolvedAt { get; private set; }
       public DateTime SlaDeadline { get; private set; }

       public bool IsSlaBreached => DateTime.UtcNow > SlaDeadline && ResolvedAt is null;

       public TimeSpan TimeUntilSlaBreach => SlaDeadline - DateTime.UtcNow;

       public static ErrorOr<WorkOrder> Create(
           TenantId tenantId,
           PropertyId propertyId,
           string description,
           ServiceCategory category,
           Priority priority)
       {
           var workOrder = new WorkOrder(
               new WorkOrderId(Guid.NewGuid()),
               tenantId,
               propertyId,
               description,
               category,
               priority);

           // Calcular SLA deadline
           workOrder.SlaDeadline = DateTime.UtcNow.Add(priority.Sla);

           workOrder.AddDomainEvent(new WorkOrderCreatedEvent(
               workOrder.Id,
               priority,
               workOrder.SlaDeadline,
               DateTime.UtcNow));

           return workOrder;
       }

       public ErrorOr<Success> EscalatePriority(PropertyManagerId escalatedBy, string reason)
       {
           if (Priority == Priority.Emergency)
               return Error.Conflict(
                   code: "WorkOrder.AlreadyMaxPriority",
                   description: "Work order is already at maximum priority");

           var oldPriority = Priority;
           Priority = GetNextHigherPriority(Priority);

           // Recalcular SLA
           SlaDeadline = DateTime.UtcNow.Add(Priority.Sla);
           UpdateModifiedDate();

           AddDomainEvent(new WorkOrderPriorityEscalatedEvent(
               Id,
               oldPriority,
               Priority,
               escalatedBy,
               reason,
               SlaDeadline,
               DateTime.UtcNow));

           return Result.Success;
       }
   }
   ```

4. **Background job para monitoreo de SLA**
   ```csharp
   // Background service que corre cada hora
   public class SlaMonitoringService : BackgroundService
   {
       private readonly IWorkOrderRepository _repository;
       private readonly INotificationService _notificationService;

       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               await CheckSlaBreachesAsync();
               await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
           }
       }

       private async Task CheckSlaBreachesAsync()
       {
           // Encontrar Work Orders cerca de breach (2 horas antes)
           var nearBreachWorkOrders = await _repository
               .GetNearingSlaBreachAsync(TimeSpan.FromHours(2));

           foreach (var wo in nearBreachWorkOrders)
           {
               // Notificar a Property Manager
               await _notificationService.SendSlaWarningAsync(
                   wo.PropertyId,
                   wo.Id,
                   wo.TimeUntilSlaBreach);
           }

           // Encontrar Work Orders con SLA breached
           var breachedWorkOrders = await _repository
               .GetSlaBreachedAsync();

           foreach (var wo in breachedWorkOrders)
           {
               // Auto-escalar prioridad
               wo.EscalatePriority(
                   PropertyManagerId.System,
                   "Auto-escalated due to SLA breach");

               // Notificar urgente
               await _notificationService.SendSlaBreachAlertAsync(wo);
           }

           await _unitOfWork.SaveChangesAsync();
       }
   }
   ```

### 🔍 Ejemplo de Priorización

```csharp
// Escenario 1: Emergency
Tenant: "Sin agua en toda la casa"
→ Category: Plumbing
→ Priority: Emergency (auto-detected por keyword "sin agua")
→ SLA: 4 horas
→ Vendor asignado inmediatamente

// Escenario 2: High
Tenant: "El aire acondicionado no funciona y hace 95°F"
→ Category: HVAC
→ Priority: High (HVAC + temperatura)
→ SLA: 24 horas
→ Vendor contactado en las próximas horas

// Escenario 3: Normal
Tenant: "La puerta del closet no cierra bien"
→ Category: GeneralMaintenance
→ Priority: Normal
→ SLA: 3 días
→ Programado según disponibilidad

// Escenario 4: Low
Tenant: "Hay un rayón en la pared de la sala"
→ Category: GeneralMaintenance
→ Priority: Low (keyword "rayón")
→ SLA: 7 días
→ Se agenda cuando haya disponibilidad
```

### ⚠️ Violaciones Comunes
- No definir SLAs claros
- Priorizar todos los Work Orders igual
- No monitorear breaches de SLA
- No auto-escalar Work Orders vencidos
- No notificar a Property Managers de SLAs en riesgo

---

## Regla 15: Notificaciones y Alertas

### 📌 Descripción
El sistema debe enviar **notificaciones oportunas y relevantes** a los participantes correctos según eventos del Work Order, usando el canal preferido de cada usuario.

### 🎯 Razón de Negocio
- Mantener a todos informados
- Reducir tiempo de respuesta
- Mejorar satisfacción del tenant
- Cumplir SLAs de comunicación

### ✅ Reglas Específicas

1. **Tipos de notificaciones por stakeholder**
   ```csharp
   // Tenant recibe notificaciones de:
   - Work Order creado (confirmación)
   - Vendor asignado (nombre, hora estimada)
   - Vendor en camino
   - Trabajo completado (solicitud de confirmación)
   - Cambios en schedule

   // Vendor recibe notificaciones de:
   - Nuevo Work Order asignado
   - Cambios en prioridad
   - Mensajes del tenant
   - Cancelaciones

   // Property Manager recibe notificaciones de:
   - Work Orders de alta prioridad creados
   - SLA warnings (2 horas antes de breach)
   - SLA breaches
   - Trabajos completados (resumen diario)
   - Costos que exceden aprobación automática
   ```

2. **Preferencias de canal por usuario**
   ```csharp
   public class User : AggregateRoot<UserId>
   {
       private readonly Dictionary<NotificationType, Channel> _notificationPreferences = new();

       public string Email { get; private set; }
       public string PhoneNumber { get; private set; }
       public Channel PreferredChannel { get; private set; }

       public void SetNotificationPreference(NotificationType type, Channel channel)
       {
           if (!IsValidChannelForUser(channel))
               throw new InvalidOperationException($"Channel {channel} not available for user");

           _notificationPreferences[type] = channel;
           UpdateModifiedDate();
       }

       public Channel GetPreferredChannel(NotificationType notificationType)
       {
           return _notificationPreferences.TryGetValue(notificationType, out var channel)
               ? channel
               : PreferredChannel; // Default
       }

       private bool IsValidChannelForUser(Channel channel)
       {
           return channel switch
           {
               Channel.Email => !string.IsNullOrEmpty(Email),
               Channel.SMS or Channel.WhatsApp => !string.IsNullOrEmpty(PhoneNumber),
               Channel.WebChat => true,
               _ => false
           };
       }
   }
   ```

3. **Event-driven notifications**
   ```csharp
   // Domain Event Handler
   public class WorkOrderCreatedEventHandler : IDomainEventHandler<WorkOrderCreatedEvent>
   {
       private readonly INotificationService _notificationService;
       private readonly ITenantRepository _tenantRepository;
       private readonly IPropertyManagerRepository _pmRepository;

       public async Task Handle(WorkOrderCreatedEvent @event)
       {
           // 1. Notificar al tenant (confirmación)
           var tenant = await _tenantRepository.GetByIdAsync(@event.TenantId);
           await _notificationService.SendAsync(new Notification
           {
               RecipientId = tenant.Id,
               Type = NotificationType.WorkOrderCreated,
               Channel = tenant.PreferredChannel,
               Title = "Solicitud recibida",
               Message = $"Hemos recibido tu solicitud. Número: {@event.WorkOrderId}",
               Data = new { WorkOrderId = @event.WorkOrderId }
           });

           // 2. Si es alta prioridad, notificar a Property Manager
           if (@event.Priority.Level <= Priority.High.Level)
           {
               var pm = await _pmRepository.GetByPropertyAsync(@event.PropertyId);
               await _notificationService.SendAsync(new Notification
               {
                   RecipientId = pm.Id,
                   Type = NotificationType.HighPriorityWorkOrder,
                   Channel = Channel.Email, // PMs prefieren email para urgencias
                   Title = $"Work Order de {Priority.Name} Prioridad",
                   Message = $"Nuevo Work Order {@event.WorkOrderId} requiere atención",
                   Data = new { WorkOrderId = @event.WorkOrderId, Priority = @event.Priority }
               });
           }
       }
   }

   // Vendor Assigned Event Handler
   public class VendorAssignedEventHandler : IDomainEventHandler<VendorAssignedEvent>
   {
       private readonly INotificationService _notificationService;
       private readonly IVendorRepository _vendorRepository;
       private readonly ITenantRepository _tenantRepository;
       private readonly IWorkOrderRepository _workOrderRepository;

       public async Task Handle(VendorAssignedEvent @event)
       {
           var vendor = await _vendorRepository.GetByIdAsync(@event.VendorId);
           var workOrder = await _workOrderRepository.GetByIdAsync(@event.WorkOrderId);
           var tenant = await _tenantRepository.GetByIdAsync(workOrder.TenantId);

           // 1. Notificar al vendor
           await _notificationService.SendAsync(new Notification
           {
               RecipientId = vendor.Id,
               Type = NotificationType.WorkOrderAssigned,
               Channel = vendor.PreferredChannel,
               Title = "Nuevo trabajo asignado",
               Message = $"Trabajo: {workOrder.Description}. Dirección: {workOrder.Property.Address}",
               Data = new { WorkOrderId = @event.WorkOrderId }
           });

           // 2. Notificar al tenant
           await _notificationService.SendAsync(new Notification
           {
               RecipientId = tenant.Id,
               Type = NotificationType.VendorAssigned,
               Channel = tenant.PreferredChannel,
               Title = "Técnico asignado",
               Message = $"{vendor.Name} está programado para {workOrder.ScheduledFor:MMM dd, h:mm tt}",
               Data = new { VendorName = vendor.Name, ScheduledFor = workOrder.ScheduledFor }
           });
       }
   }
   ```

4. **Notificaciones agrupadas (digest)**
   ```csharp
   // Para evitar spam, agrupar notificaciones de baja prioridad
   public class DailyDigestService
   {
       public async Task SendDailyDigestAsync(PropertyManagerId pmId)
       {
           var yesterday = DateTime.UtcNow.AddDays(-1);
           var events = await GetDailyEventsAsync(pmId, yesterday);

           var digest = new DigestNotification
           {
               Date = yesterday,
               Summary = new
               {
                   TotalWorkOrders = events.WorkOrdersCreated.Count,
                   Completed = events.WorkOrdersCompleted.Count,
                   InProgress = events.WorkOrdersInProgress.Count,
                   NearingSla = events.WorkOrdersNearingSla.Count,
                   TotalCost = events.TotalCost
               },
               Details = events
           };

           await _notificationService.SendDigestAsync(pmId, digest);
       }
   }
   ```

### 🔍 Ejemplo de Flujo de Notificaciones

```
1. Tenant crea Work Order
   → Tenant recibe (SMS): "✓ Solicitud #1234 recibida"
   → PM recibe (Email): "[HIGH] Nuevo Work Order #1234 - Sin AC"

2. Aimee categoriza y busca vendors
   → Tenant recibe (SMS): "Buscando técnico de HVAC disponible..."

3. Vendor asignado
   → Vendor recibe (SMS): "Nuevo trabajo: AC repair en 123 Main St, mañana 2PM"
   → Tenant recibe (WhatsApp): "John el técnico irá mañana a las 2PM"

4. Vendor en camino
   → Tenant recibe (WhatsApp): "John está en camino, llegará en 15 minutos"

5. Trabajo completado
   → Tenant recibe (WhatsApp): "¿El aire acondicionado ya funciona correctamente?"
   → PM recibe (Digest diario): "5 Work Orders completados hoy, costo total: $850"

6. SLA Warning (2 horas antes)
   → PM recibe (Email + SMS): "⚠️ Work Order #5678 vence en 2 horas"

7. SLA Breach
   → PM recibe (Email + SMS + Push): "🚨 ALERTA: Work Order #5678 ha excedido SLA"
```

### ⚠️ Violaciones Comunes
- Enviar demasiadas notificaciones (spam)
- No respetar preferencias de canal
- No agrupar notificaciones de baja prioridad
- No incluir información relevante (link, dirección, hora)
- No tener opt-out para ciertos tipos de notificaciones

---

## Regla 16: Manejo de Fallos en Integraciones

### 📌 Descripción
Las integraciones con sistemas externos (PMS, OpenAI, Twilio) **pueden fallar**. El sistema debe implementar **estrategias de resiliencia** como retry, circuit breaker, y fallbacks.

### 🎯 Razón de Negocio
- Garantizar disponibilidad del sistema
- No bloquear operaciones críticas
- Recuperación automática de fallos transitorios
- Experiencia de usuario consistente

### ✅ Reglas Específicas

1. **Retry con exponential backoff para fallos transitorios**
   ```csharp
   // Usar Polly para retry policies
   public class TwilioMessagingService : IMessagingService
   {
       private readonly ITwilioClient _client;
       private readonly IAsyncPolicy<ErrorOr<Success>> _retryPolicy;

       public TwilioMessagingService(ITwilioClient client)
       {
           _client = client;

           // Retry policy: 3 intentos con exponential backoff
           _retryPolicy = Policy<ErrorOr<Success>>
               .Handle<TwilioException>(ex => IsTransient(ex))
               .WaitAndRetryAsync(
                   retryCount: 3,
                   sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                   onRetry: (outcome, timespan, retryAttempt, context) =>
                   {
                       _logger.LogWarning(
                           "Twilio retry attempt {RetryAttempt} after {Delay}s",
                           retryAttempt,
                           timespan.TotalSeconds);
                   });
       }

       public async Task<ErrorOr<Success>> SendSmsAsync(string to, string message)
       {
           return await _retryPolicy.ExecuteAsync(async () =>
           {
               try
               {
                   var result = await _client.Messages.CreateAsync(
                       to: to,
                       from: _twilioConfig.PhoneNumber,
                       body: message);

                   return Result.Success;
               }
               catch (TwilioException ex) when (IsTransient(ex))
               {
                   return Error.Failure(
                       code: "Twilio.TransientError",
                       description: ex.Message);
               }
               catch (TwilioException ex)
               {
                   return Error.Failure(
                       code: "Twilio.PermanentError",
                       description: ex.Message);
               }
           });
       }

       private bool IsTransient(TwilioException ex)
       {
           // Errores transitorios: timeout, rate limit, server error
           var transientStatusCodes = new[] { 429, 500, 502, 503, 504 };
           return ex.StatusCode.HasValue &&
                  transientStatusCodes.Contains((int)ex.StatusCode.Value);
       }
   }
   ```

2. **Circuit Breaker para evitar cascading failures**
   ```csharp
   public class OpenAiService : IAiCategorizationService
   {
       private readonly IOpenAiClient _client;
       private readonly IAsyncPolicy<ErrorOr<WorkOrderCategorization>> _circuitBreakerPolicy;

       public OpenAiService(IOpenAiClient client)
       {
           _client = client;

           // Circuit Breaker: Abre después de 5 fallos consecutivos
           // Permanece abierto por 30 segundos
           // Luego prueba 1 request (half-open)
           _circuitBreakerPolicy = Policy<ErrorOr<WorkOrderCategorization>>
               .Handle<HttpRequestException>()
               .OrResult(result => result.IsError)
               .CircuitBreakerAsync(
                   handledEventsAllowedBeforeBreaking: 5,
                   durationOfBreak: TimeSpan.FromSeconds(30),
                   onBreak: (outcome, duration) =>
                   {
                       _logger.LogError(
                           "OpenAI circuit breaker opened. Will retry after {Duration}s",
                           duration.TotalSeconds);
                   },
                   onReset: () =>
                   {
                       _logger.LogInformation("OpenAI circuit breaker reset");
                   },
                   onHalfOpen: () =>
                   {
                       _logger.LogInformation("OpenAI circuit breaker half-open, testing...");
                   });
       }

       public async Task<ErrorOr<WorkOrderCategorization>> CategorizeAsync(string description)
       {
           try
           {
               return await _circuitBreakerPolicy.ExecuteAsync(async () =>
               {
                   var response = await _client.GetCategorizationAsync(description);

                   if (response.IsError)
                       return response.Errors;

                   return response.Value;
               });
           }
           catch (BrokenCircuitException)
           {
               // Circuit breaker está abierto, usar fallback
               _logger.LogWarning("OpenAI circuit breaker is open, using fallback categorization");

               return GetFallbackCategorization(description);
           }
       }

       private ErrorOr<WorkOrderCategorization> GetFallbackCategorization(string description)
       {
           // Fallback: categorización basada en keywords simples
           return new WorkOrderCategorization
           {
               Category = ServiceCategory.GeneralMaintenance,
               Priority = Priority.Normal,
               IsAiCategorized = false,
               Confidence = 0.5
           };
       }
   }
   ```

3. **Fallback strategies**
   ```csharp
   // 1. Fallback para sincronización de PMS
   public class PmsSyncService
   {
       public async Task<ErrorOr<SyncResult>> SyncPropertiesAsync()
       {
           try
           {
               // Intentar sincronizar con PMS
               return await _pmsProvider.SyncPropertiesAsync();
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "PMS sync failed, using cached data");

               // Fallback: usar datos en cache (último sync exitoso)
               var cachedProperties = await _cache.GetPropertiesAsync();

               return new SyncResult
               {
                   Success = false,
                   Message = "Using cached data due to PMS unavailability",
                   Source = SyncSource.Cache,
                   Properties = cachedProperties
               };
           }
       }
   }

   // 2. Fallback para AI categorization
   public class WorkOrderService
   {
       public async Task<ErrorOr<WorkOrder>> CreateAsync(CreateWorkOrderCommand command)
       {
           // Intentar categorizar con IA
           var aiResult = await _aiService.CategorizeAsync(command.Description);

           WorkOrderCategorization categorization;

           if (aiResult.IsError)
           {
               // Fallback: categorización manual requerida
               categorization = new WorkOrderCategorization
               {
                   Category = ServiceCategory.Uncategorized,
                   Priority = Priority.Normal,
                   RequiresManualReview = true
               };

               // Notificar a PM para categorización manual
               await _notificationService.SendManualCategorizationRequestAsync(
                   command.PropertyId,
                   command.Description);
           }
           else
           {
               categorization = aiResult.Value;
           }

           // Crear Work Order (no bloquear por fallo de IA)
           return await WorkOrder.Create(
               command.TenantId,
               command.PropertyId,
               command.Description,
               categorization.Category,
               categorization.Priority);
       }
   }

   // 3. Fallback para messaging
   public class NotificationService
   {
       public async Task SendAsync(Notification notification)
       {
           // Intentar canal preferido
           var result = await SendViaChannelAsync(notification, notification.Channel);

           if (result.IsError)
           {
               // Fallback: intentar canal alternativo
               _logger.LogWarning(
                   "Failed to send via {Channel}, trying fallback",
                   notification.Channel);

               var fallbackChannel = GetFallbackChannel(notification.Channel);
               result = await SendViaChannelAsync(notification, fallbackChannel);

               if (result.IsError)
               {
                   // Último fallback: guardar en outbox para retry posterior
                   await _outbox.SaveForRetryAsync(notification);
               }
           }
       }

       private Channel GetFallbackChannel(Channel primary)
       {
           return primary switch
           {
               Channel.WhatsApp => Channel.SMS,
               Channel.SMS => Channel.Email,
               _ => Channel.Email
           };
       }
   }
   ```

4. **Dead Letter Queue para mensajes fallidos**
   ```csharp
   public class MessageOutbox : AggregateRoot<MessageOutboxId>
   {
       public Guid NotificationId { get; private set; }
       public string RecipientId { get; private set; }
       public Channel Channel { get; private set; }
       public string Content { get; private set; }
       public int RetryCount { get; private set; }
       public DateTime? LastRetryAt { get; private set; }
       public DateTime? ScheduledRetryAt { get; private set; }
       public OutboxStatus Status { get; private set; }

       public ErrorOr<Success> IncrementRetry()
       {
           if (RetryCount >= MaxRetries)
           {
               Status = OutboxStatus.Failed;
               AddDomainEvent(new MessagePermanentlyFailedEvent(Id, NotificationId));
               return Error.Failure(
                   code: "Outbox.MaxRetriesReached",
                   description: "Message failed after maximum retries");
           }

           RetryCount++;
           LastRetryAt = DateTime.UtcNow;

           // Exponential backoff
           ScheduledRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, RetryCount));

           UpdateModifiedDate();
           return Result.Success;
       }

       private const int MaxRetries = 5;
   }

   // Background service para procesar outbox
   public class OutboxProcessorService : BackgroundService
   {
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               await ProcessPendingMessagesAsync();
               await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
           }
       }

       private async Task ProcessPendingMessagesAsync()
       {
           var pendingMessages = await _outboxRepository
               .GetPendingMessagesAsync(DateTime.UtcNow);

           foreach (var msg in pendingMessages)
           {
               var result = await _messagingService.SendAsync(msg);

               if (result.IsError)
               {
                   msg.IncrementRetry();
               }
               else
               {
                   msg.MarkAsProcessed();
               }
           }

           await _unitOfWork.SaveChangesAsync();
       }
   }
   ```

### ⚠️ Violaciones Comunes
- No implementar retry para fallos transitorios
- Retry infinito sin exponential backoff
- No usar circuit breaker (causar cascading failures)
- Bloquear operaciones críticas por fallos de integraciones
- No tener fallbacks
- No logear fallos de integración

---

## Regla 17: Rate Limiting para APIs Externas

### 📌 Descripción
Las APIs externas tienen **límites de tasa (rate limits)**. El sistema debe respetar estos límites para evitar bloqueos y sobrecostos.

### 🎯 Razón de Negocio
- Evitar bloqueos de cuenta (429 Too Many Requests)
- Controlar costos de APIs (OpenAI cobra por request)
- Cumplir términos de servicio
- Distribuir carga uniformemente

### ✅ Reglas Específicas

1. **Rate limits por proveedor**
   ```csharp
   // OpenAI
   - GPT-4: 10,000 requests/day (tier standard)
   - Rate: 500 requests/minute

   // Twilio
   - SMS: 1 mensaje/segundo por número
   - WhatsApp: 1,000 mensajes/segundo (total)

   // Buildium API
   - 1,000 requests/hour
   - 20 requests/second

   // Hostify API
   - 5,000 requests/day
   - 100 requests/minute
   ```

2. **Token bucket algorithm para rate limiting**
   ```csharp
   public class RateLimiter
   {
       private readonly SemaphoreSlim _semaphore;
       private readonly int _maxTokens;
       private readonly TimeSpan _refillInterval;
       private int _tokens;
       private DateTime _lastRefill;

       public RateLimiter(int maxTokens, TimeSpan refillInterval)
       {
           _maxTokens = maxTokens;
           _tokens = maxTokens;
           _refillInterval = refillInterval;
           _lastRefill = DateTime.UtcNow;
           _semaphore = new SemaphoreSlim(1, 1);
       }

       public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default)
       {
           await _semaphore.WaitAsync(cancellationToken);
           try
           {
               RefillTokens();

               if (_tokens > 0)
               {
                   _tokens--;
                   return true;
               }

               return false;
           }
           finally
           {
               _semaphore.Release();
           }
       }

       public async Task WaitForTokenAsync(CancellationToken cancellationToken = default)
       {
           while (!await TryAcquireAsync(cancellationToken))
           {
               var timeUntilRefill = _lastRefill.Add(_refillInterval) - DateTime.UtcNow;
               if (timeUntilRefill > TimeSpan.Zero)
               {
                   await Task.Delay(timeUntilRefill, cancellationToken);
               }
           }
       }

       private void RefillTokens()
       {
           var now = DateTime.UtcNow;
           var elapsed = now - _lastRefill;

           if (elapsed >= _refillInterval)
           {
               var intervalsElapsed = (int)(elapsed / _refillInterval);
               _tokens = Math.Min(_maxTokens, _tokens + intervalsElapsed);
               _lastRefill = _lastRefill.Add(_refillInterval * intervalsElapsed);
           }
       }
   }

   // Uso con OpenAI
   public class OpenAiService
   {
       private readonly RateLimiter _rateLimiter;

       public OpenAiService()
       {
           // 500 requests por minuto
           _rateLimiter = new RateLimiter(
               maxTokens: 500,
               refillInterval: TimeSpan.FromMinutes(1));
       }

       public async Task<ErrorOr<string>> GetCompletionAsync(string prompt)
       {
           // Esperar por token disponible
           await _rateLimiter.WaitForTokenAsync();

           try
           {
               return await _client.GetCompletionAsync(prompt);
           }
           catch (RateLimitException ex)
           {
               // OpenAI nos rate limited, backoff
               _logger.LogWarning("OpenAI rate limit hit, backing off");
               await Task.Delay(TimeSpan.FromSeconds(60));

               return Error.Failure(
                   code: "OpenAI.RateLimitExceeded",
                   description: "Rate limit exceeded, please try again later");
           }
       }
   }
   ```

3. **Cost tracking para OpenAI**
   ```csharp
   public class OpenAiCostTracker
   {
       private readonly IDistributedCache _cache;

       // GPT-4 pricing (ejemplo)
       private const decimal CostPer1kInputTokens = 0.03m;
       private const decimal CostPer1kOutputTokens = 0.06m;

       // Límite diario
       private const decimal DailyBudgetLimit = 100.00m;

       public async Task<ErrorOr<Success>> TrackUsageAsync(
           int inputTokens,
           int outputTokens)
       {
           var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
           var cacheKey = $"openai:cost:{today}";

           var currentCost = await GetTodaysCostAsync(cacheKey);

           var requestCost = CalculateCost(inputTokens, outputTokens);
           var newTotal = currentCost + requestCost;

           if (newTotal > DailyBudgetLimit)
           {
               _logger.LogError(
                   "OpenAI daily budget limit reached: ${NewTotal} > ${Limit}",
                   newTotal,
                   DailyBudgetLimit);

               return Error.Failure(
                   code: "OpenAI.BudgetLimitExceeded",
                   description: $"Daily budget limit of ${DailyBudgetLimit} exceeded");
           }

           await _cache.SetStringAsync(
               cacheKey,
               newTotal.ToString(),
               new DistributedCacheEntryOptions
               {
                   AbsoluteExpiration = DateTime.UtcNow.Date.AddDays(1)
               });

           _logger.LogInformation(
               "OpenAI usage: {InputTokens} input, {OutputTokens} output. Cost: ${Cost}. Daily total: ${Total}",
               inputTokens,
               outputTokens,
               requestCost,
               newTotal);

           return Result.Success;
       }

       private decimal CalculateCost(int inputTokens, int outputTokens)
       {
           var inputCost = (inputTokens / 1000m) * CostPer1kInputTokens;
           var outputCost = (outputTokens / 1000m) * CostPer1kOutputTokens;
           return inputCost + outputCost;
       }
   }
   ```

4. **Request batching para reducir llamadas**
   ```csharp
   public class TwilioBatchSender
   {
       private readonly List<SmsMessage> _batch = new();
       private readonly SemaphoreSlim _batchLock = new(1, 1);
       private const int BatchSize = 100;
       private const int BatchIntervalSeconds = 5;

       public async Task QueueMessageAsync(SmsMessage message)
       {
           await _batchLock.WaitAsync();
           try
           {
               _batch.Add(message);

               // Enviar batch si alcanzamos el tamaño
               if (_batch.Count >= BatchSize)
               {
                   await FlushBatchAsync();
               }
           }
           finally
           {
               _batchLock.Release();
           }
       }

       // Background timer para flush periódico
       private async Task FlushBatchAsync()
       {
           if (_batch.Count == 0)
               return;

           var messagesToSend = _batch.ToList();
           _batch.Clear();

           // Twilio permite enviar mensajes en lotes
           foreach (var msg in messagesToSend)
           {
               await _twilioClient.SendAsync(msg);

               // Rate limit: 1 mensaje/segundo
               await Task.Delay(TimeSpan.FromSeconds(1));
           }
       }
   }
   ```

### ⚠️ Violaciones Comunes
- No implementar rate limiting
- No respetar límites del proveedor
- No trackear costos de APIs metered (OpenAI)
- Hacer requests individuales en vez de batching cuando es posible
- No cachear respuestas que pueden ser reutilizadas

---

## Regla 18: Idempotencia en Sincronizaciones

### 📌 Descripción
Las operaciones de sincronización con sistemas externos deben ser **idempotentes**: ejecutarlas múltiples veces produce el mismo resultado que ejecutarlas una vez.

### 🎯 Razón de Negocio
- Evitar duplicación de datos
- Permitir reintentos seguros
- Soportar exactly-once semantics
- Recuperación de fallos sin efectos secundarios

### ✅ Reglas Específicas

1. **Usar claves de idempotencia para operaciones externas**
   ```csharp
   public class WorkOrderSyncService
   {
       public async Task<ErrorOr<Success>> SyncToPmsAsync(
           WorkOrderId workOrderId,
           ExternalSystemConfig config)
       {
           var workOrder = await _repository.GetByIdAsync(workOrderId);

           // Generar clave de idempotencia única y determinista
           var idempotencyKey = GenerateIdempotencyKey(workOrderId, config.SystemType);

           try
           {
               // Si ya existe en PMS, actualizar. Si no, crear.
               if (workOrder.ExternalWorkOrderId is not null)
               {
                   // UPDATE (idempotent)
                   await _pmsProvider.UpdateWorkOrderAsync(
                       workOrder.ExternalWorkOrderId,
                       workOrder,
                       idempotencyKey);
               }
               else
               {
                   // CREATE (idempotent con key)
                   var externalId = await _pmsProvider.CreateWorkOrderAsync(
                       workOrder,
                       idempotencyKey);

                   // Guardar external ID
                   workOrder.SetExternalWorkOrderId(externalId, config.SystemType);
                   await _unitOfWork.SaveChangesAsync();
               }

               return Result.Success;
           }
           catch (IdempotencyKeyAlreadyUsedException)
           {
               // Ya fue procesado previamente, esto es OK
               _logger.LogInformation(
                   "Work Order {WorkOrderId} already synced to PMS (idempotency key matched)",
                   workOrderId);

               return Result.Success;
           }
       }

       private string GenerateIdempotencyKey(WorkOrderId workOrderId, ExternalSystemType system)
       {
           // Formato: doorx-{system}-{workOrderId}-{version}
           return $"doorx-{system.ToString().ToLower()}-{workOrderId.Value}-v1";
       }
   }
   ```

2. **Detectar duplicados al importar desde PMS**
   ```csharp
   public class PmsPropertySyncService
   {
       public async Task<SyncResult> SyncPropertiesAsync(ExternalSystemConfig config)
       {
           var externalProperties = await _pmsProvider.GetPropertiesAsync(config);

           var created = 0;
           var updated = 0;
           var skipped = 0;

           foreach (var extProp in externalProperties)
           {
               // Buscar por ExternalId (único por sistema)
               var existing = await _propertyRepository
                   .GetByExternalIdAsync(extProp.ExternalId, config.SystemType);

               if (existing is null)
               {
                   // Verificar que no exista por dirección (data quality check)
                   var duplicateCheck = await _propertyRepository
                       .GetByAddressAsync(extProp.Address);

                   if (duplicateCheck is not null)
                   {
                       _logger.LogWarning(
                           "Potential duplicate property found: External ID {ExternalId} matches address of existing property {PropertyId}",
                           extProp.ExternalId,
                           duplicateCheck.Id);

                       // Vincular external ID al existente
                       duplicateCheck.SetExternalReference(extProp.ExternalId, config.SystemType);
                       updated++;
                       continue;
                   }

                   // Crear nuevo
                   var newProperty = Property.CreateFromExternal(
                       extProp.Address,
                       extProp.Type,
                       extProp.ExternalId,
                       config.SystemType);

                   await _propertyRepository.AddAsync(newProperty.Value);
                   created++;
               }
               else
               {
                   // Actualizar existente (solo si cambió)
                   if (HasChanged(existing, extProp))
                   {
                       existing.UpdateFromExternal(extProp.Address, extProp.Type);
                       updated++;
                   }
                   else
                   {
                       skipped++;
                   }
               }
           }

           await _unitOfWork.SaveChangesAsync();

           return new SyncResult(created, updated, skipped);
       }

       private bool HasChanged(Property existing, ExternalProperty external)
       {
           // Comparar por hash o propiedades específicas
           return existing.Address.ToString() != external.Address.ToString() ||
                  existing.PropertyType != external.Type;
       }
   }
   ```

3. **Sync tokens para cambios incrementales**
   ```csharp
   public class ExternalSystemConfig : AggregateRoot<ExternalSystemConfigId>
   {
       public string? LastSyncToken { get; private set; }
       public DateTime? LastFullSyncAt { get; private set; }
       public DateTime? LastIncrementalSyncAt { get; private set; }

       public void UpdateSyncToken(string token, bool isFullSync)
       {
           LastSyncToken = token;

           if (isFullSync)
           {
               LastFullSyncAt = DateTime.UtcNow;
           }
           else
           {
               LastIncrementalSyncAt = DateTime.UtcNow;
           }

           UpdateModifiedDate();
       }
   }

   // Sincronización incremental
   public class IncrementalSyncService
   {
       public async Task<SyncResult> SyncChangesAsync(ExternalSystemConfig config)
       {
           // Usar sync token para obtener solo cambios desde última vez
           var changes = await _pmsProvider.GetChangesSinceAsync(
               config.LastSyncToken,
               config);

           foreach (var change in changes.Properties)
           {
               switch (change.ChangeType)
               {
                   case ChangeType.Created:
                       await CreatePropertyAsync(change.Data);
                       break;

                   case ChangeType.Updated:
                       await UpdatePropertyAsync(change.Data);
                       break;

                   case ChangeType.Deleted:
                       await SoftDeletePropertyAsync(change.ExternalId);
                       break;
               }
           }

           // Guardar nuevo sync token
           config.UpdateSyncToken(changes.NextSyncToken, isFullSync: false);
           await _unitOfWork.SaveChangesAsync();

           return new SyncResult(changes.TotalChanges);
       }
   }
   ```

4. **Deduplicación de webhooks**
   ```csharp
   // Webhooks pueden ser enviados múltiples veces
   public class WebhookDeduplicationService
   {
       private readonly IDistributedCache _cache;

       public async Task<bool> IsD duplicateAsync(string webhookId, TimeSpan ttl)
       {
           var cacheKey = $"webhook:processed:{webhookId}";
           var existing = await _cache.GetStringAsync(cacheKey);

           if (existing is not null)
           {
               _logger.LogInformation(
                   "Webhook {WebhookId} already processed, skipping",
                   webhookId);
               return true; // Es duplicado
           }

           // Marcar como procesado
           await _cache.SetStringAsync(
               cacheKey,
               DateTime.UtcNow.ToString(),
               new DistributedCacheEntryOptions
               {
                   AbsoluteExpirationRelativeToNow = ttl
               });

           return false; // No es duplicado
       }
   }

   // Webhook handler
   [HttpPost("webhooks/buildium/work-order")]
   public async Task<IActionResult> HandleBuildiumWebhook(
       [FromHeader(Name = "X-Buildium-Webhook-Id")] string webhookId,
       [FromBody] BuildiumWebhookPayload payload)
   {
       // Verificar duplicación (ventana de 24 horas)
       if (await _deduplicationService.IsDuplicateAsync(webhookId, TimeSpan.FromHours(24)))
       {
           return Ok(); // Ya procesado, retornar 200 para que no reintente
       }

       // Procesar webhook
       await _workOrderSyncService.ProcessWebhookAsync(payload);

       return Ok();
   }
   ```

### ⚠️ Violaciones Comunes
- No usar claves de idempotencia en APIs externas
- No detectar duplicados al importar
- Procesar webhooks múltiples veces
- No usar sync tokens para cambios incrementales
- No hacer sync completo periódicamente (drift)

---

## Regla 19: Gestión de Conversaciones con IA

### 📌 Descripción
Las **conversaciones con Aimee** (IA) deben mantener **contexto**, ser **orientadas a objetivos** (resolver el Work Order), y tener **fallback a humano** cuando sea necesario.

### 🎯 Razón de Negocio
- Automatizar comunicación repetitiva
- Escalar sin aumentar staff
- Mejorar tiempo de respuesta
- Mantener calidad de servicio

### ✅ Reglas Específicas

1. **Contexto de conversación persistente**
   ```csharp
   public class AiAssistantService
   {
       private readonly IOpenAiClient _openAiClient;
       private readonly IConversationRepository _conversationRepo;

       public async Task<ErrorOr<string>> GenerateResponseAsync(
           ConversationId conversationId,
           string userMessage)
       {
           // 1. Obtener conversación completa para contexto
           var conversation = await _conversationRepo.GetByIdAsync(conversationId);
           var workOrder = await _workOrderRepo.GetByIdAsync(conversation.WorkOrderId);
           var tenant = await _tenantRepo.GetByIdAsync(conversation.TenantId);

           // 2. Construir contexto para IA
           var systemPrompt = BuildSystemPrompt(workOrder, tenant);
           var conversationHistory = BuildConversationHistory(conversation.Messages);

           // 3. Llamar a OpenAI con contexto completo
           var response = await _openAiClient.GetChatCompletionAsync(
               systemPrompt,
               conversationHistory,
               userMessage);

           if (response.IsError)
               return response.Errors;

           // 4. Verificar si requiere escalamiento a humano
           if (ShouldEscalateToHuman(response.Value, conversation))
           {
               await EscalateToPropertyManagerAsync(conversation);
               return "He notificado a un representante que te contactará pronto.";
           }

           return response.Value;
       }

       private string BuildSystemPrompt(WorkOrder workOrder, Tenant tenant)
       {
           return $@"
Eres Aimee, un asistente virtual experto en mantenimiento de propiedades.

CONTEXTO DEL WORK ORDER:
- ID: {workOrder.Id}
- Categoría: {workOrder.Category}
- Prioridad: {workOrder.Priority}
- Estado: {workOrder.Status}
- Descripción original: {workOrder.Description}

INFORMACIÓN DEL TENANT:
- Nombre: {tenant.Name}
- Idioma preferido: {tenant.PreferredLanguage}
- Property: {workOrder.Property.Address}

TU OBJETIVO:
1. Hacer preguntas de diagnóstico relevantes
2. Categorizar correctamente el problema
3. Asignar prioridad apropiada
4. Coordinar con vendor para schedule
5. Mantener informado al tenant
6. Confirmar satisfacción al completar

REGLAS:
- Sé conciso y claro (mensajes de SMS)
- Usa el idioma preferido del tenant ({tenant.PreferredLanguage})
- Si no puedes ayudar, escala a un humano
- No prometas fechas sin confirmar con vendor
- Siempre sé empático y profesional
";
       }

       private List<ChatMessage> BuildConversationHistory(IEnumerable<Message> messages)
       {
           return messages
               .OrderBy(m => m.SentAt)
               .Select(m => new ChatMessage
               {
                   Role = m.Sender == ParticipantType.AI ? "assistant" : "user",
                   Content = m.Content,
                   Timestamp = m.SentAt
               })
               .ToList();
       }

       private bool ShouldEscalateToHuman(string aiResponse, Conversation conversation)
       {
           // Escalar si:
           // - IA expresa incertidumbre
           // - Tenant está frustrado
           // - Más de 10 mensajes sin resolución
           // - Problema complejo fuera de scope

           var uncertaintyPhrases = new[]
           {
               "no estoy segura",
               "no puedo ayudar",
               "necesitas hablar con",
               "i'm not sure",
               "i can't help"
           };

           if (uncertaintyPhrases.Any(phrase =>
               aiResponse.ToLower().Contains(phrase)))
           {
               return true;
           }

           if (conversation.Messages.Count >= 10 &&
               conversation.WorkOrder.Status == WorkOrderStatus.Open)
           {
               return true;
           }

           // Detectar frustración del tenant
           var lastTenantMessages = conversation.Messages
               .Where(m => m.Sender == ParticipantType.Tenant)
               .TakeLast(3)
               .Select(m => m.Content.ToLower());

           var frustrationKeywords = new[]
           {
               "terrible", "horrible", "awful", "frustrated", "angry",
               "terrible", "horrible", "frustrado", "enojado"
           };

           if (lastTenantMessages.Any(msg =>
               frustrationKeywords.Any(keyword => msg.Contains(keyword))))
           {
               return true;
           }

           return false;
       }
   }
   ```

2. **Estados de conversación con workflow**
   ```csharp
   public enum ConversationState
   {
       InitialDiagnosis = 1,    // Aimee haciendo preguntas
       VendorSearch = 2,        // Buscando vendor disponible
       SchedulingCoordination = 3, // Coordinando horario
       AwaitingService = 4,     // Esperando visita del vendor
       ServiceInProgress = 5,   // Vendor trabajando
       ConfirmingCompletion = 6,// Confirmando satisfacción
       Completed = 7,           // Work Order cerrado
       EscalatedToHuman = 99    // Escalado a Property Manager
   }

   public class Conversation : AggregateRoot<ConversationId>
   {
       public ConversationState State { get; private set; }

       public ErrorOr<Success> TransitionTo(ConversationState newState)
       {
           if (!IsValidTransition(State, newState))
               return Error.Conflict(
                   code: "Conversation.InvalidStateTransition",
                   description: $"Cannot transition from {State} to {newState}");

           var oldState = State;
           State = newState;
           UpdateModifiedDate();

           AddDomainEvent(new ConversationStateChangedEvent(
               Id,
               oldState,
               newState,
               DateTime.UtcNow));

           return Result.Success;
       }

       private bool IsValidTransition(ConversationState from, ConversationState to)
       {
           // Definir transiciones válidas
           var validTransitions = new Dictionary<ConversationState, ConversationState[]>
           {
               [ConversationState.InitialDiagnosis] = new[]
               {
                   ConversationState.VendorSearch,
                   ConversationState.EscalatedToHuman
               },
               [ConversationState.VendorSearch] = new[]
               {
                   ConversationState.SchedulingCoordination,
                   ConversationState.EscalatedToHuman
               },
               [ConversationState.SchedulingCoordination] = new[]
               {
                   ConversationState.AwaitingService,
                   ConversationState.VendorSearch // Re-schedule
               },
               [ConversationState.AwaitingService] = new[]
               {
                   ConversationState.ServiceInProgress
               },
               [ConversationState.ServiceInProgress] = new[]
               {
                   ConversationState.ConfirmingCompletion
               },
               [ConversationState.ConfirmingCompletion] = new[]
               {
                   ConversationState.Completed,
                   ConversationState.ServiceInProgress // Re-work needed
               },
               [ConversationState.EscalatedToHuman] = new[]
               {
                   ConversationState.AwaitingService, // PM resolvió
                   ConversationState.Completed
               }
           };

           return validTransitions.TryGetValue(from, out var allowed) &&
                  allowed.Contains(to);
       }
   }
   ```

3. **Function calling para acciones específicas**
   ```csharp
   public class AiAssistantService
   {
       private readonly FunctionDefinition[] _availableFunctions = new[]
       {
           new FunctionDefinition
           {
               Name = "search_available_vendors",
               Description = "Search for vendors available for a specific service category and location",
               Parameters = new
               {
                   service_category = "string",
                   zip_code = "string",
                   priority = "string"
               }
           },
           new FunctionDefinition
           {
               Name = "schedule_vendor",
               Description = "Schedule a vendor for a specific date and time",
               Parameters = new
               {
                   vendor_id = "string",
                   date = "string (ISO 8601)",
                   time_slot = "string"
               }
           },
           new FunctionDefinition
           {
               Name = "update_work_order_priority",
               Description = "Update the priority of a work order based on new information",
               Parameters = new
               {
                   priority = "string (Emergency, High, Normal, Low)",
                   reason = "string"
               }
           },
           new FunctionDefinition
           {
               Name = "escalate_to_human",
               Description = "Escalate the conversation to a human property manager",
               Parameters = new
               {
                   reason = "string"
               }
           }
       };

       public async Task<ErrorOr<string>> ProcessWithFunctionsAsync(
           Conversation conversation,
           string userMessage)
       {
           var response = await _openAiClient.GetCompletionWithFunctionsAsync(
               BuildSystemPrompt(conversation),
               BuildHistory(conversation),
               userMessage,
               _availableFunctions);

           // Si IA decidió llamar una función
           if (response.FunctionCall is not null)
           {
               var functionResult = await ExecuteFunctionAsync(
                   response.FunctionCall.Name,
                   response.FunctionCall.Arguments,
                   conversation);

               if (functionResult.IsError)
                   return functionResult.Errors;

               // Llamar a IA nuevamente con el resultado de la función
               return await _openAiClient.GetCompletionAsync(
                   BuildSystemPrompt(conversation),
                   BuildHistory(conversation),
                   $"Function result: {functionResult.Value}");
           }

           return response.Message;
       }

       private async Task<ErrorOr<string>> ExecuteFunctionAsync(
           string functionName,
           Dictionary<string, object> arguments,
           Conversation conversation)
       {
           return functionName switch
           {
               "search_available_vendors" => await SearchVendorsAsync(arguments, conversation),
               "schedule_vendor" => await ScheduleVendorAsync(arguments, conversation),
               "update_work_order_priority" => await UpdatePriorityAsync(arguments, conversation),
               "escalate_to_human" => await EscalateToHumanAsync(arguments, conversation),
               _ => Error.Validation(code: "AI.UnknownFunction", description: $"Unknown function: {functionName}")
           };
       }
   }
   ```

4. **Límites y safeguards**
   ```csharp
   public class ConversationSafeguards
   {
       // Máximo de mensajes en una conversación antes de escalar
       private const int MaxMessagesBeforeEscalation = 15;

       // Máximo costo por conversación (OpenAI tokens)
       private const decimal MaxCostPerConversation = 1.00m;

       // Timeout de inactividad (auto-close)
       private static readonly TimeSpan InactivityTimeout = TimeSpan.FromDays(3);

       public async Task<ErrorOr<Success>> ValidateConversationAsync(Conversation conversation)
       {
           // 1. Verificar límite de mensajes
           if (conversation.Messages.Count >= MaxMessagesBeforeEscalation)
           {
               await _escalationService.EscalateAsync(
                   conversation,
                   "Exceeded maximum messages limit");

               return Error.Conflict(
                   code: "Conversation.MaxMessagesReached",
                   description: "Conversation has been escalated due to length");
           }

           // 2. Verificar costo acumulado
           var totalCost = await _costTracker.GetConversationCostAsync(conversation.Id);
           if (totalCost >= MaxCostPerConversation)
           {
               await _escalationService.EscalateAsync(
                   conversation,
                   $"Exceeded cost limit: ${totalCost}");

               return Error.Conflict(
                   code: "Conversation.CostLimitExceeded",
                   description: "Conversation cost limit exceeded");
           }

           // 3. Verificar inactividad
           var lastMessage = conversation.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
           if (lastMessage is not null &&
               DateTime.UtcNow - lastMessage.SentAt > InactivityTimeout)
           {
               conversation.Close(ConversationCloseReason.Inactivity);

               return Error.Conflict(
                   code: "Conversation.Inactive",
                   description: "Conversation closed due to inactivity");
           }

           return Result.Success;
       }
   }
   ```

### ⚠️ Violaciones Comunes
- No mantener contexto de conversación
- No escalar a humano cuando es necesario
- Promesas que la IA no puede cumplir
- No limitar costo/longitud de conversaciones
- No tener fallback cuando IA falla

---

## Regla 20: Validación de Datos Externos

### 📌 Descripción
Los **datos provenientes de sistemas externos** (PMS, webhooks, APIs) deben ser **validados rigurosamente** antes de ser persistidos o usados en lógica de negocio.

### 🎯 Razón de Negocio
- Proteger integridad de datos
- Evitar inyección maliciosa
- Detectar inconsistencias temprano
- Mantener calidad de datos

### ✅ Reglas Específicas

1. **Validación en la frontera (Anti-Corruption Layer)**
   ```csharp
   // Anti-Corruption Layer para PMS
   public class PmsDataValidator
   {
       public ErrorOr<ValidatedProperty> ValidateProperty(ExternalProperty externalProp)
       {
           var errors = new List<Error>();

           // 1. Validar campos requeridos
           if (string.IsNullOrWhiteSpace(externalProp.ExternalId))
               errors.Add(Error.Validation(
                   code: "PMS.Property.MissingExternalId",
                   description: "External ID is required"));

           if (externalProp.Address is null)
               errors.Add(Error.Validation(
                   code: "PMS.Property.MissingAddress",
                   description: "Address is required"));

           // 2. Validar formato de datos
           if (!string.IsNullOrEmpty(externalProp.Address?.ZipCode) &&
               !IsValidZipCode(externalProp.Address.ZipCode))
               errors.Add(Error.Validation(
                   code: "PMS.Property.InvalidZipCode",
                   description: $"Invalid ZIP code: {externalProp.Address.ZipCode}"));

           // 3. Sanitizar datos de texto
           var sanitizedAddress = SanitizeText(externalProp.Address?.Street);
           var sanitizedCity = SanitizeText(externalProp.Address?.City);

           // 4. Validar rangos
           if (externalProp.Units.HasValue && externalProp.Units.Value < 0)
               errors.Add(Error.Validation(
                   code: "PMS.Property.InvalidUnits",
                   description: "Units cannot be negative"));

           if (errors.Any())
               return errors;

           return new ValidatedProperty
           {
               ExternalId = externalProp.ExternalId,
               Address = new Address(
                   sanitizedAddress,
                   sanitizedCity,
                   externalProp.Address.State,
                   externalProp.Address.ZipCode),
               PropertyType = externalProp.Type,
               Units = externalProp.Units
           };
       }

       private bool IsValidZipCode(string zipCode)
       {
           // US ZIP code: 5 digits or 5+4 format
           return Regex.IsMatch(zipCode, @"^\d{5}(-\d{4})?$");
       }

       private string SanitizeText(string? input)
       {
           if (string.IsNullOrEmpty(input))
               return string.Empty;

           // Remover caracteres peligrosos
           var sanitized = input
               .Replace("<", "")
               .Replace(">", "")
               .Replace("'", "")
               .Replace("\"", "")
               .Trim();

           // Limitar longitud
           return sanitized.Length > 200
               ? sanitized.Substring(0, 200)
               : sanitized;
       }
   }
   ```

2. **Schema validation para webhooks**
   ```csharp
   public class BuildiumWebhookValidator
   {
       private readonly IJsonSchemaValidator _schemaValidator;

       public ErrorOr<ValidatedWebhookPayload> ValidateWebhook(
           string payload,
           string signature,
           string timestamp)
       {
           // 1. Verificar firma (HMAC)
           if (!VerifySignature(payload, signature, timestamp))
               return Error.Forbidden(
                   code: "Webhook.InvalidSignature",
                   description: "Webhook signature verification failed");

           // 2. Verificar timestamp (evitar replay attacks)
           if (!IsTimestampRecent(timestamp, TimeSpan.FromMinutes(5)))
               return Error.Validation(
                   code: "Webhook.ExpiredTimestamp",
                   description: "Webhook timestamp is too old");

           // 3. Validar JSON schema
           var schemaValidation = _schemaValidator.Validate(payload, WebhookSchema);
           if (!schemaValidation.IsValid)
               return Error.Validation(
                   code: "Webhook.InvalidSchema",
                   description: $"Schema validation failed: {schemaValidation.Errors}");

           // 4. Deserializar y validar tipos
           var webhookData = JsonSerializer.Deserialize<BuildiumWebhookPayload>(payload);

           if (webhookData is null)
               return Error.Validation(
                   code: "Webhook.DeserializationFailed",
                   description: "Failed to deserialize webhook payload");

           // 5. Validar valores de negocio
           if (!Enum.IsDefined(typeof(WebhookEventType), webhookData.EventType))
               return Error.Validation(
                   code: "Webhook.UnknownEventType",
                   description: $"Unknown event type: {webhookData.EventType}");

           return new ValidatedWebhookPayload
           {
               EventType = webhookData.EventType,
               EntityId = webhookData.EntityId,
               Data = webhookData.Data,
               OccurredAt = webhookData.OccurredAt
           };
       }

       private bool VerifySignature(string payload, string signature, string timestamp)
       {
           var secretKey = _configuration["Buildium:WebhookSecret"];
           var message = $"{timestamp}.{payload}";

           using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
           var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
           var expectedSignature = Convert.ToBase64String(hash);

           return signature == expectedSignature;
       }

       private bool IsTimestampRecent(string timestamp, TimeSpan maxAge)
       {
           if (!long.TryParse(timestamp, out var unixTimestamp))
               return false;

           var webhookTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
           var age = DateTimeOffset.UtcNow - webhookTime;

           return age <= maxAge && age >= TimeSpan.Zero;
       }
   }
   ```

3. **Business rules validation**
   ```csharp
   public class ExternalTenantValidator
   {
       public ErrorOr<ValidatedTenant> ValidateTenant(ExternalTenant externalTenant)
       {
           var errors = new List<Error>();

           // 1. Email válido
           if (!string.IsNullOrEmpty(externalTenant.Email) &&
               !IsValidEmail(externalTenant.Email))
               errors.Add(DomainErrors.Tenant.InvalidEmail);

           // 2. Teléfono válido
           if (!string.IsNullOrEmpty(externalTenant.PhoneNumber) &&
               !IsValidPhoneNumber(externalTenant.PhoneNumber))
               errors.Add(DomainErrors.Tenant.InvalidPhoneNumber);

           // 3. Property assignment válido
           if (string.IsNullOrEmpty(externalTenant.PropertyExternalId))
               errors.Add(Error.Validation(
                   code: "Tenant.MissingProperty",
                   description: "Tenant must be assigned to a property"));

           // 4. Validar fechas
           if (externalTenant.LeaseStartDate.HasValue &&
               externalTenant.LeaseEndDate.HasValue &&
               externalTenant.LeaseStartDate > externalTenant.LeaseEndDate)
               errors.Add(Error.Validation(
                   code: "Tenant.InvalidLeaseDates",
                   description: "Lease start date cannot be after end date"));

           // 5. Validar que no sea un tenant "test" o "demo"
           if (IsTestData(externalTenant.Name, externalTenant.Email))
           {
               _logger.LogWarning(
                   "Skipping test tenant: {Name} / {Email}",
                   externalTenant.Name,
                   externalTenant.Email);

               return Error.Validation(
                   code: "Tenant.TestData",
                   description: "Test data detected, skipping");
           }

           if (errors.Any())
               return errors;

           return new ValidatedTenant
           {
               ExternalId = externalTenant.ExternalId,
               Name = externalTenant.Name,
               Email = NormalizeEmail(externalTenant.Email),
               PhoneNumber = NormalizePhoneNumber(externalTenant.PhoneNumber),
               PropertyExternalId = externalTenant.PropertyExternalId,
               PreferredLanguage = externalTenant.PreferredLanguage ?? "en"
           };
       }

       private bool IsValidEmail(string email)
       {
           try
           {
               var addr = new System.Net.Mail.MailAddress(email);
               return addr.Address == email;
           }
           catch
           {
               return false;
           }
       }

       private bool IsValidPhoneNumber(string phoneNumber)
       {
           // US/International phone number
           var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
           return cleaned.Length >= 10 && cleaned.Length <= 15;
       }

       private bool IsTestData(string name, string email)
       {
           var testIndicators = new[]
           {
               "test", "demo", "example", "sample",
               "fake", "dummy", "localhost"
           };

           var lowerName = name?.ToLower() ?? "";
           var lowerEmail = email?.ToLower() ?? "";

           return testIndicators.Any(indicator =>
               lowerName.Contains(indicator) || lowerEmail.Contains(indicator));
       }

       private string NormalizeEmail(string email)
       {
           return email?.Trim().ToLowerInvariant() ?? string.Empty;
       }

       private string NormalizePhoneNumber(string phoneNumber)
       {
           if (string.IsNullOrEmpty(phoneNumber))
               return string.Empty;

           // Extraer solo dígitos
           var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

           // Formato: +1XXXXXXXXXX (US)
           if (digits.Length == 10)
               return $"+1{digits}";

           if (digits.Length == 11 && digits[0] == '1')
               return $"+{digits}";

           return $"+{digits}";
       }
   }
   ```

4. **Logging y alertas para datos inválidos**
   ```csharp
   public class DataQualityMonitor
   {
       private readonly ILogger<DataQualityMonitor> _logger;
       private readonly IMetricsCollector _metrics;

       public async Task ReportValidationFailureAsync(
           string source,
           string entityType,
           List<Error> errors)
       {
           // 1. Log estructurado
           _logger.LogWarning(
               "Data validation failed for {EntityType} from {Source}. Errors: {Errors}",
               entityType,
               source,
               string.Join(", ", errors.Select(e => e.Code)));

           // 2. Métricas
           _metrics.IncrementCounter(
               "data_validation_failures",
               tags: new Dictionary<string, string>
               {
                   ["source"] = source,
                   ["entity_type"] = entityType,
                   ["error_code"] = errors.First().Code
               });

           // 3. Alertar si hay muchos fallos
           var recentFailures = await _metrics.GetCounterValueAsync(
               "data_validation_failures",
               TimeSpan.FromHours(1));

           if (recentFailures >= 100)
           {
               await _alertService.SendAlertAsync(new Alert
               {
                   Severity = AlertSeverity.High,
                   Title = "High Data Validation Failure Rate",
                   Message = $"100+ validation failures in the last hour from {source}",
                   Tags = new[] { "data-quality", source }
               });
           }
       }
   }
   ```

### ⚠️ Violaciones Comunes
- Confiar en datos externos sin validación
- No verificar firmas de webhooks (seguridad)
- No sanitizar texto (XSS, injection)
- No validar rangos y formatos
- No detectar datos de test/demo
- No normalizar datos (emails, teléfonos)
- No monitorear calidad de datos

---

## 📊 Resumen de Reglas Críticas

### Reglas de Dominio (DDD)

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

### Reglas de Integraciones y Funcionalidades

| # | Regla | Impacto | Validación |
|---|-------|---------|------------|
| 11 | Sincronización con PMS | Alto | Solo datos maestros, NO finanzas |
| 12 | Categorización Automática por IA | Alto | Fallback si IA falla, permitir override |
| 13 | Comunicación Multi-Canal | Alto | Mantener contexto entre canales |
| 14 | Priorización de Work Orders | Alto | SLAs por prioridad, auto-escalamiento |
| 15 | Notificaciones y Alertas | Medio | Event-driven, respetar preferencias |
| 16 | Manejo de Fallos en Integraciones | Alto | Retry, circuit breaker, fallbacks |
| 17 | Rate Limiting para APIs | Medio | Respetar límites, track costos |
| 18 | Idempotencia en Sincronizaciones | Alto | Claves de idempotencia, dedup webhooks |
| 19 | Gestión de Conversaciones con IA | Alto | Mantener contexto, escalar a humano |
| 20 | Validación de Datos Externos | Alto | Anti-Corruption Layer, sanitización |

---

## 🎯 Criterios de Criticidad

### Alto Impacto
Estas reglas **deben cumplirse siempre**. Su violación puede causar:
- Inconsistencia de datos
- Pérdida de información
- Problemas de seguridad
- Fallos en cascada
- Incumplimiento de SLAs

### Medio Impacto
Estas reglas **deberían cumplirse**. Su violación puede causar:
- Degradación de experiencia de usuario
- Problemas de rendimiento
- Costos adicionales
- Dificultad en mantenimiento

---

## 🔗 Referencias

### Documentación Interna
- [ARCHITECTURE.md](ARCHITECTURE.md) - Arquitectura del sistema
- [UBIQUITOUS_LANGUAGE.md](UBIQUITOUS_LANGUAGE.md) - Lenguaje ubicuo del dominio
- [CICD.md](CICD.md) - CI/CD y deployment
- [/src/Domain/Common/README.md](../src/Domain/Common/README.md) - Guía de clases base DDD

### APIs Externas
- [OpenAI API Documentation](https://platform.openai.com/docs) - Aimee (IA Assistant)
- [Twilio API Documentation](https://www.twilio.com/docs) - SMS/WhatsApp messaging
- [Buildium API](https://api.buildium.com) - Property Management System
- [Hostify API](https://hostify.com/api-documentation) - Property Management System
- [AppFolio API](https://api.appfolio.com) - Property Management System

### Patrones y Prácticas
- [ErrorOr Library](https://github.com/amantinband/error-or) - Manejo de errores funcional
- [Polly](https://github.com/App-vNext/Polly) - Resiliencia (retry, circuit breaker)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Clean Architecture - Robert Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

**Última actualización:** 2024-11-24
**Versión:** 2.0.0 (Agregadas reglas 11-20 sobre integraciones y funcionalidades)
