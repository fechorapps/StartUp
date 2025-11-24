# DoorX Ubiquitous Language (Simplified)

## 🎯 Purpose

DoorX es una plataforma de comunicación inteligente para mantenimiento que conecta inquilinos con proveedores de servicio mediante IA.

## 👥 Actores Principales

### Tenant

**Definition:** Persona que vive en una propiedad y necesita reportar problemas de mantenimiento

**Code:** `Tenant` class

**Solo necesitamos:**
- Nombre
- Teléfono/Email
- PropertyId (dónde vive)
- Preferred Language

### Vendor

**Definition:** Proveedor que realiza trabajos de mantenimiento

**Code:** `Vendor` class

**Solo necesitamos:**
- Nombre/Empresa
- Teléfono/Email
- Service Categories (qué servicios ofrece)
- Service Areas (ZIP codes donde trabaja)
- Rating
- Availability

### Property Manager

**Definition:** Quien aprueba trabajos y gestiona vendors

**Code:** `PropertyManager` class

**Solo necesitamos:**
- Nombre
- Email/Teléfono
- Properties que administra
- Approval Limits

## 🔧 Conceptos Core de Mantenimiento

### Work Order

**Definition:** Solicitud de mantenimiento creada por un tenant

**Code:** `WorkOrder` class

**Atributos esenciales:**

```csharp
public class WorkOrder
{
    public WorkOrderId Id { get; set; }
    public TenantId TenantId { get; set; }
    public PropertyId PropertyId { get; set; }
    public string IssueDescription { get; set; }
    public ServiceCategory Category { get; set; }
    public Priority Priority { get; set; }
    public WorkOrderStatus Status { get; set; }
    public VendorId? AssignedVendorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public List<Message> Messages { get; set; }
}
```

### Service Category

**Definition:** Tipo de servicio necesario

**Valores:**
- Plumbing
- Electrical
- HVAC
- Appliance
- Pest Control
- Cleaning
- General Maintenance

### Priority

**Definition:** Qué tan urgente es el problema

**Valores:**
- **Emergency** (< 24 horas) - Sin agua, sin electricidad, seguridad
- **High** (1-2 días) - Problemas importantes
- **Normal** (3-5 días) - Reparaciones estándar
- **Low** (5+ días) - Mejoras cosméticas

### Work Order Status

**Estados del flujo:**
- **Open** - Tenant reportó problema
- **Categorized** - IA identificó el tipo de problema
- **VendorSearch** - Buscando vendors disponibles
- **Bidding** - Esperando cotizaciones
- **Scheduled** - Vendor asignado, fecha confirmada
- **InProgress** - Vendor trabajando
- **Completed** - Trabajo terminado
- **Closed** - Tenant confirmó satisfacción

## 💬 Comunicación

### Conversation

**Definition:** Hilo de mensajes sobre un work order

**Participantes:** Tenant + Vendor + Aimee (IA)

**Code:** `Conversation` class

**Channels:** SMS, WhatsApp, WebChat

### Message

**Definition:** Un mensaje individual en la conversación

**Atributos:**

```csharp
public class Message
{
    public MessageId Id { get; set; }
    public string Content { get; set; }
    public SenderType Sender { get; set; } // Tenant/Vendor/AI
    public DateTime SentAt { get; set; }
    public Channel Channel { get; set; } // SMS/WhatsApp/Web
}
```

### Aimee (AI Assistant)

**Definition:** IA que orquesta la comunicación

**Responsabilidades:**
- Entender el problema del tenant
- Categorizar el tipo de servicio
- Buscar vendors apropiados
- Coordinar horarios
- Facilitar la comunicación
- Confirmar satisfacción

## 🔄 Flujo Simplificado

### 1. TENANT REPORTS
```
Tenant: "Mi aire acondicionado no funciona"
```

### 2. AI CATEGORIZES
```
Aimee: "Entiendo, problema de HVAC. ¿Hace ruido? ¿No enfría?"
```

### 3. FIND VENDORS
```
Aimee busca vendors de HVAC en el área
```

### 4. GET QUOTES
```
Aimee: "¿Puedes revisar AC en 123 Main St?"
Vendor: "Sí, $95 por visita, puedo ir mañana 2PM"
```

### 5. COORDINATE
```
Aimee → Tenant: "Vendor puede ir mañana 2PM, ¿funciona?"
Tenant: "Sí"
```

### 6. CONFIRM WORK
```
Vendor: "Trabajo completado, era el filtro"
```

### 7. CLOSE
```
Aimee → Tenant: "¿Todo funcionando bien?"
Tenant: "Sí, gracias"
```

## 🔌 Integraciones (Simplificado)

### PMS Integration

**Para qué:** Obtener datos básicos, NO gestionar finanzas

**Obtenemos:**
- Lista de properties
- Lista de tenants (nombre y contacto)
- Lista de vendors disponibles

**NO manejamos:**
- Pagos de renta
- Contratos/Leases
- Finanzas
- Reportes de propietarios

### External Work Order ID

**Definition:** ID del work order en el PMS externo

**Para:** Sincronización básica de estado

**Ejemplo:** "Buildium-WO-12345"

## ❌ Fuera de Alcance (NO manejamos)

- ❌ Rent (pagos de alquiler)
- ❌ Leases (contratos)
- ❌ Security Deposits
- ❌ Owner financials
- ❌ Accounting
- ❌ Tenant screening
- ❌ Rent collection
- ❌ Late fees
- ❌ Evictions
- ❌ Insurance claims

## 🎯 Modelo de Dominio Simplificado

```csharp
namespace DoorX.Domain
{
    // Solo 5 entidades principales
    public class WorkOrder { }     // El ticket de mantenimiento
    public class Tenant { }         // Quien reporta
    public class Vendor { }         // Quien arregla
    public class Property { }       // Dónde está el problema
    public class Conversation { }   // La comunicación

    // Value Objects mínimos
    public class ServiceCategory { }
    public class Priority { }
    public class WorkOrderStatus { }
    public class Message { }

    // NO necesitamos
    // X Lease
    // X Rent
    // X SecurityDeposit
    // X Owner
    // X Portfolio
    // X Payment
}
```
