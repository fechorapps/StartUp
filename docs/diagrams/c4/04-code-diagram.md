# DoorX - Code Diagram (C4 Level 4)

## Descripción

Diagramas de clases mostrando la implementación detallada de componentes críticos del sistema. Este nivel es opcional y se usa para documentar partes complejas o críticas.

**Nivel:** C4 Level 4 - Code/Class Diagrams
**Audiencia:** Desarrolladores trabajando en el código
**Propósito:** Documentar la implementación específica de componentes complejos

---

## WorkOrder Aggregate - Class Diagram

```mermaid
classDiagram
    class AggregateRoot~WorkOrderId~ {
        <<abstract>>
        #TId Id
        #List~IDomainEvent~ _domainEvents
        +IReadOnlyCollection~IDomainEvent~ DomainEvents
        #AddDomainEvent(IDomainEvent)
        +ClearDomainEvents()
    }

    class WorkOrder {
        -const int MaxBidsAllowed = 5
        -List~VendorBid~ _bids
        +TenantId TenantId
        +PropertyId PropertyId
        +string IssueDescription
        +ServiceCategory Category
        +Priority Priority
        +WorkOrderStatus Status
        +VendorId? AssignedVendorId
        +Money? FinalCost
        +IReadOnlyCollection~VendorBid~ Bids
        -WorkOrder(id, tenantId, propertyId, ...)
        +Create(tenantId, propertyId, ...) WorkOrder$
        +AddBid(vendorId, amount, message) ErrorOr~VendorBid~
        +AssignVendor(vendorId) ErrorOr~Success~
        +Complete(finalCost) ErrorOr~Success~
        +Cancel(reason) ErrorOr~Success~
        -CanAddMoreBids() bool
        -IsValidStatusTransition(newStatus) bool
    }

    class VendorBid {
        +VendorBidId Id
        +WorkOrderId WorkOrderId
        +VendorId VendorId
        +Money BidAmount
        +string Message
        +DateTime SubmittedAt
        +BidStatus Status
        -VendorBid(id, workOrderId, vendorId, ...)
        +Create(workOrderId, vendorId, ...) VendorBid$
        +Accept() ErrorOr~Success~
        +Reject() ErrorOr~Success~
    }

    class WorkOrderId {
        +Guid Value
        +WorkOrderId(Guid value)
        +ToString() string
        +Equals(object?) bool
        +GetHashCode() int
    }

    class ServiceCategory {
        <<enumeration>>
        Plumbing
        Electrical
        HVAC
        Appliance
        Carpentry
        Painting
        Cleaning
        Landscaping
        Other
    }

    class Priority {
        <<enumeration>>
        Low
        Medium
        High
        Emergency
    }

    class WorkOrderStatus {
        <<enumeration>>
        Open
        BiddingInProgress
        VendorAssigned
        InProgress
        Completed
        Cancelled
    }

    class Money {
        +decimal Amount
        +Currency Currency
        +Money(decimal amount, Currency currency)
        +Add(Money other) Money
        +Subtract(Money other) Money
        +Multiply(decimal factor) Money
        +Equals(object?) bool
    }

    AggregateRoot~WorkOrderId~ <|-- WorkOrder : inherits
    WorkOrder "1" *-- "0..5" VendorBid : contains
    WorkOrder --> WorkOrderId : has
    WorkOrder --> ServiceCategory : has
    WorkOrder --> Priority : has
    WorkOrder --> WorkOrderStatus : has
    WorkOrder --> Money : has
    VendorBid --> WorkOrderId : references
```

---

## Repository Pattern - Interface & Implementation

```mermaid
classDiagram
    class IRepository~TEntity,TId~ {
        <<interface>>
        +GetByIdAsync(TId id) Task~TEntity?~
        +GetAllAsync() Task~IEnumerable~TEntity~~
        +AddAsync(TEntity entity) Task
        +Update(TEntity entity) void
        +Delete(TEntity entity) void
    }

    class IWorkOrderRepository {
        <<interface>>
        +GetByTenantAsync(TenantId tenantId) Task~IEnumerable~WorkOrder~~
        +GetOpenWorkOrdersAsync() Task~IEnumerable~WorkOrder~~
        +GetWithBidsAsync(WorkOrderId id) Task~WorkOrder?~
        +GetByPropertyAsync(PropertyId propertyId) Task~IEnumerable~WorkOrder~~
    }

    class WorkOrderRepository {
        -ApplicationDbContext _context
        +WorkOrderRepository(ApplicationDbContext context)
        +GetByIdAsync(WorkOrderId id) Task~WorkOrder?~
        +GetAllAsync() Task~IEnumerable~WorkOrder~~
        +AddAsync(WorkOrder entity) Task
        +Update(WorkOrder entity) void
        +Delete(WorkOrder entity) void
        +GetByTenantAsync(TenantId tenantId) Task~IEnumerable~WorkOrder~~
        +GetOpenWorkOrdersAsync() Task~IEnumerable~WorkOrder~~
        +GetWithBidsAsync(WorkOrderId id) Task~WorkOrder?~
        +GetByPropertyAsync(PropertyId propertyId) Task~IEnumerable~WorkOrder~~
    }

    class ApplicationDbContext {
        +DbSet~WorkOrder~ WorkOrders
        +DbSet~Vendor~ Vendors
        +DbSet~Property~ Properties
        +DbSet~Tenant~ Tenants
        +ApplicationDbContext(DbContextOptions options)
        #OnModelCreating(ModelBuilder modelBuilder)
    }

    IRepository~TEntity,TId~ <|-- IWorkOrderRepository : extends
    IWorkOrderRepository <|.. WorkOrderRepository : implements
    WorkOrderRepository --> ApplicationDbContext : uses
```

---

## CQRS - Command Handler Pattern

```mermaid
classDiagram
    class ICommandHandler~TCommand,TResult~ {
        <<interface>>
        +Handle(TCommand command, CancellationToken ct) Task~ErrorOr~TResult~~
    }

    class CreateWorkOrderCommand {
        +TenantId TenantId
        +PropertyId PropertyId
        +string IssueDescription
        +ContactInfo ContactInfo
    }

    class CreateWorkOrderCommandValidator {
        +CreateWorkOrderCommandValidator()
        +Validate(CreateWorkOrderCommand command) ValidationResult
    }

    class CreateWorkOrderCommandHandler {
        -IWorkOrderRepository _workOrderRepository
        -ITenantRepository _tenantRepository
        -IPropertyRepository _propertyRepository
        -IOpenAIService _aiService
        -IUnitOfWork _unitOfWork
        +CreateWorkOrderCommandHandler(...)
        +Handle(CreateWorkOrderCommand command, CancellationToken ct) Task~ErrorOr~WorkOrderId~~
    }

    class IOpenAIService {
        <<interface>>
        +CategorizeIssueAsync(string description) Task~ServiceCategory~
        +DeterminePriorityAsync(string description) Task~Priority~
    }

    class IUnitOfWork {
        <<interface>>
        +SaveChangesAsync(CancellationToken ct) Task~int~
        +BeginTransactionAsync() Task~IDbContextTransaction~
    }

    ICommandHandler~TCommand,TResult~ <|.. CreateWorkOrderCommandHandler : implements
    CreateWorkOrderCommandHandler ..> CreateWorkOrderCommand : handles
    CreateWorkOrderCommandHandler --> IWorkOrderRepository : uses
    CreateWorkOrderCommandHandler --> IOpenAIService : uses
    CreateWorkOrderCommandHandler --> IUnitOfWork : uses
    CreateWorkOrderCommand --> CreateWorkOrderCommandValidator : validated by
```

---

## Factory Pattern - PMS Provider Selection

```mermaid
classDiagram
    class ITicketSystemProvider {
        <<interface>>
        +CreateWorkOrderAsync(WorkOrder workOrder) Task~string~
        +UpdateWorkOrderStatusAsync(string externalId, WorkOrderStatus status) Task
        +GetVendorsAsync(ServiceCategory category, Address location) Task~IEnumerable~ExternalVendor~~
        +SyncWorkOrderAsync(string externalId) Task~WorkOrder~
    }

    class BuildiumProvider {
        -HttpClient _httpClient
        -BuildiumConfig _config
        +BuildiumProvider(HttpClient client, IOptions~BuildiumConfig~ config)
        +CreateWorkOrderAsync(WorkOrder workOrder) Task~string~
        +UpdateWorkOrderStatusAsync(string externalId, WorkOrderStatus status) Task
        +GetVendorsAsync(ServiceCategory category, Address location) Task~IEnumerable~ExternalVendor~~
        +SyncWorkOrderAsync(string externalId) Task~WorkOrder~
    }

    class HostifyProvider {
        -HttpClient _httpClient
        -HostifyConfig _config
        +HostifyProvider(HttpClient client, IOptions~HostifyConfig~ config)
        +CreateWorkOrderAsync(WorkOrder workOrder) Task~string~
        +UpdateWorkOrderStatusAsync(string externalId, WorkOrderStatus status) Task
        +GetVendorsAsync(ServiceCategory category, Address location) Task~IEnumerable~ExternalVendor~~
        +SyncWorkOrderAsync(string externalId) Task~WorkOrder~
    }

    class TicketSystemProviderFactory {
        -IServiceProvider _serviceProvider
        -IPropertyRepository _propertyRepository
        +TicketSystemProviderFactory(IServiceProvider serviceProvider, IPropertyRepository repo)
        +GetProviderForPropertyAsync(PropertyId propertyId) Task~ITicketSystemProvider~
        -GetProviderByType(ERPType erpType) ITicketSystemProvider
    }

    class ERPType {
        <<enumeration>>
        Buildium
        Hostify
        AppFolio
        None
    }

    ITicketSystemProvider <|.. BuildiumProvider : implements
    ITicketSystemProvider <|.. HostifyProvider : implements
    TicketSystemProviderFactory --> ITicketSystemProvider : creates
    TicketSystemProviderFactory --> ERPType : uses
```

---

## Domain Events - Publisher/Subscriber

```mermaid
classDiagram
    class IDomainEvent {
        <<interface>>
        +DateTime OccurredOn
    }

    class WorkOrderCreatedEvent {
        +WorkOrderId WorkOrderId
        +TenantId TenantId
        +PropertyId PropertyId
        +ServiceCategory Category
        +Priority Priority
        +DateTime OccurredOn
        +WorkOrderCreatedEvent(workOrderId, tenantId, ...)
    }

    class VendorAssignedEvent {
        +WorkOrderId WorkOrderId
        +VendorId VendorId
        +DateTime OccurredOn
        +VendorAssignedEvent(workOrderId, vendorId)
    }

    class IDomainEventHandler~TEvent~ {
        <<interface>>
        +Handle(TEvent event, CancellationToken ct) Task
    }

    class WorkOrderCreatedEventHandler {
        -IMessagingService _messagingService
        -IPropertyRepository _propertyRepository
        +WorkOrderCreatedEventHandler(...)
        +Handle(WorkOrderCreatedEvent event, CancellationToken ct) Task
    }

    class VendorAssignedEventHandler {
        -IMessagingService _messagingService
        -IVendorRepository _vendorRepository
        -ITicketSystemProviderFactory _providerFactory
        +VendorAssignedEventHandler(...)
        +Handle(VendorAssignedEvent event, CancellationToken ct) Task
    }

    IDomainEvent <|.. WorkOrderCreatedEvent : implements
    IDomainEvent <|.. VendorAssignedEvent : implements
    IDomainEventHandler~TEvent~ <|.. WorkOrderCreatedEventHandler : implements
    IDomainEventHandler~TEvent~ <|.. VendorAssignedEventHandler : implements
    WorkOrderCreatedEventHandler ..> WorkOrderCreatedEvent : handles
    VendorAssignedEventHandler ..> VendorAssignedEvent : handles
```

---

## Value Objects - Immutability & Validation

```mermaid
classDiagram
    class ValueObject {
        <<abstract>>
        #GetEqualityComponents() IEnumerable~object~*
        +Equals(object?) bool
        +GetHashCode() int
        +operator ==(ValueObject?, ValueObject?) bool
        +operator !=(ValueObject?, ValueObject?) bool
    }

    class Address {
        +string Street
        +string City
        +string State
        +string ZipCode
        +string Country
        -Address(street, city, state, zipCode, country)
        +Create(street, city, state, zipCode, country) ErrorOr~Address~$
        +ToString() string
        #GetEqualityComponents() IEnumerable~object~
        -Validate() ErrorOr~Success~
    }

    class Rating {
        +decimal Value
        -Rating(decimal value)
        +Create(decimal value) ErrorOr~Rating~$
        +ToString() string
        #GetEqualityComponents() IEnumerable~object~
        -Validate() ErrorOr~Success~
    }

    class ContactInfo {
        +PhoneNumber PhoneNumber
        +Email? Email
        +PreferredChannel Channel
        -ContactInfo(phoneNumber, email, channel)
        +Create(phoneNumber, email, channel) ErrorOr~ContactInfo~$
        #GetEqualityComponents() IEnumerable~object~
    }

    class PhoneNumber {
        +string Value
        +string CountryCode
        -PhoneNumber(value, countryCode)
        +Create(value, countryCode) ErrorOr~PhoneNumber~$
        +ToString() string
        #GetEqualityComponents() IEnumerable~object~
    }

    ValueObject <|-- Address : inherits
    ValueObject <|-- Rating : inherits
    ValueObject <|-- ContactInfo : inherits
    ValueObject <|-- PhoneNumber : inherits
    ContactInfo --> PhoneNumber : contains
```

---

## Notas de Implementación

### Aggregate Design Principles

1. **Constructor privado + Factory method estático**
   ```csharp
   private WorkOrder(...) { }
   public static WorkOrder Create(...) { }
   ```

2. **Colecciones privadas con exposición de solo lectura**
   ```csharp
   private readonly List<VendorBid> _bids = new();
   public IReadOnlyCollection<VendorBid> Bids => _bids.AsReadOnly();
   ```

3. **Validación en el aggregate**
   ```csharp
   private bool CanAddMoreBids() => _bids.Count < MaxBidsAllowed;
   ```

### Repository Pattern

- **Interfaces en Domain layer** (Dependency Inversion)
- **Implementaciones en Infrastructure layer**
- Operaciones específicas del agregado (`GetWithBidsAsync`)

### CQRS Benefits

- **Separación de concerns:** Reads vs Writes
- **Optimización independiente:** Different models for queries
- **Escalabilidad:** Read replicas para queries

### Factory Pattern for PMS

- **Runtime selection** basado en configuración de Property
- **Extensible:** Nuevos providers sin modificar código existente
- **Testeable:** Mock del provider en tests

---

## Convenciones de Código

### Naming Conventions

- **Aggregates:** PascalCase singular (WorkOrder, Vendor)
- **Value Objects:** PascalCase descriptivo (ServiceCategory, Priority)
- **Commands:** Verb + Noun + "Command" (CreateWorkOrderCommand)
- **Handlers:** Command/Query + "Handler" (CreateWorkOrderCommandHandler)
- **Events:** Past tense + "Event" (WorkOrderCreatedEvent)

### ErrorOr Pattern

```csharp
// Return type
public ErrorOr<WorkOrder> CreateWorkOrder(...)

// Success case
return workOrder;

// Error case
return Error.Validation("WorkOrder.MaxBids", "Maximum bids reached");

// Consuming
result.Match(
    success => Ok(success),
    errors => Problem(errors)
);
```

---

## Referencias

- [Clean Architecture Code Example](https://github.com/jasontaylordev/CleanArchitecture)
- [DDD Aggregates](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [Value Objects](https://martinfowler.com/bliki/ValueObject.html)
- [ErrorOr Library](https://github.com/amantinband/error-or)
- [DoorX Domain Model](../../DOMAIN_MODEL.md)

---

📍 **Estás aquí:** C4 Level 4 - Code Diagram
📖 **Anterior:** [03-component-diagram.md](./03-component-diagram.md)
