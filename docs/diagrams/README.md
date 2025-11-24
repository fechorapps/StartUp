# Service Management System - DoorX

## Documentación de Arquitectura y Modelo de Dominio

**Versión:** 1.0  
**Última actualización:** Noviembre 2024  
**Stack:** .NET Core 8, Clean Architecture, DDD

---

## Tabla de Contenidos

1. [Visión General](#1-visión-general)
2. [Diagrama de Contexto](#2-diagrama-de-contexto)
3. [Arquitectura del Sistema](#3-arquitectura-del-sistema)
4. [Bounded Contexts](#4-bounded-contexts)
5. [Modelo de Dominio](#5-modelo-de-dominio)
6. [Flujos Principales](#6-flujos-principales)
7. [Integración con Plataformas Externas](#7-integración-con-plataformas-externas)
8. [Webhooks](#8-webhooks)
9. [API Endpoints](#9-api-endpoints)
10. [Estructura del Proyecto](#10-estructura-del-proyecto)

---

## 1. Visión General

### 1.1 Descripción del Sistema

El **Service Management System** es una capa de IA intermedia que permite a huéspedes de propiedades de renta solicitar servicios de mantenimiento (plomería, electricidad, handyman, etc.) mediante conversación natural.

El sistema:
- Interpreta solicitudes en lenguaje natural usando IA (Claude API)
- Transforma las solicitudes en work orders estructuradas
- Envía las work orders a plataformas de Property Management (Buildium, AppFolio, Hostify)
- Recibe actualizaciones de estado via webhooks
- Notifica a los huéspedes sobre el progreso

### 1.2 Principio Fundamental

> **El sistema NO tiene contacto directo con los contratistas.**  
> Toda la comunicación con contratistas se realiza a través de las plataformas de Property Management vía webhooks.

### 1.3 Características Principales

| Característica | Descripción |
|----------------|-------------|
| **AI-First** | Conversación natural para solicitar servicios |
| **Multi-Platform** | Integración con múltiples PMS (Buildium, AppFolio, Hostify) |
| **Multi-Tenant** | Cada Property Owner tiene configuración aislada |
| **Real-Time** | Actualizaciones instantáneas via WebSocket |
| **Multi-Language** | Soporte para español e inglés |

---

## 2. Diagrama de Contexto

### 2.1 Actores del Sistema

| Actor | Tipo | Descripción | Interacción |
|-------|------|-------------|-------------|
| **Guest** | Usuario Final | Huésped que renta una propiedad | Directa - App móvil/web |
| **Property Owner** | Administrador | Dueño de las propiedades | Directa - Portal admin |
| **Contractor** | Externo | Profesional de servicios | **Indirecta** - Via plataformas PMS |

### 2.2 Sistemas Externos

| Sistema | Tipo | Propósito | Comunicación |
|---------|------|-----------|--------------|
| **Buildium** | PMS | Gestión de propiedades y vendors | REST API + Webhooks |
| **AppFolio** | PMS | Gestión de propiedades y contractors | REST API + Webhooks |
| **Hostify** | PMS | Short-term rentals (Airbnb, VRBO) | REST API + Webhooks |
| **Claude API** | IA | Procesamiento de lenguaje natural | REST API |

### 2.3 Diagrama de Flujo de Comunicación

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           FLUJO DE COMUNICACIÓN                                  │
└─────────────────────────────────────────────────────────────────────────────────┘

                    ┌─────────────┐
                    │    Guest    │
                    │    (App)    │
                    └──────┬──────┘
                           │
                           │ Chat / Solicitudes
                           │ [HTTPS/WSS]
                           ▼
              ┌────────────────────────────┐
              │                            │
              │   Service Management       │
              │        System              │
              │                            │
              │   • AI Assistant Layer     │
              │   • Intent Recognition     │
              │   • Work Order Creation    │
              │   • Status Tracking        │
              │                            │
              └─────────────┬──────────────┘
                            │
          ┌─────────────────┼─────────────────┐
          │                 │                 │
          ▼                 ▼                 ▼
   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
   │  Buildium   │   │  AppFolio   │   │   Hostify   │
   │             │   │             │   │             │
   │ • Vendors   │   │ • Vendors   │   │ • Vendors   │
   │ • WorkOrders│   │ • WorkOrders│   │ • Tasks     │
   │ • Webhooks  │   │ • Webhooks  │   │ • Webhooks  │
   └──────┬──────┘   └──────┬──────┘   └──────┬──────┘
          │                 │                 │
          │  Notifica y     │  Notifica y     │  Notifica y
          │  asigna         │  asigna         │  asigna
          ▼                 ▼                 ▼
   ┌─────────────────────────────────────────────────┐
   │                  CONTRACTORS                     │
   │                                                  │
   │   • Reciben trabajo desde su plataforma PMS     │
   │   • Actualizan estado en su plataforma PMS      │
   │   • NO conocen nuestro sistema directamente     │
   │                                                  │
   └─────────────────────────────────────────────────┘
```

### 2.4 Flujo de Datos

#### Outbound (Sistema → Plataformas)

```
Guest App ──► Sistema ──► Claude API (análisis de intent)
                 │
                 └──► Buildium/AppFolio/Hostify
                           │
                           ├──► Crea Work Order
                           ├──► Asigna Vendor (según reglas de la plataforma)
                           └──► Plataforma notifica al Contractor
```

#### Inbound (Plataformas → Sistema via Webhooks)

```
Contractor actualiza estado en Buildium/AppFolio/Hostify
                    │
                    ▼
Plataforma envía Webhook ──► Sistema
                                │
                                ├──► Valida firma del webhook
                                ├──► Actualiza ServiceRequest.Status
                                ├──► Notifica al Guest via Push/WebSocket
                                └──► Guarda historial de cambios
```

---

## 3. Arquitectura del Sistema

### 3.1 Clean Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              CAPAS DE LA ARQUITECTURA                            │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────┐
│                              PRESENTATION LAYER                                  │
│                                                                                  │
│   ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐                │
│   │ API Controllers │  │  SignalR Hubs   │  │   Middlewares   │                │
│   └─────────────────┘  └─────────────────┘  └─────────────────┘                │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              APPLICATION LAYER                                   │
│                                                                                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│   │   Commands   │  │   Queries    │  │  Validators  │  │     DTOs     │       │
│   │  & Handlers  │  │  & Handlers  │  │  (Fluent)    │  │              │       │
│   └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘       │
│                                                                                  │
│   ┌──────────────────────────────────────────────────────────────────────┐      │
│   │                    Interfaces / Ports                                 │      │
│   └──────────────────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                                DOMAIN LAYER                                      │
│                                                                                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│   │  Aggregates  │  │   Entities   │  │Value Objects │  │Domain Events │       │
│   └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘       │
│                                                                                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                          │
│   │   Enums      │  │Specifications│  │Domain Services│                         │
│   └──────────────┘  └──────────────┘  └──────────────┘                          │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            INFRASTRUCTURE LAYER                                  │
│                                                                                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│   │ Repositories │  │  DbContext   │  │External APIs │  │  AI Services │       │
│   └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘       │
│                                                                                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                          │
│   │  Event Bus   │  │   Caching    │  │   Webhooks   │                          │
│   └──────────────┘  └──────────────┘  └──────────────┘                          │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Contenedores (C4 Level 2)

| Contenedor | Tecnología | Responsabilidad |
|------------|------------|-----------------|
| **GuestApp** | React Native | App móvil para huéspedes |
| **AdminPortal** | Angular | Portal de administración |
| **API Gateway** | .NET Core 8 | REST API + SignalR Hub |
| **AIAssistantService** | .NET Core 8 | Procesamiento de IA |
| **IntegrationService** | .NET Core 8 | Integraciones con PMS |
| **SqlServer** | SQL Server 2022 | Base de datos principal |
| **Redis** | Redis 7 | Cache y message queue |

---

## 4. Bounded Contexts

### 4.1 Mapa de Contextos

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              BOUNDED CONTEXTS                                    │
└─────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────┐         ┌──────────────────────┐         ┌──────────────────────┐
│  PropertyManagement  │         │    ServiceRequest    │         │     AIAssistant      │
│                      │         │    (Core Domain)     │         │                      │
│  • Property          │────────►│                      │◄────────│  • Conversation      │
│  • Unit              │         │  • ServiceRequest    │         │  • ConversationMsg   │
│  • Guest             │         │  • SRMessage         │         │  • AIIntent          │
│  • Owner             │         │  • SRAttachment      │         │  • ConvContext       │
│  • Lease             │         │  • SRStatusHistory   │         │                      │
│                      │         │                      │         │                      │
│  Exports:            │         │  Imports:            │         │  Imports:            │
│  PropertyId, UnitId  │         │  PropertyId, UnitId  │         │  GuestId, PropertyId │
│  GuestId, OwnerId    │         │  GuestId             │         │  ServiceRequestId    │
└──────────────────────┘         └───────────┬──────────┘         └──────────────────────┘
                                             │
                                             │
                              ┌──────────────┴──────────────┐
                              │                             │
                              ▼                             ▼
               ┌──────────────────────┐      ┌──────────────────────┐
               │  WebhookProcessing   │      │ PlatformIntegration  │
               │                      │      │                      │
               │  • IncomingWebhook   │◄────►│  • Integration       │
               │  • WebhookHandler    │      │  • Credentials       │
               │  • ProcessingLog     │      │  • SyncHistory       │
               │                      │      │  • ExternalMapping   │
               │                      │      │                      │
               └──────────────────────┘      └───────────┬──────────┘
                                                         │
                                                         │ REST API + Webhooks
                                                         ▼
                                          ┌──────────────────────────────┐
                                          │     EXTERNAL PLATFORMS       │
                                          │  Buildium │ AppFolio │Hostify│
                                          └──────────────────────────────┘
                                                         │
                                                         │ (Comunicación indirecta)
                                                         ▼
                                          ┌──────────────────────────────┐
                                          │         CONTRACTORS          │
                                          │  (No interactúan con el     │
                                          │   sistema directamente)      │
                                          └──────────────────────────────┘
```

### 4.2 Descripción de Contextos

#### PropertyManagement Context
Gestiona la información base de propiedades, unidades, huéspedes y propietarios.

#### ServiceRequest Context (Core Domain)
El corazón del sistema. Gestiona el ciclo de vida completo de las solicitudes de servicio.

#### AIAssistant Context
Maneja las conversaciones con el asistente de IA, reconocimiento de intenciones y generación de respuestas.

#### WebhookProcessing Context
Procesa los webhooks entrantes de las plataformas externas de manera confiable y ordenada.

#### PlatformIntegration Context
Gestiona las conexiones con plataformas externas (Buildium, AppFolio, Hostify).

---

## 5. Modelo de Dominio

### 5.1 PropertyManagement Context

#### Property (Aggregate Root)

```csharp
public class Property : AggregateRoot<PropertyId>
{
    public PropertyId Id { get; private set; }
    public OwnerId OwnerId { get; private set; }
    public Address Address { get; private set; }
    public PropertyType Type { get; private set; }
    public PropertyStatus Status { get; private set; }
    public PropertyConfiguration Configuration { get; private set; }
    
    private readonly List<Unit> _units = new();
    public IReadOnlyCollection<Unit> Units => _units.AsReadOnly();
    
    // Factory method
    public static Property Create(
        OwnerId ownerId,
        Address address,
        PropertyType type,
        PropertyConfiguration configuration)
    {
        var property = new Property
        {
            Id = PropertyId.New(),
            OwnerId = ownerId,
            Address = address,
            Type = type,
            Status = PropertyStatus.Active,
            Configuration = configuration
        };
        
        property.AddDomainEvent(new PropertyCreatedEvent(property.Id));
        return property;
    }
    
    public Result AddUnit(string unitNumber)
    {
        if (_units.Any(u => u.UnitNumber == unitNumber))
            return Result.Failure("Unit number already exists");
            
        var unit = Unit.Create(Id, unitNumber);
        _units.Add(unit);
        
        AddDomainEvent(new UnitAddedToPropertyEvent(Id, unit.Id));
        return Result.Success();
    }
}
```

#### Guest (Aggregate Root)

```csharp
public class Guest : AggregateRoot<GuestId>
{
    public GuestId Id { get; private set; }
    public PersonalInfo PersonalInfo { get; private set; }
    public ContactInfo ContactInfo { get; private set; }
    public GuestStatus Status { get; private set; }
    public DateTime RegistrationDate { get; private set; }
    
    private readonly List<Lease> _leases = new();
    public IReadOnlyCollection<Lease> Leases => _leases.AsReadOnly();
    
    public bool CanRequestServiceFor(PropertyId propertyId, UnitId unitId)
    {
        return _leases.Any(l => 
            l.PropertyId == propertyId && 
            l.UnitId == unitId && 
            l.IsActive);
    }
}
```

#### Value Objects

```csharp
public record PropertyId(Guid Value)
{
    public static PropertyId New() => new(Guid.NewGuid());
}

public record UnitId(Guid Value)
{
    public static UnitId New() => new(Guid.NewGuid());
}

public record GuestId(Guid Value)
{
    public static GuestId New() => new(Guid.NewGuid());
}

public record OwnerId(Guid Value)
{
    public static OwnerId New() => new(Guid.NewGuid());
}

public record Address(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country
);

public record PersonalInfo(
    string FirstName,
    string LastName,
    DateTime? DateOfBirth
);

public record ContactInfo(
    string Email,
    string PhoneNumber,
    string? PreferredLanguage
);

public record PropertyConfiguration(
    bool AllowsServiceRequests,
    TimeSpan MaxServiceResponseTime,
    List<ServiceType> AllowedServices
);
```

#### Enums

```csharp
public enum PropertyType
{
    SingleFamily,
    Apartment,
    Condo,
    Townhouse,
    ShortTermRental
}

public enum PropertyStatus
{
    Active,
    UnderMaintenance,
    Inactive
}

public enum UnitStatus
{
    Vacant,
    Occupied,
    UnderMaintenance
}

public enum GuestStatus
{
    Active,
    Inactive,
    Blocked
}
```

---

### 5.2 ServiceRequest Context (Core Domain)

#### ServiceRequest (Aggregate Root)

```csharp
public class ServiceRequest : AggregateRoot<ServiceRequestId>
{
    public ServiceRequestId Id { get; private set; }
    public PropertyId PropertyId { get; private set; }
    public UnitId UnitId { get; private set; }
    public GuestId RequestedById { get; private set; }
    public ServiceType ServiceType { get; private set; }
    public ServicePriority Priority { get; private set; }
    public ServiceRequestStatus Status { get; private set; }
    public ProblemDescription Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    // Referencia externa (Buildium, AppFolio, etc.)
    public ExternalWorkOrderReference? ExternalReference { get; private set; }
    
    // Info del contractor asignado (viene del webhook)
    public ExternalContractorInfo? AssignedContractor { get; private set; }
    
    private readonly List<ServiceRequestMessage> _messages = new();
    public IReadOnlyCollection<ServiceRequestMessage> Messages => _messages.AsReadOnly();
    
    private readonly List<ServiceRequestAttachment> _attachments = new();
    public IReadOnlyCollection<ServiceRequestAttachment> Attachments => _attachments.AsReadOnly();
    
    private readonly List<ServiceRequestStatusChange> _statusHistory = new();
    public IReadOnlyCollection<ServiceRequestStatusChange> StatusHistory => _statusHistory.AsReadOnly();
    
    // Factory method
    public static ServiceRequest Create(
        PropertyId propertyId,
        UnitId unitId,
        GuestId requestedById,
        ServiceType serviceType,
        ServicePriority priority,
        ProblemDescription description)
    {
        var request = new ServiceRequest
        {
            Id = ServiceRequestId.New(),
            PropertyId = propertyId,
            UnitId = unitId,
            RequestedById = requestedById,
            ServiceType = serviceType,
            Priority = priority,
            Status = ServiceRequestStatus.Pending,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
        
        request.AddStatusChange(ServiceRequestStatus.Pending, "Request created");
        request.AddDomainEvent(new ServiceRequestCreatedEvent(request));
        
        return request;
    }
    
    // Método para vincular con work order externa
    public Result LinkToExternalWorkOrder(
        PlatformType platform,
        string externalId,
        string externalUrl)
    {
        if (ExternalReference != null)
            return Result.Failure("Already linked to external work order");
            
        ExternalReference = new ExternalWorkOrderReference(
            platform,
            externalId,
            externalUrl
        );
        
        AddDomainEvent(new ServiceRequestLinkedToExternalEvent(Id, ExternalReference));
        return Result.Success();
    }
    
    // Método principal para actualizar desde webhook
    public Result UpdateFromWebhook(WebhookPayload payload)
    {
        var newStatus = MapExternalStatus(payload.ExternalStatus, payload.Platform);
        
        if (newStatus != Status)
        {
            ChangeStatus(newStatus, $"Updated from {payload.Platform} webhook");
        }
        
        // Actualizar info del contractor si viene en el payload
        if (payload.ContractorInfo != null)
        {
            AssignedContractor = new ExternalContractorInfo(
                payload.ContractorInfo.Name,
                payload.ContractorInfo.Phone,
                payload.ContractorInfo.Email,
                payload.ContractorInfo.ScheduledFor
            );
        }
        
        // Si se completó, registrar fecha
        if (newStatus == ServiceRequestStatus.Completed && CompletedAt == null)
        {
            CompletedAt = DateTime.UtcNow;
        }
        
        AddDomainEvent(new ServiceRequestUpdatedFromWebhookEvent(Id, payload));
        return Result.Success();
    }
    
    public Result AddMessage(string content, MessageAuthor author, MessageType type)
    {
        var message = ServiceRequestMessage.Create(Id, content, author, type);
        _messages.Add(message);
        
        AddDomainEvent(new MessageAddedToServiceRequestEvent(Id, message.Id));
        return Result.Success();
    }
    
    public Result AddAttachment(string fileName, string fileUrl, AttachmentType type, GuestId uploadedBy)
    {
        var attachment = ServiceRequestAttachment.Create(Id, fileName, fileUrl, type, uploadedBy);
        _attachments.Add(attachment);
        
        return Result.Success();
    }
    
    public Result Cancel(string reason)
    {
        if (Status == ServiceRequestStatus.Completed)
            return Result.Failure("Cannot cancel a completed request");
            
        if (Status == ServiceRequestStatus.Cancelled)
            return Result.Failure("Request is already cancelled");
            
        ChangeStatus(ServiceRequestStatus.Cancelled, reason);
        AddDomainEvent(new ServiceRequestCancelledEvent(Id, reason));
        
        return Result.Success();
    }
    
    private void ChangeStatus(ServiceRequestStatus newStatus, string? reason = null)
    {
        var previousStatus = Status;
        Status = newStatus;
        AddStatusChange(newStatus, reason);
        
        AddDomainEvent(new ServiceRequestStatusChangedEvent(Id, previousStatus, newStatus));
    }
    
    private void AddStatusChange(ServiceRequestStatus status, string? reason)
    {
        _statusHistory.Add(new ServiceRequestStatusChange(
            status,
            DateTime.UtcNow,
            reason
        ));
    }
    
    private static ServiceRequestStatus MapExternalStatus(string externalStatus, PlatformType platform)
    {
        return platform switch
        {
            PlatformType.Buildium => MapBuildiumStatus(externalStatus),
            PlatformType.AppFolio => MapAppFolioStatus(externalStatus),
            PlatformType.Hostify => MapHostifyStatus(externalStatus),
            _ => ServiceRequestStatus.Pending
        };
    }
    
    private static ServiceRequestStatus MapBuildiumStatus(string status)
    {
        return status.ToLower() switch
        {
            "new" => ServiceRequestStatus.Pending,
            "assigned" => ServiceRequestStatus.Assigned,
            "in_progress" => ServiceRequestStatus.InProgress,
            "completed" => ServiceRequestStatus.Completed,
            "cancelled" => ServiceRequestStatus.Cancelled,
            _ => ServiceRequestStatus.Pending
        };
    }
    
    private static ServiceRequestStatus MapAppFolioStatus(string status)
    {
        return status.ToLower() switch
        {
            "open" => ServiceRequestStatus.Pending,
            "scheduled" => ServiceRequestStatus.Assigned,
            "in_progress" => ServiceRequestStatus.InProgress,
            "done" => ServiceRequestStatus.Completed,
            "closed" => ServiceRequestStatus.Completed,
            "void" => ServiceRequestStatus.Cancelled,
            _ => ServiceRequestStatus.Pending
        };
    }
    
    private static ServiceRequestStatus MapHostifyStatus(string status)
    {
        return status.ToLower() switch
        {
            "pending" => ServiceRequestStatus.Pending,
            "confirmed" => ServiceRequestStatus.Assigned,
            "in_progress" => ServiceRequestStatus.InProgress,
            "completed" => ServiceRequestStatus.Completed,
            "cancelled" => ServiceRequestStatus.Cancelled,
            _ => ServiceRequestStatus.Pending
        };
    }
}
```

#### Value Objects

```csharp
public record ServiceRequestId(Guid Value)
{
    public static ServiceRequestId New() => new(Guid.NewGuid());
}

public record ProblemDescription(
    string Summary,
    string DetailedDescription,
    string? Location,
    List<string> Tags
);

public record ExternalWorkOrderReference(
    PlatformType Platform,
    string ExternalId,
    string ExternalUrl
);

public record ExternalContractorInfo(
    string Name,
    string? Phone,
    string? Email,
    DateTime? ScheduledFor
);

public record MessageAuthor(
    string AuthorId,
    string AuthorName,
    AuthorType AuthorType
);

public record ServiceRequestStatusChange(
    ServiceRequestStatus Status,
    DateTime ChangedAt,
    string? Reason
);
```

#### Enums

```csharp
public enum ServiceType
{
    Plumbing,
    Electrical,
    HVAC,
    Appliances,
    Carpentry,
    Painting,
    Cleaning,
    Locksmith,
    PestControl,
    Landscaping,
    GeneralHandyman,
    Other
}

public enum ServicePriority
{
    Low,
    Normal,
    High,
    Emergency
}

public enum ServiceRequestStatus
{
    Pending,        // Creada, esperando envío a plataforma
    Submitted,      // Enviada a plataforma externa
    Assigned,       // Contractor asignado por la plataforma
    Scheduled,      // Visita programada
    InProgress,     // Contractor trabajando
    Completed,      // Trabajo terminado
    Cancelled,      // Cancelada
    RequiresFollowUp // Necesita trabajo adicional
}

public enum MessageType
{
    GuestMessage,
    AIResponse,
    SystemNotification,
    StatusUpdate
}

public enum AuthorType
{
    Guest,
    AIAssistant,
    System,
    PropertyManager
}

public enum AttachmentType
{
    Image,
    Video,
    Document,
    Audio
}
```

#### Domain Events

```csharp
public record ServiceRequestCreatedEvent(
    ServiceRequest ServiceRequest
) : IDomainEvent;

public record ServiceRequestLinkedToExternalEvent(
    ServiceRequestId ServiceRequestId,
    ExternalWorkOrderReference ExternalReference
) : IDomainEvent;

public record ServiceRequestUpdatedFromWebhookEvent(
    ServiceRequestId ServiceRequestId,
    WebhookPayload Payload
) : IDomainEvent;

public record ServiceRequestStatusChangedEvent(
    ServiceRequestId ServiceRequestId,
    ServiceRequestStatus PreviousStatus,
    ServiceRequestStatus NewStatus
) : IDomainEvent;

public record ServiceRequestCancelledEvent(
    ServiceRequestId ServiceRequestId,
    string Reason
) : IDomainEvent;

public record MessageAddedToServiceRequestEvent(
    ServiceRequestId ServiceRequestId,
    ServiceRequestMessageId MessageId
) : IDomainEvent;
```

---

### 5.3 AIAssistant Context

#### Conversation (Aggregate Root)

```csharp
public class Conversation : AggregateRoot<ConversationId>
{
    public ConversationId Id { get; private set; }
    public GuestId GuestId { get; private set; }
    public PropertyId PropertyId { get; private set; }
    public UnitId? UnitId { get; private set; }
    public ConversationType Type { get; private set; }
    public ConversationStatus Status { get; private set; }
    public ServiceRequestId? RelatedServiceRequestId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public ConversationContext Context { get; private set; }
    
    private readonly List<ConversationMessage> _messages = new();
    public IReadOnlyCollection<ConversationMessage> Messages => _messages.AsReadOnly();
    
    public static Conversation Start(
        GuestId guestId,
        PropertyId propertyId,
        UnitId? unitId = null)
    {
        return new Conversation
        {
            Id = ConversationId.New(),
            GuestId = guestId,
            PropertyId = propertyId,
            UnitId = unitId,
            Type = ConversationType.ServiceRequest,
            Status = ConversationStatus.Active,
            StartedAt = DateTime.UtcNow,
            Context = ConversationContext.Empty()
        };
    }
    
    public Result AddUserMessage(string content, List<AttachmentId>? attachments = null)
    {
        var message = ConversationMessage.CreateUserMessage(content, attachments);
        _messages.Add(message);
        
        AddDomainEvent(new UserMessageAddedEvent(Id, message.Id, content));
        return Result.Success();
    }
    
    public Result AddAIResponse(string content, AIIntent intent)
    {
        var message = ConversationMessage.CreateAIMessage(content, intent);
        _messages.Add(message);
        
        AddDomainEvent(new AIResponseGeneratedEvent(Id, message.Id, intent));
        return Result.Success();
    }
    
    public Result LinkToServiceRequest(ServiceRequestId serviceRequestId)
    {
        if (RelatedServiceRequestId != null)
            return Result.Failure("Already linked to a service request");
            
        RelatedServiceRequestId = serviceRequestId;
        Type = ConversationType.ServiceRequest;
        
        return Result.Success();
    }
    
    public Result UpdateContext(ConversationContext newContext)
    {
        Context = newContext;
        return Result.Success();
    }
    
    public Result Close()
    {
        if (Status == ConversationStatus.Closed)
            return Result.Failure("Conversation is already closed");
            
        Status = ConversationStatus.Closed;
        EndedAt = DateTime.UtcNow;
        
        return Result.Success();
    }
}
```

#### Value Objects

```csharp
public record ConversationId(Guid Value)
{
    public static ConversationId New() => new(Guid.NewGuid());
}

public record ConversationMessageId(Guid Value)
{
    public static ConversationMessageId New() => new(Guid.NewGuid());
}

public record ConversationContext(
    string? CurrentTopic,
    ServiceType? IdentifiedServiceType,
    ServicePriority? IdentifiedPriority,
    Dictionary<string, string> ExtractedEntities,
    List<string> PendingQuestions
)
{
    public static ConversationContext Empty() => new(
        null,
        null,
        null,
        new Dictionary<string, string>(),
        new List<string>()
    );
}

public record AIIntent(
    IntentType Type,
    decimal Confidence,
    Dictionary<string, object> Parameters
);
```

#### Enums

```csharp
public enum ConversationType
{
    ServiceRequest,
    GeneralInquiry,
    StatusCheck,
    Complaint,
    Feedback
}

public enum ConversationStatus
{
    Active,
    WaitingForUser,
    WaitingForAction,
    Closed
}

public enum IntentType
{
    RequestService,
    ProvideInformation,
    AskQuestion,
    ConfirmAction,
    CancelRequest,
    CheckStatus,
    ProvideFeedback,
    Greeting,
    Unknown
}
```

---

### 5.4 WebhookProcessing Context

#### IncomingWebhook (Aggregate Root)

```csharp
public class IncomingWebhook : AggregateRoot<WebhookId>
{
    public WebhookId Id { get; private set; }
    public PlatformType SourcePlatform { get; private set; }
    public string EventType { get; private set; }
    public string RawPayload { get; private set; }
    public string? Signature { get; private set; }
    public WebhookProcessingStatus Status { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    
    // Referencia al ServiceRequest afectado
    public ServiceRequestId? AffectedServiceRequestId { get; private set; }
    
    public static IncomingWebhook Create(
        PlatformType platform,
        string eventType,
        string rawPayload,
        string? signature)
    {
        return new IncomingWebhook
        {
            Id = WebhookId.New(),
            SourcePlatform = platform,
            EventType = eventType,
            RawPayload = rawPayload,
            Signature = signature,
            Status = WebhookProcessingStatus.Received,
            ReceivedAt = DateTime.UtcNow,
            RetryCount = 0
        };
    }
    
    public Result StartProcessing()
    {
        if (Status != WebhookProcessingStatus.Received && 
            Status != WebhookProcessingStatus.Retrying)
            return Result.Failure("Invalid status for processing");
            
        Status = WebhookProcessingStatus.Processing;
        return Result.Success();
    }
    
    public Result MarkAsProcessed(ServiceRequestId? affectedServiceRequestId = null)
    {
        Status = WebhookProcessingStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        AffectedServiceRequestId = affectedServiceRequestId;
        
        AddDomainEvent(new WebhookProcessedEvent(Id, SourcePlatform, EventType));
        return Result.Success();
    }
    
    public Result MarkAsFailed(string error)
    {
        Status = WebhookProcessingStatus.Failed;
        ErrorMessage = error;
        
        AddDomainEvent(new WebhookFailedEvent(Id, error));
        return Result.Success();
    }
    
    public Result ScheduleRetry()
    {
        if (RetryCount >= 3)
        {
            Status = WebhookProcessingStatus.Failed;
            ErrorMessage = "Max retries exceeded";
            return Result.Failure("Max retries exceeded");
        }
        
        Status = WebhookProcessingStatus.Retrying;
        RetryCount++;
        
        return Result.Success();
    }
}
```

#### Value Objects

```csharp
public record WebhookId(Guid Value)
{
    public static WebhookId New() => new(Guid.NewGuid());
}

public record WebhookPayload(
    PlatformType Platform,
    string EventType,
    string ExternalWorkOrderId,
    string ExternalStatus,
    ContractorInfoPayload? ContractorInfo,
    Dictionary<string, object> AdditionalData
);

public record ContractorInfoPayload(
    string Name,
    string? Phone,
    string? Email,
    DateTime? ScheduledFor,
    string? Notes
);
```

#### Enums

```csharp
public enum WebhookProcessingStatus
{
    Received,
    Processing,
    Processed,
    Failed,
    Retrying
}
```

---

### 5.5 PlatformIntegration Context

#### PlatformIntegration (Aggregate Root)

```csharp
public class PlatformIntegration : AggregateRoot<IntegrationId>
{
    public IntegrationId Id { get; private set; }
    public OwnerId OwnerId { get; private set; }
    public PlatformType Platform { get; private set; }
    public string DisplayName { get; private set; }
    public IntegrationStatus Status { get; private set; }
    public ConnectionCredentials Credentials { get; private set; }
    public IntegrationConfiguration Configuration { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastSyncError { get; private set; }
    
    private readonly List<PropertyMapping> _propertyMappings = new();
    public IReadOnlyCollection<PropertyMapping> PropertyMappings => _propertyMappings.AsReadOnly();
    
    public static PlatformIntegration Create(
        OwnerId ownerId,
        PlatformType platform,
        string displayName,
        ConnectionCredentials credentials)
    {
        return new PlatformIntegration
        {
            Id = IntegrationId.New(),
            OwnerId = ownerId,
            Platform = platform,
            DisplayName = displayName,
            Status = IntegrationStatus.PendingValidation,
            Credentials = credentials,
            Configuration = IntegrationConfiguration.Default()
        };
    }
    
    public Result Activate()
    {
        if (Status == IntegrationStatus.Active)
            return Result.Failure("Integration is already active");
            
        Status = IntegrationStatus.Active;
        AddDomainEvent(new IntegrationActivatedEvent(Id, Platform));
        
        return Result.Success();
    }
    
    public Result Deactivate(string reason)
    {
        Status = IntegrationStatus.Inactive;
        AddDomainEvent(new IntegrationDeactivatedEvent(Id, reason));
        
        return Result.Success();
    }
    
    public Result RecordSuccessfulSync()
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncError = null;
        
        return Result.Success();
    }
    
    public Result RecordSyncError(string error)
    {
        LastSyncError = error;
        
        if (ConsecutiveErrors() > 5)
        {
            Status = IntegrationStatus.Error;
        }
        
        return Result.Success();
    }
    
    public Result AddPropertyMapping(PropertyId propertyId, string externalPropertyId)
    {
        if (_propertyMappings.Any(m => m.PropertyId == propertyId))
            return Result.Failure("Property already mapped");
            
        _propertyMappings.Add(new PropertyMapping(propertyId, externalPropertyId));
        return Result.Success();
    }
    
    public string? GetExternalPropertyId(PropertyId propertyId)
    {
        return _propertyMappings
            .FirstOrDefault(m => m.PropertyId == propertyId)?
            .ExternalPropertyId;
    }
    
    private int ConsecutiveErrors() => 0; // Implementation needed
}
```

#### Value Objects

```csharp
public record IntegrationId(Guid Value)
{
    public static IntegrationId New() => new(Guid.NewGuid());
}

public record ConnectionCredentials(
    string ApiKey,
    string? ApiSecret,
    string? AccessToken,
    string? RefreshToken,
    DateTime? TokenExpiresAt
)
{
    public bool IsTokenExpired => TokenExpiresAt.HasValue && TokenExpiresAt < DateTime.UtcNow;
}

public record IntegrationConfiguration(
    bool AutoCreateWorkOrders,
    bool AutoAssignVendors,
    int SyncIntervalMinutes,
    List<ServiceType> SupportedServiceTypes,
    Dictionary<string, string> CustomSettings
)
{
    public static IntegrationConfiguration Default() => new(
        true,
        true,
        60,
        Enum.GetValues<ServiceType>().ToList(),
        new Dictionary<string, string>()
    );
}

public record PropertyMapping(
    PropertyId PropertyId,
    string ExternalPropertyId
);
```

#### Enums

```csharp
public enum PlatformType
{
    Buildium,
    AppFolio,
    Hostify,
    Yardi,
    RentManager,
    Custom
}

public enum IntegrationStatus
{
    PendingValidation,
    Active,
    Inactive,
    Error,
    Suspended
}
```

---

## 6. Flujos Principales

### 6.1 Flujo de Solicitud de Servicio

```
TIEMPO    COMPONENTE              ACCIÓN
──────    ──────────              ──────
T+0       Guest                   Escribe: "Tengo una fuga en el baño"
          │
          ▼
T+1       GuestApp                Envía mensaje via WebSocket
          │
          ▼
T+2       API Gateway             Recibe mensaje, autentica, enruta
          │
          ▼
T+3       AIAssistantService      ProcessUserMessageCommand
          │
          ├──► ConversationRepo   Obtiene/crea conversación
          │
          ▼
T+4       Claude API              Analiza intent
          │                       Response: {
          │                         intent: "RequestService",
          │                         serviceType: "Plumbing",
          │                         priority: "High",
          │                         confidence: 0.95
          │                       }
          │
          ▼
T+5       AIAssistantService      Detecta que necesita más info
          │
          ▼
T+6       Guest                   Recibe: "¿Puedes enviar una foto?"
          │
          ▼
T+7       Guest                   Envía foto
          │
          ▼
T+8       Claude API              Analiza imagen, confirma severidad
          │
          ▼
T+9       ServiceRequestService   CreateServiceRequestCommand
          │                       → Crea ServiceRequest (Status: Pending)
          │
          ▼
T+10      IntegrationService      SubmitWorkOrderCommand
          │
          ├──► Determina plataforma del Owner
          │
          ▼
T+11      Buildium API            POST /workorders
          │                       → Crea work order en Buildium
          │
          ▼
T+12      ServiceRequestService   Actualiza ExternalReference
          │                       → Status: Submitted
          │
          ▼
T+13      Guest                   Recibe: "Tu solicitud ha sido enviada. 
          │                       Te notificaremos cuando se asigne un técnico."
          │
          ▼
          ... (Tiempo pasa, Buildium asigna contractor) ...
          │
          ▼
T+N       Buildium                Envía webhook: workorder.assigned
          │
          ▼
T+N+1     WebhookController       Recibe webhook
          │
          ▼
T+N+2     WebhookProcessingService Valida firma, parsea payload
          │
          ▼
T+N+3     ServiceRequestService   UpdateFromWebhookCommand
          │                       → Status: Assigned
          │                       → AssignedContractor: { name, phone, scheduledFor }
          │
          ▼
T+N+4     NotificationService     Notifica al Guest
          │
          ▼
T+N+5     Guest                   Recibe: "Se asignó a Juan Plomero.
                                  Llegará mañana a las 10am."
```

### 6.2 Diagrama de Secuencia

```
┌───────┐     ┌─────────┐     ┌──────────┐     ┌────────────┐     ┌──────────┐     ┌──────────┐
│ Guest │     │   API   │     │    AI    │     │  Service   │     │Integration│    │ Buildium │
│  App  │     │ Gateway │     │ Assistant│     │  Request   │     │  Service  │    │   API    │
└───┬───┘     └────┬────┘     └────┬─────┘     └─────┬──────┘     └─────┬─────┘    └────┬─────┘
    │              │               │                 │                  │               │
    │ "Fuga en baño"               │                 │                  │               │
    │──────────────►               │                 │                  │               │
    │              │               │                 │                  │               │
    │              │ ProcessMessage│                 │                  │               │
    │              │───────────────►                 │                  │               │
    │              │               │                 │                  │               │
    │              │               │ Analyze Intent  │                  │               │
    │              │               │────────────────►│                  │               │
    │              │               │                 │                  │               │
    │              │◄──────────────│                 │                  │               │
    │◄─────────────│ "¿Puedes enviar foto?"          │                  │               │
    │              │               │                 │                  │               │
    │ [Envía foto] │               │                 │                  │               │
    │──────────────►               │                 │                  │               │
    │              │───────────────►                 │                  │               │
    │              │               │                 │                  │               │
    │              │               │ CreateServiceRequest               │               │
    │              │               │─────────────────►                  │               │
    │              │               │                 │                  │               │
    │              │               │                 │ SubmitWorkOrder  │               │
    │              │               │                 │──────────────────►               │
    │              │               │                 │                  │               │
    │              │               │                 │                  │ POST /workorders
    │              │               │                 │                  │───────────────►
    │              │               │                 │                  │               │
    │              │               │                 │                  │◄──────────────│
    │              │               │                 │◄─────────────────│ workOrderId   │
    │              │               │◄────────────────│                  │               │
    │◄─────────────│◄──────────────│ "Solicitud enviada"               │               │
    │              │               │                 │                  │               │
    │              │               │                 │                  │               │
    │   ═══════════════════════════════════════════════════════════════════════════    │
    │              │               │    WEBHOOK: workorder.assigned    │               │
    │   ═══════════════════════════════════════════════════════════════════════════    │
    │              │               │                 │                  │               │
    │              │               │                 │                  │◄──────────────│
    │              │               │                 │◄─────────────────│ Webhook       │
    │              │               │                 │                  │               │
    │              │               │                 │ Update Status    │               │
    │              │               │                 │ (Assigned)       │               │
    │              │               │                 │                  │               │
    │◄─────────────│◄──────────────│◄────────────────│ "Juan Plomero asignado"         │
    │              │               │                 │                  │               │
```

### 6.3 Estado del ServiceRequest

```
                                    ┌─────────────────┐
                                    │     START       │
                                    └────────┬────────┘
                                             │
                                             │ Guest crea solicitud
                                             ▼
                              ┌──────────────────────────────┐
                              │          PENDING             │
                              │  (Esperando envío a PMS)     │
                              └──────────────┬───────────────┘
                                             │
                                             │ Enviada a Buildium/AppFolio
                                             ▼
                              ┌──────────────────────────────┐
                              │         SUBMITTED            │
                              │  (En plataforma externa)     │
                              └──────────────┬───────────────┘
                                             │
                         ┌───────────────────┼───────────────────┐
                         │                   │                   │
                   Webhook:            Webhook:            Webhook:
                   assigned            cancelled           error
                         │                   │                   │
                         ▼                   │                   │
          ┌──────────────────────────────┐   │                   │
          │         ASSIGNED             │   │                   │
          │  (Contractor asignado por    │   │                   │
          │   la plataforma)             │   │                   │
          └──────────────┬───────────────┘   │                   │
                         │                   │                   │
                   Webhook:                  │                   │
                   in_progress               │                   │
                         │                   │                   │
                         ▼                   │                   │
          ┌──────────────────────────────┐   │                   │
          │        IN_PROGRESS           │   │                   │
          │  (Contractor trabajando)     │   │                   │
          └──────────────┬───────────────┘   │                   │
                         │                   │                   │
              ┌──────────┴──────────┐        │                   │
              │                     │        │                   │
        Webhook:              Webhook:       │                   │
        completed          requires_followup │                   │
              │                     │        │                   │
              ▼                     ▼        ▼                   ▼
┌─────────────────────────┐  ┌────────────────────┐  ┌────────────────────┐
│       COMPLETED         │  │ REQUIRES_FOLLOW_UP │  │     CANCELLED      │
│  (Trabajo terminado)    │  │ (Trabajo adicional)│  │                    │
└─────────────────────────┘  └────────────────────┘  └────────────────────┘
```

---

## 7. Integración con Plataformas Externas

### 7.1 Arquitectura de Integración

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         INTEGRATION ARCHITECTURE                                 │
└─────────────────────────────────────────────────────────────────────────────────┘

                         ┌────────────────────────────┐
                         │   IntegrationService       │
                         │   (Facade Pattern)         │
                         └─────────────┬──────────────┘
                                       │
                                       │ IExternalPlatformClient
                                       │
              ┌────────────────────────┼────────────────────────┐
              │                        │                        │
              ▼                        ▼                        ▼
┌─────────────────────────┐ ┌─────────────────────────┐ ┌─────────────────────────┐
│   BuildiumClient        │ │   AppFolioClient        │ │   HostifyClient         │
│                         │ │                         │ │                         │
│ ┌─────────────────────┐ │ │ ┌─────────────────────┐ │ │ ┌─────────────────────┐ │
│ │ Authentication      │ │ │ │ Authentication      │ │ │ │ Authentication      │ │
│ │ - API Key           │ │ │ │ - OAuth 2.0         │ │ │ │ - API Key + Secret  │ │
│ └─────────────────────┘ │ │ └─────────────────────┘ │ │ └─────────────────────┘ │
│                         │ │                         │ │                         │
│ ┌─────────────────────┐ │ │ ┌─────────────────────┐ │ │ ┌─────────────────────┐ │
│ │ Endpoints           │ │ │ │ Endpoints           │ │ │ │ Endpoints           │ │
│ │ - POST /workorders  │ │ │ │ - POST /work-orders │ │ │ │ - POST /tasks       │ │
│ │ - GET /workorders   │ │ │ │ - GET /work-orders  │ │ │ │ - GET /tasks        │ │
│ │ - GET /vendors      │ │ │ │ - GET /vendors      │ │ │ │ - GET /vendors      │ │
│ └─────────────────────┘ │ │ └─────────────────────┘ │ │ └─────────────────────┘ │
│                         │ │                         │ │                         │
│ ┌─────────────────────┐ │ │ ┌─────────────────────┐ │ │ ┌─────────────────────┐ │
│ │ Mapper              │ │ │ │ Mapper              │ │ │ │ Mapper              │ │
│ │ ServiceRequest →    │ │ │ │ ServiceRequest →    │ │ │ │ ServiceRequest →    │ │
│ │ BuildiumWorkOrder   │ │ │ │ AppFolioWorkOrder   │ │ │ │ HostifyTask         │ │
│ └─────────────────────┘ │ │ └─────────────────────┘ │ │ └─────────────────────┘ │
└─────────────────────────┘ └─────────────────────────┘ └─────────────────────────┘
              │                        │                        │
              └────────────────────────┼────────────────────────┘
                                       │
                                       ▼
                    ┌──────────────────────────────────┐
                    │      Resilience Layer (Polly)    │
                    │                                  │
                    │  • Retry (3 attempts, exp backoff)│
                    │  • Circuit Breaker (5 failures)  │
                    │  • Timeout (30 seconds)          │
                    │  • Rate Limiting                 │
                    └──────────────────────────────────┘
```

### 7.2 Interface del Cliente

```csharp
public interface IExternalPlatformClient
{
    PlatformType Platform { get; }
    
    Task<Result<string>> CreateWorkOrderAsync(
        CreateWorkOrderRequest request,
        CancellationToken cancellationToken = default);
    
    Task<Result<ExternalWorkOrder>> GetWorkOrderAsync(
        string externalId,
        CancellationToken cancellationToken = default);
    
    Task<Result> CancelWorkOrderAsync(
        string externalId,
        string reason,
        CancellationToken cancellationToken = default);
    
    Task<Result<List<ExternalVendor>>> GetVendorsAsync(
        ServiceType? serviceType = null,
        CancellationToken cancellationToken = default);
    
    Task<bool> ValidateCredentialsAsync(
        CancellationToken cancellationToken = default);
}

public record CreateWorkOrderRequest(
    string ExternalPropertyId,
    string Title,
    string Description,
    ServiceType ServiceType,
    ServicePriority Priority,
    List<string>? AttachmentUrls,
    Dictionary<string, string>? CustomFields
);

public record ExternalWorkOrder(
    string ExternalId,
    string Status,
    string? AssignedVendorId,
    string? AssignedVendorName,
    DateTime? ScheduledFor,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record ExternalVendor(
    string ExternalId,
    string Name,
    string? Phone,
    string? Email,
    List<ServiceType> Services
);
```

### 7.3 Implementación Buildium

```csharp
public class BuildiumClient : IExternalPlatformClient
{
    private readonly HttpClient _httpClient;
    private readonly BuildiumConfiguration _config;
    private readonly ILogger<BuildiumClient> _logger;
    
    public PlatformType Platform => PlatformType.Buildium;
    
    public BuildiumClient(
        HttpClient httpClient,
        IOptions<BuildiumConfiguration> config,
        ILogger<BuildiumClient> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-buildium-client-id", _config.ClientId);
        _httpClient.DefaultRequestHeaders.Add("x-buildium-client-secret", _config.ClientSecret);
    }
    
    public async Task<Result<string>> CreateWorkOrderAsync(
        CreateWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var buildiumRequest = MapToBuilidumRequest(request);
            
            var response = await _httpClient.PostAsJsonAsync(
                "/v1/workorders",
                buildiumRequest,
                cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Buildium API error: {Error}", error);
                return Result.Failure<string>($"Buildium API error: {response.StatusCode}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<BuildiumWorkOrderResponse>(
                cancellationToken: cancellationToken);
            
            return Result.Success(result!.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating work order in Buildium");
            return Result.Failure<string>(ex.Message);
        }
    }
    
    private BuildiumCreateWorkOrderRequest MapToBuilidumRequest(CreateWorkOrderRequest request)
    {
        return new BuildiumCreateWorkOrderRequest
        {
            PropertyId = int.Parse(request.ExternalPropertyId),
            Title = request.Title,
            Description = request.Description,
            Priority = MapPriority(request.Priority),
            TaskCategory = MapServiceType(request.ServiceType),
            EntryAllowed = "Yes",
            AssignedToUserId = null // Buildium asigna automáticamente
        };
    }
    
    private string MapPriority(ServicePriority priority) => priority switch
    {
        ServicePriority.Emergency => "High",
        ServicePriority.High => "High",
        ServicePriority.Normal => "Normal",
        ServicePriority.Low => "Low",
        _ => "Normal"
    };
    
    private string MapServiceType(ServiceType type) => type switch
    {
        ServiceType.Plumbing => "Plumbing",
        ServiceType.Electrical => "Electrical",
        ServiceType.HVAC => "HVAC",
        ServiceType.Appliances => "Appliances",
        _ => "General"
    };
}
```

---

## 8. Webhooks

### 8.1 Endpoints de Webhook

```csharp
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhooksController> _logger;
    
    [HttpPost("buildium")]
    public async Task<IActionResult> HandleBuildiumWebhook(
        [FromHeader(Name = "X-Buildium-Signature")] string signature,
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received Buildium webhook");
        
        var command = new ProcessWebhookCommand(
            PlatformType.Buildium,
            payload.GetRawText(),
            signature
        );
        
        var result = await _mediator.Send(command, cancellationToken);
        
        // Siempre retornar 200 para evitar reintentos innecesarios
        // Los errores se manejan internamente con retry queue
        return Ok(new { received = true });
    }
    
    [HttpPost("appfolio")]
    public async Task<IActionResult> HandleAppFolioWebhook(
        [FromHeader(Name = "X-AppFolio-Signature")] string signature,
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received AppFolio webhook");
        
        var command = new ProcessWebhookCommand(
            PlatformType.AppFolio,
            payload.GetRawText(),
            signature
        );
        
        await _mediator.Send(command, cancellationToken);
        
        return Ok(new { received = true });
    }
    
    [HttpPost("hostify")]
    public async Task<IActionResult> HandleHostifyWebhook(
        [FromHeader(Name = "X-Hostify-Signature")] string signature,
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received Hostify webhook");
        
        var command = new ProcessWebhookCommand(
            PlatformType.Hostify,
            payload.GetRawText(),
            signature
        );
        
        await _mediator.Send(command, cancellationToken);
        
        return Ok(new { received = true });
    }
}
```

### 8.2 Procesamiento de Webhooks

```csharp
public class ProcessWebhookCommandHandler : IRequestHandler<ProcessWebhookCommand, Result>
{
    private readonly IWebhookRepository _webhookRepository;
    private readonly IServiceRequestRepository _serviceRequestRepository;
    private readonly IWebhookSignatureValidator _signatureValidator;
    private readonly IWebhookPayloadParser _payloadParser;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessWebhookCommandHandler> _logger;
    
    public async Task<Result> Handle(
        ProcessWebhookCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Crear registro del webhook
        var webhook = IncomingWebhook.Create(
            request.Platform,
            "unknown", // Se actualiza después del parsing
            request.RawPayload,
            request.Signature
        );
        
        await _webhookRepository.AddAsync(webhook, cancellationToken);
        
        try
        {
            // 2. Validar firma
            if (!await _signatureValidator.ValidateAsync(request.Platform, request.RawPayload, request.Signature))
            {
                webhook.MarkAsFailed("Invalid signature");
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Failure("Invalid webhook signature");
            }
            
            // 3. Parsear payload
            var payloadResult = _payloadParser.Parse(request.Platform, request.RawPayload);
            if (payloadResult.IsFailure)
            {
                webhook.MarkAsFailed($"Parse error: {payloadResult.Error}");
                await _unitOfWork.CommitAsync(cancellationToken);
                return payloadResult;
            }
            
            var payload = payloadResult.Value;
            
            // 4. Buscar ServiceRequest relacionado
            var serviceRequest = await _serviceRequestRepository
                .GetByExternalIdAsync(payload.ExternalWorkOrderId, request.Platform, cancellationToken);
            
            if (serviceRequest == null)
            {
                _logger.LogWarning(
                    "ServiceRequest not found for external ID {ExternalId}", 
                    payload.ExternalWorkOrderId);
                    
                webhook.MarkAsProcessed();
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(); // No es error, puede ser work order no creada por nosotros
            }
            
            // 5. Actualizar ServiceRequest
            var previousStatus = serviceRequest.Status;
            var updateResult = serviceRequest.UpdateFromWebhook(payload);
            
            if (updateResult.IsFailure)
            {
                webhook.MarkAsFailed(updateResult.Error);
                await _unitOfWork.CommitAsync(cancellationToken);
                return updateResult;
            }
            
            // 6. Marcar webhook como procesado
            webhook.MarkAsProcessed(serviceRequest.Id);
            
            await _unitOfWork.CommitAsync(cancellationToken);
            
            // 7. Notificar al Guest si hubo cambio de estado
            if (previousStatus != serviceRequest.Status)
            {
                await _notificationService.NotifyStatusChangeAsync(
                    serviceRequest.Id,
                    serviceRequest.RequestedById,
                    previousStatus,
                    serviceRequest.Status,
                    serviceRequest.AssignedContractor,
                    cancellationToken);
            }
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            webhook.MarkAsFailed(ex.Message);
            await _unitOfWork.CommitAsync(cancellationToken);
            throw;
        }
    }
}
```

### 8.3 Eventos de Webhook por Plataforma

| Plataforma | Evento | Acción en Sistema |
|------------|--------|-------------------|
| **Buildium** | `workorder.created` | Log (work order fue creada por nosotros) |
| **Buildium** | `workorder.assigned` | Status → Assigned, guardar info contractor |
| **Buildium** | `workorder.scheduled` | Status → Scheduled, guardar fecha |
| **Buildium** | `workorder.in_progress` | Status → InProgress |
| **Buildium** | `workorder.completed` | Status → Completed, solicitar rating |
| **Buildium** | `workorder.cancelled` | Status → Cancelled |
| **AppFolio** | `work_order.status_changed` | Mapear status y actualizar |
| **AppFolio** | `work_order.vendor_assigned` | Guardar info contractor |
| **Hostify** | `task.updated` | Mapear status y actualizar |
| **Hostify** | `task.completed` | Status → Completed |

---

## 9. API Endpoints

### 9.1 Conversations API

```
POST   /api/conversations                    # Iniciar conversación
GET    /api/conversations/{id}               # Obtener conversación
POST   /api/conversations/{id}/messages      # Enviar mensaje
GET    /api/conversations/{id}/messages      # Obtener mensajes
POST   /api/conversations/{id}/close         # Cerrar conversación
```

### 9.2 Service Requests API

```
POST   /api/service-requests                 # Crear solicitud (interno, desde AI)
GET    /api/service-requests/{id}            # Obtener solicitud
GET    /api/service-requests                 # Listar solicitudes (con filtros)
POST   /api/service-requests/{id}/cancel     # Cancelar solicitud
GET    /api/service-requests/{id}/messages   # Obtener mensajes de la solicitud
POST   /api/service-requests/{id}/messages   # Agregar mensaje
GET    /api/service-requests/{id}/history    # Obtener historial de estados
```

### 9.3 Properties API

```
GET    /api/properties                       # Listar propiedades del owner
GET    /api/properties/{id}                  # Obtener propiedad
POST   /api/properties                       # Crear propiedad
PUT    /api/properties/{id}                  # Actualizar propiedad
GET    /api/properties/{id}/units            # Obtener unidades
POST   /api/properties/{id}/units            # Agregar unidad
```

### 9.4 Integrations API

```
GET    /api/integrations                     # Listar integraciones del owner
POST   /api/integrations                     # Crear integración
GET    /api/integrations/{id}                # Obtener integración
PUT    /api/integrations/{id}                # Actualizar integración
DELETE /api/integrations/{id}                # Eliminar integración
POST   /api/integrations/{id}/validate       # Validar credenciales
POST   /api/integrations/{id}/sync           # Forzar sincronización
GET    /api/integrations/{id}/mappings       # Obtener mapeos de propiedades
POST   /api/integrations/{id}/mappings       # Crear mapeo de propiedad
```

### 9.5 Webhooks API

```
POST   /api/webhooks/buildium                # Webhook de Buildium
POST   /api/webhooks/appfolio                # Webhook de AppFolio
POST   /api/webhooks/hostify                 # Webhook de Hostify
```

---

## 10. Estructura del Proyecto

```
src/
├── DoorX.Domain/
│   ├── PropertyManagement/
│   │   ├── Aggregates/
│   │   │   ├── Property.cs
│   │   │   ├── Guest.cs
│   │   │   └── Owner.cs
│   │   ├── Entities/
│   │   │   ├── Unit.cs
│   │   │   └── Lease.cs
│   │   ├── ValueObjects/
│   │   │   ├── PropertyId.cs
│   │   │   ├── Address.cs
│   │   │   └── ...
│   │   ├── Enums/
│   │   └── Events/
│   │
│   ├── ServiceRequests/
│   │   ├── Aggregates/
│   │   │   └── ServiceRequest.cs
│   │   ├── Entities/
│   │   │   ├── ServiceRequestMessage.cs
│   │   │   └── ServiceRequestAttachment.cs
│   │   ├── ValueObjects/
│   │   │   ├── ServiceRequestId.cs
│   │   │   ├── ProblemDescription.cs
│   │   │   ├── ExternalWorkOrderReference.cs
│   │   │   └── ExternalContractorInfo.cs
│   │   ├── Enums/
│   │   │   ├── ServiceType.cs
│   │   │   ├── ServicePriority.cs
│   │   │   └── ServiceRequestStatus.cs
│   │   └── Events/
│   │       ├── ServiceRequestCreatedEvent.cs
│   │       ├── ServiceRequestStatusChangedEvent.cs
│   │       └── ...
│   │
│   ├── AIAssistant/
│   │   ├── Aggregates/
│   │   │   └── Conversation.cs
│   │   ├── Entities/
│   │   │   └── ConversationMessage.cs
│   │   ├── ValueObjects/
│   │   │   ├── ConversationId.cs
│   │   │   ├── ConversationContext.cs
│   │   │   └── AIIntent.cs
│   │   ├── Enums/
│   │   └── Events/
│   │
│   ├── WebhookProcessing/
│   │   ├── Aggregates/
│   │   │   └── IncomingWebhook.cs
│   │   ├── ValueObjects/
│   │   │   ├── WebhookId.cs
│   │   │   └── WebhookPayload.cs
│   │   ├── Enums/
│   │   └── Events/
│   │
│   ├── PlatformIntegration/
│   │   ├── Aggregates/
│   │   │   └── PlatformIntegration.cs
│   │   ├── ValueObjects/
│   │   │   ├── IntegrationId.cs
│   │   │   ├── ConnectionCredentials.cs
│   │   │   └── PropertyMapping.cs
│   │   ├── Enums/
│   │   └── Events/
│   │
│   └── Common/
│       ├── AggregateRoot.cs
│       ├── Entity.cs
│       ├── ValueObject.cs
│       ├── IDomainEvent.cs
│       └── Result.cs
│
├── DoorX.Application/
│   ├── ServiceRequests/
│   │   ├── Commands/
│   │   │   ├── CreateServiceRequest/
│   │   │   │   ├── CreateServiceRequestCommand.cs
│   │   │   │   ├── CreateServiceRequestCommandHandler.cs
│   │   │   │   └── CreateServiceRequestCommandValidator.cs
│   │   │   ├── CancelServiceRequest/
│   │   │   └── UpdateFromWebhook/
│   │   ├── Queries/
│   │   │   ├── GetServiceRequest/
│   │   │   └── GetServiceRequestsByGuest/
│   │   └── EventHandlers/
│   │
│   ├── Conversations/
│   │   ├── Commands/
│   │   │   ├── StartConversation/
│   │   │   ├── ProcessUserMessage/
│   │   │   └── CloseConversation/
│   │   └── Queries/
│   │
│   ├── Webhooks/
│   │   ├── Commands/
│   │   │   └── ProcessWebhook/
│   │   └── Services/
│   │       ├── IWebhookSignatureValidator.cs
│   │       └── IWebhookPayloadParser.cs
│   │
│   ├── Integrations/
│   │   ├── Commands/
│   │   │   ├── CreateIntegration/
│   │   │   ├── SubmitWorkOrder/
│   │   │   └── SyncIntegration/
│   │   └── Queries/
│   │
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   └── IExternalPlatformClient.cs
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs
│   │   │   └── LoggingBehavior.cs
│   │   └── Mappings/
│   │
│   └── DependencyInjection.cs
│
├── DoorX.Infrastructure/
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── ServiceRequestConfiguration.cs
│   │   │   ├── ConversationConfiguration.cs
│   │   │   └── ...
│   │   ├── Repositories/
│   │   │   ├── ServiceRequestRepository.cs
│   │   │   ├── ConversationRepository.cs
│   │   │   └── ...
│   │   └── Migrations/
│   │
│   ├── ExternalPlatforms/
│   │   ├── Buildium/
│   │   │   ├── BuildiumClient.cs
│   │   │   ├── BuildiumConfiguration.cs
│   │   │   ├── BuildiumWebhookParser.cs
│   │   │   └── BuildiumSignatureValidator.cs
│   │   ├── AppFolio/
│   │   │   └── ...
│   │   ├── Hostify/
│   │   │   └── ...
│   │   └── Common/
│   │       ├── ExternalPlatformClientFactory.cs
│   │       └── ResiliencePolicies.cs
│   │
│   ├── AI/
│   │   ├── ClaudeClient.cs
│   │   ├── IntentRecognitionService.cs
│   │   └── ResponseGenerationService.cs
│   │
│   ├── Notifications/
│   │   ├── NotificationService.cs
│   │   ├── PushNotificationProvider.cs
│   │   └── EmailNotificationProvider.cs
│   │
│   ├── Caching/
│   │   └── RedisCacheService.cs
│   │
│   └── DependencyInjection.cs
│
├── DoorX.API/
│   ├── Controllers/
│   │   ├── ConversationsController.cs
│   │   ├── ServiceRequestsController.cs
│   │   ├── PropertiesController.cs
│   │   ├── IntegrationsController.cs
│   │   └── WebhooksController.cs
│   │
│   ├── Hubs/
│   │   └── ConversationHub.cs
│   │
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   │
│   ├── Filters/
│   │   └── ValidationFilter.cs
│   │
│   └── Program.cs
│
└── tests/
    ├── DoorX.Domain.Tests/
    ├── DoorX.Application.Tests/
    ├── DoorX.Infrastructure.Tests/
    └── DoorX.API.Tests/
```

---

## Notas de Implementación

### Manejo de Transacciones
- Usar Unit of Work pattern para commits atómicos
- Domain Events se despachan después del commit exitoso

### Resiliencia
- Polly para retry/circuit breaker en llamadas a APIs externas
- Webhooks se procesan con idempotencia (verificar si ya fue procesado)
- Cola de retry para webhooks fallidos

### Seguridad
- Validación de firmas en todos los webhooks
- Credenciales encriptadas en base de datos
- Rate limiting en endpoints públicos

### Observabilidad
- Structured logging con Serilog
- Métricas con Application Insights
- Tracing distribuido para debugging

### Multi-tenancy
- Filtrado por OwnerId en todas las queries
- Aislamiento de datos entre tenants
- Configuración por tenant para integraciones

---

**Documento generado para DoorX - Service Management System**  
**Arquitectura: Clean Architecture + DDD**  
**Stack: .NET Core 8, SQL Server, Redis, SignalR**
