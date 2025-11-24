# DoorX - Estrategia de Catálogos y Persistencia

> Documento que define qué catálogos se manejan en memoria vs base de datos

## 📋 Tabla de Contenidos

1. [Criterios de Decisión](#criterios-de-decisión)
2. [Catálogos Solo en Memoria](#catálogos-solo-en-memoria)
3. [Catálogos con Persistencia Opcional](#catálogos-con-persistencia-opcional)
4. [Implementación con Smart Enums](#implementación-con-smart-enums)
5. [Almacenamiento en Base de Datos](#almacenamiento-en-base-de-datos)
6. [Migración y Versionado](#migración-y-versionado)

---

## 🎯 Criterios de Decisión

### ¿Cuándo usar SOLO MEMORIA (Smart Enum)?

✅ El catálogo es **fijo y definido por el negocio**
✅ Los valores **NO cambian** frecuentemente
✅ **NO requiere** que usuarios finales agreguen valores
✅ Los cambios se hacen mediante **deploy de código**
✅ Son parte del **modelo de dominio** y su lógica

**Ventajas:**
- 🚀 Mejor rendimiento (sin queries a BD)
- 🔒 Type-safety en tiempo de compilación
- 📝 IntelliSense en el IDE
- 🧪 Fácil de testear
- 🔄 No requiere migraciones de datos

### ¿Cuándo usar PERSISTENCIA (Tabla en BD)?

✅ El catálogo **puede crecer** con el tiempo
✅ Usuarios **administradores pueden agregar** valores
✅ Requiere **auditoría** de cambios
✅ Tiene **metadatos extensos** que cambian
✅ Necesita **internacionalización dinámica**
✅ Se integra con **sistemas externos** que definen valores

**Ventajas:**
- 🔄 Flexibilidad para agregar valores sin deploy
- 👥 Usuarios pueden administrar catálogos
- 📊 Auditoría de cambios
- 🌍 Internacionalización dinámica

---

## 🧠 Catálogos Solo en Memoria

Estos catálogos se implementan como **Smart Enums** y NO tienen tabla en la base de datos.

### 1. Priority (Prioridad de Work Order)

**Ubicación:** `src/Domain/WorkOrders/ValueObjects/Priority.cs`

**Decisión:** ❌ NO PERSISTIR

**Razones:**
- Son 4 niveles fijos definidos por el negocio (Emergency, High, Normal, Low)
- Los tiempos de respuesta esperados son parte de la lógica de negocio
- NO requiere que usuarios agreguen nuevas prioridades
- Cambios son raros y controlados por el equipo de producto

**Valores:**
```csharp
Emergency -> 24 hours
High      -> 48 hours
Normal    -> 120 hours (5 días)
Low       -> 168 hours (7 días)
```

**Almacenamiento en BD:**
- Como **INT** (Id del enum): `Priority = 1` (Emergency)
- O como **VARCHAR(50)**: `Priority = 'Emergency'`

---

### 2. WorkOrderStatus (Estado del Work Order)

**Ubicación:** `src/Domain/WorkOrders/ValueObjects/WorkOrderStatus.cs`

**Decisión:** ❌ NO PERSISTIR

**Razones:**
- El workflow es parte fundamental de la lógica de negocio
- Las transiciones válidas están codificadas en el dominio
- NO se pueden agregar estados arbitrariamente
- El flujo está vinculado a reglas de negocio complejas

**Valores:**
```csharp
1  -> Open
2  -> Categorized
3  -> VendorSearch
4  -> Bidding
5  -> Scheduled
6  -> InProgress
7  -> Completed
8  -> Closed
9  -> Cancelled
```

**Almacenamiento en BD:**
- Como **INT**: `Status = 5` (Scheduled)
- Recomendado: INT para mejor rendimiento en queries

---

### 3. Channel (Canal de Comunicación)

**Ubicación:** `src/Domain/Conversations/ValueObjects/Channel.cs`

**Decisión:** ❌ NO PERSISTIR

**Razones:**
- Son canales de integración técnica (SMS, WhatsApp, etc.)
- Agregar un nuevo canal requiere desarrollo de integración
- NO es configurable por usuarios
- Cambios implican código de infraestructura

**Valores:**
```csharp
1 -> SMS
2 -> WhatsApp
3 -> WebChat
4 -> Email
```

**Almacenamiento en BD:**
- Como **VARCHAR(50)**: `Channel = 'WhatsApp'`
- Recomendado: VARCHAR para claridad en queries

---

### 4. SenderType (Tipo de Emisor)

**Ubicación:** `src/Domain/Conversations/ValueObjects/SenderType.cs`

**Decisión:** ❌ NO PERSISTIR

**Razones:**
- Son los actores del sistema (Tenant, Vendor, AI, PropertyManager)
- Parte fundamental del modelo de dominio
- NO se agregan nuevos tipos de emisor frecuentemente
- Cada tipo tiene lógica de negocio asociada

**Valores:**
```csharp
1 -> Tenant
2 -> Vendor
3 -> AI
4 -> PropertyManager
```

**Almacenamiento en BD:**
- Como **VARCHAR(50)**: `SenderType = 'Tenant'`

---

### 5. Language (Idioma)

**Ubicación:** `src/Domain/Common/ValueObjects/Language.cs`

**Decisión:** ❌ NO PERSISTIR (para MVP)

**Razones:**
- Conjunto limitado de idiomas soportados inicialmente
- Agregar un idioma requiere traducciones del sistema
- NO es autoservicio para usuarios
- Crece muy lentamente

**Valores:**
```csharp
1 -> en (English)
2 -> es (Spanish)
3 -> fr (French)
4 -> pt (Portuguese)
```

**Futuro:** Si se requieren 20+ idiomas, considerar persistencia.

**Almacenamiento en BD:**
- Como **VARCHAR(5)**: `Language = 'es'`
- Código ISO 639-1

---

### 6. PropertyType (Tipo de Propiedad)

**Ubicación:** `src/Domain/Properties/Entities/Property.cs` (inner class)

**Decisión:** ❌ NO PERSISTIR

**Razones:**
- Conjunto estándar de tipos de propiedad
- NO requiere que property managers creen nuevos tipos
- Cambios son poco frecuentes

**Valores:**
```csharp
1 -> Apartment
2 -> House
3 -> Condo
4 -> Townhouse
5 -> CommercialBuilding
6 -> Other
```

**Almacenamiento en BD:**
- Como **VARCHAR(50)**: `PropertyType = 'Apartment'`

---

## 🗄️ Catálogos con Persistencia Opcional

### 7. ServiceCategory (Categoría de Servicio)

**Ubicación:** `src/Domain/WorkOrders/ValueObjects/ServiceCategory.cs`

**Decisión:** ⚠️ ANÁLISIS REQUERIDO

**Opción A: Solo Memoria (Recomendado para MVP)**
- ✅ Conjunto fijo de categorías comunes
- ✅ Más rápido y simple
- ❌ Requiere deploy para agregar categorías

**Opción B: Con Persistencia**
- ✅ Property managers pueden agregar categorías
- ✅ Soporte multi-tenant con categorías custom
- ❌ Más complejo
- ❌ Requiere UI de administración

**Valores Iniciales:**
```csharp
1  -> Plumbing
2  -> Electrical
3  -> HVAC
4  -> Appliance
5  -> PestControl
6  -> Cleaning
7  -> GeneralMaintenance
```

**Recomendación Inicial:**
- **MVP**: Smart Enum (memoria)
- **Futuro**: Migrar a tabla si se necesita customización por tenant

**Si se persiste:**
```sql
CREATE TABLE ServiceCategories (
    Id INT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    IsActive BIT DEFAULT 1,
    DisplayOrder INT,
    IconName VARCHAR(50),
    CreatedOnUtc DATETIME2 NOT NULL,
    ModifiedOnUtc DATETIME2
);
```

---

## 💻 Implementación con Smart Enums

### Estructura Base

```csharp
public class Priority : SmartEnum<Priority>
{
    // Valores estáticos predefinidos
    public static readonly Priority Emergency = new(1, "Emergency", 24);
    public static readonly Priority High = new(2, "High", 48);
    public static readonly Priority Normal = new(3, "Normal", 120);
    public static readonly Priority Low = new(4, "Low", 168);

    // Metadata adicional
    public int ExpectedResponseHours { get; }

    // Constructor privado
    private Priority(int id, string name, int expectedResponseHours)
        : base(id, name)
    {
        ExpectedResponseHours = expectedResponseHours;
    }

    // Métodos de negocio
    public bool IsEmergency() => this == Emergency;

    public TimeSpan GetExpectedResponseTime()
        => TimeSpan.FromHours(ExpectedResponseHours);
}
```

### Uso en Entidades

```csharp
public class WorkOrder : AggregateRoot<WorkOrderId>
{
    // Propiedad fuertemente tipada
    public Priority Priority { get; private set; }

    // Uso
    var workOrder = WorkOrder.Create(...);

    // ✅ Type-safe
    workOrder.Priority = Priority.Emergency;

    // ✅ IntelliSense
    if (workOrder.Priority.IsEmergency())
    {
        // enviar alerta
    }

    // ✅ Metadata disponible
    var hours = workOrder.Priority.ExpectedResponseHours;
}
```

### Conversión desde String/Int

```csharp
// Desde nombre
var priority = Priority.FromName("Emergency");

// Desde ID
var priority = Priority.FromId(1);

// Try pattern
if (Priority.TryFromName("High", out var priority))
{
    // usar priority
}

// Obtener todos
var allPriorities = Priority.GetAll();
```

---

## 🗃️ Almacenamiento en Base de Datos

### Entity Framework Configuration

```csharp
public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        // Opción 1: Almacenar como INT (más eficiente)
        builder.Property(w => w.Priority)
            .HasConversion(
                p => p.Id,                          // A la BD
                id => Priority.FromId(id)!          // Desde la BD
            )
            .HasColumnType("INT")
            .IsRequired();

        // Opción 2: Almacenar como STRING (más legible)
        builder.Property(w => w.Priority)
            .HasConversion(
                p => p.Name,                        // A la BD
                name => Priority.FromName(name)!    // Desde la BD
            )
            .HasColumnType("VARCHAR(50)")
            .IsRequired();

        // Opción 3: Almacenar ambos (óptimo para queries)
        builder.Property(w => w.Priority)
            .HasConversion(
                p => p.Id,
                id => Priority.FromId(id)!
            );

        builder.Property<string>("PriorityName")
            .HasConversion(
                _ => default!,
                _ => default!
            )
            .HasComputedColumnSql("CASE Priority WHEN 1 THEN 'Emergency' WHEN 2 THEN 'High' ... END", stored: true);
    }
}
```

### Recomendaciones de Almacenamiento

| Smart Enum | Tipo Recomendado | Razón |
|------------|------------------|-------|
| Priority | INT | Queries por rango, ordenamiento |
| WorkOrderStatus | INT | Queries frecuentes, índices |
| Channel | VARCHAR(50) | Claridad en queries |
| SenderType | VARCHAR(50) | Claridad en queries |
| Language | VARCHAR(5) | Estándar ISO |
| PropertyType | VARCHAR(50) | Pocos valores, claridad |
| ServiceCategory | INT o VARCHAR(100) | INT si es fijo, VARCHAR si puede crecer |

---

## 🔄 Migración y Versionado

### Agregar un Nuevo Valor

**Smart Enum (Memoria):**

```csharp
// ANTES
public class Priority : SmartEnum<Priority>
{
    public static readonly Priority Emergency = new(1, "Emergency", 24);
    public static readonly Priority High = new(2, "High", 48);
    public static readonly Priority Normal = new(3, "Normal", 120);
    public static readonly Priority Low = new(4, "Low", 168);
}

// DESPUÉS - Agregar nuevo valor
public class Priority : SmartEnum<Priority>
{
    public static readonly Priority Emergency = new(1, "Emergency", 24);
    public static readonly Priority Urgent = new(2, "Urgent", 36);      // ⬅️ NUEVO
    public static readonly Priority High = new(3, "High", 48);          // ⚠️ ID cambió
    public static readonly Priority Normal = new(4, "Normal", 120);
    public static readonly Priority Low = new(5, "Low", 168);
}
```

⚠️ **PROBLEMA**: Cambiar IDs rompe datos existentes

**Solución Correcta:**

```csharp
// Nunca cambiar IDs existentes, solo agregar al final
public class Priority : SmartEnum<Priority>
{
    public static readonly Priority Emergency = new(1, "Emergency", 24);
    public static readonly Priority High = new(2, "High", 48);
    public static readonly Priority Normal = new(3, "Normal", 120);
    public static readonly Priority Low = new(4, "Low", 168);
    public static readonly Priority Urgent = new(5, "Urgent", 36);      // ⬅️ NUEVO al final
}
```

### Deprecar un Valor

```csharp
public class Priority : SmartEnum<Priority>
{
    public static readonly Priority Emergency = new(1, "Emergency", 24);
    public static readonly Priority High = new(2, "High", 48);

    [Obsolete("Use High instead")]
    public static readonly Priority Medium = new(3, "Medium", 72);      // Deprecado

    public static readonly Priority Normal = new(4, "Normal", 120);
    public static readonly Priority Low = new(5, "Low", 168);

    // Método para verificar si está deprecado
    public bool IsDeprecated() => this == Medium;
}
```

### Migración de Datos

Si necesitas cambiar de Smart Enum (memoria) a Tabla (persistencia):

```sql
-- 1. Crear tabla de catálogo
CREATE TABLE ServiceCategories (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(100) NOT NULL UNIQUE,
    IsActive BIT DEFAULT 1
);

-- 2. Insertar valores existentes del Smart Enum
INSERT INTO ServiceCategories (Id, Name) VALUES
(1, 'Plumbing'),
(2, 'Electrical'),
(3, 'HVAC'),
(4, 'Appliance'),
(5, 'PestControl'),
(6, 'Cleaning'),
(7, 'GeneralMaintenance');

-- 3. Actualizar referencias (si es necesario)
-- Las referencias por ID se mantienen automáticamente
```

---

## 📊 Resumen de Decisiones

| Catálogo | Tipo | Almacenamiento BD | Razón |
|----------|------|-------------------|-------|
| **Priority** | Smart Enum | INT | Fijo, parte de lógica de negocio |
| **WorkOrderStatus** | Smart Enum | INT | Workflow fijo con transiciones |
| **Channel** | Smart Enum | VARCHAR(50) | Canales de integración técnica |
| **SenderType** | Smart Enum | VARCHAR(50) | Actores del sistema |
| **Language** | Smart Enum | VARCHAR(5) | Conjunto limitado (MVP) |
| **PropertyType** | Smart Enum | VARCHAR(50) | Tipos estándar de propiedad |
| **ServiceCategory** | Smart Enum (MVP) | INT | Evaluar persistencia en v2 |

---

## 🎯 Mejores Prácticas

### ✅ DO

- Usa Smart Enums para catálogos fijos del dominio
- Almacena como INT cuando necesites ordenamiento o rangos
- Almacena como VARCHAR cuando la claridad es importante
- NUNCA cambies IDs de valores existentes
- Agrega nuevos valores al final de la lista
- Documenta cambios en catálogos

### ❌ DON'T

- No uses tablas de catálogo para valores que nunca cambiarán
- No uses strings mágicos en el código
- No cambies IDs existentes (rompe datos)
- No uses enums de C# tradicionales (limitados)
- No almacenes metadata compleja en Smart Enums (usa tablas)

---

## 🔮 Evolución Futura

### Señales para Migrar a Persistencia

Si observas estos patrones, considera migrar a tabla:

1. ⚠️ Clientes solicitan agregar valores custom frecuentemente
2. ⚠️ Diferentes tenants necesitan valores diferentes
3. ⚠️ Se requiere metadata extensa (descripciones largas, traducciones, etc.)
4. ⚠️ Hay más de 20-30 valores
5. ⚠️ Se necesita auditoría de cambios en el catálogo

### Arquitectura Híbrida

Para catálogos que pueden crecer:

```csharp
public class ServiceCategory : SmartEnum<ServiceCategory>
{
    // Valores base (siempre disponibles)
    public static readonly ServiceCategory Plumbing = new(1, "Plumbing");
    public static readonly ServiceCategory Electrical = new(2, "Electrical");
    // ... otros valores base

    // Caché de valores custom desde BD
    private static readonly ConcurrentDictionary<int, ServiceCategory> _customCategories = new();

    // Factory que combina valores estáticos y dinámicos
    public static ServiceCategory? FromIdWithCustom(int id)
    {
        // Primero buscar en valores estáticos
        var staticValue = FromId(id);
        if (staticValue != null)
            return staticValue;

        // Luego buscar en custom
        return _customCategories.GetValueOrDefault(id);
    }
}
```

---

**Última actualización**: 2024-11-24
**Versión**: 1.0.0
