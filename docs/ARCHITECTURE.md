# DoorX - Documentación de Arquitectura

> Guía completa de la arquitectura del proyecto para desarrolladores

## 📋 Tabla de Contenidos

1. [Información General](#información-general)
2. [Stack Tecnológico](#stack-tecnológico)
3. [Arquitectura del Sistema](#arquitectura-del-sistema)
4. [Estructura de Proyectos](#estructura-de-proyectos)
5. [Dependencias entre Capas](#dependencias-entre-capas)
6. [Sistema de Build Centralizado](#sistema-de-build-centralizado)
7. [Convenciones de Código](#convenciones-de-código)
8. [Comandos Útiles](#comandos-útiles)
9. [Próximos Pasos](#próximos-pasos)

---

## 📖 Información General

**DoorX** es un sistema SaaS de gestión inteligente de mantenimiento para propiedades que utiliza IA conversacional para automatizar el ciclo completo de solicitudes de mantenimiento.

### Problema que Resuelve

- Automatiza la gestión de solicitudes de mantenimiento en propiedades de alquiler
- Conecta inquilinos, propietarios, administradores y contratistas
- Utiliza un asistente virtual IA (Aimee) para coordinar todo el proceso
- Se integra con sistemas de gestión de propiedades externos (Buildium, Hostify, AppFolio)

---

## 🛠️ Stack Tecnológico

### Backend
- **.NET 8.0** (LTS) - Framework principal
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM
- **PostgreSQL** - Base de datos principal

### Frontend
- **Angular 18** - Framework SPA
- **Material UI** - Componentes UI
- **Tailwind CSS** - Estilos

### Integraciones
- **OpenAI GPT-4** - Asistente IA (Assistants API)
- **Twilio** - Mensajería (SMS/WhatsApp)
- **Buildium, Hostify, AppFolio** - Sistemas PMS (Property Management)

---

## 🏗️ Arquitectura del Sistema

El proyecto sigue los principios de **Clean Architecture** y **Domain-Driven Design (DDD)**.

### Capas de la Arquitectura

```
┌──────────────────────────────────────────┐
│           API Layer                      │
│  (Presentation)                          │
│  - Controllers                           │
│  - Middleware                            │
│  - Dependency Injection                  │
└──────────────────────────────────────────┘
              ↓              ↓
    ┌─────────────┐    ┌──────────────┐
    │ Application │    │Infrastructure│
    │   Layer     │    │    Layer     │
    ├─────────────┤    ├──────────────┤
    │ Use Cases   │    │ EF Core      │
    │ DTOs        │    │ OpenAI       │
    │ Interfaces  │    │ Twilio       │
    │ Validation  │    │ PMS APIs     │
    └─────────────┘    └──────────────┘
           ↓                  ↓
         ┌────────────────────────┐
         │    Domain Layer        │
         │  (Core Business)       │
         ├────────────────────────┤
         │ Entities               │
         │ Value Objects          │
         │ Domain Events          │
         │ Repository Interfaces  │
         │ Domain Services        │
         └────────────────────────┘
```

### Principios Aplicados

✅ **Inversión de Dependencias**: Las capas externas dependen de las internas
✅ **Separación de Responsabilidades**: Cada capa tiene un propósito específico
✅ **Independencia del Framework**: La lógica de negocio no depende de tecnologías externas
✅ **Testabilidad**: Facilita pruebas unitarias y de integración

---

## 📁 Estructura de Proyectos

```
DoorX/
├── Domain/                    # Capa de Dominio
│   ├── PropertyManagement/   # Contexto: Gestión de Propiedades
│   ├── ServiceRequest/        # Contexto: Solicitudes de Servicio
│   ├── ContractorManagement/  # Contexto: Gestión de Contratistas
│   ├── IntegrationPlatform/   # Contexto: Integraciones
│   ├── AIAssistant/           # Contexto: Asistente IA
│   └── Common/                # Código compartido
│
├── Application/               # Capa de Aplicación
│   ├── ServiceRequests/       # Casos de uso de ServiceRequest
│   ├── Properties/            # Casos de uso de Properties
│   ├── Contractors/           # Casos de uso de Contractors
│   ├── Conversations/         # Casos de uso de IA
│   └── Common/                # DTOs, Interfaces compartidas
│
├── Infrastructure/            # Capa de Infraestructura
│   ├── Persistence/           # Entity Framework, Repositorios
│   ├── Providers/             # PMS (Buildium, Hostify)
│   ├── AI/                    # Integración OpenAI
│   ├── Messaging/             # Twilio (SMS/WhatsApp)
│   └── Common/                # Configuración compartida
│
├── API/                       # API REST
│   ├── Controllers/           # Controladores HTTP
│   ├── Middleware/            # Middleware personalizado
│   ├── Filters/               # Filtros de acción
│   └── Extensions/            # Extension methods
│
├── docs/                      # Documentación
│   └── ARCHITECTURE.md        # Este archivo
│
├── Directory.Build.props      # Configuración global de build
├── Directory.Build.targets    # Targets personalizados de MSBuild
└── DoorX.sln                  # Archivo de solución
```

---

## 🔗 Dependencias entre Capas

### Diagrama de Dependencias

```
API
 ├── → Application
 └── → Infrastructure

Application
 └── → Domain

Infrastructure
 └── → Domain

Domain
 └── (sin dependencias)
```

### Reglas de Dependencias

| Proyecto       | Puede referenciar | NO puede referenciar |
|----------------|-------------------|----------------------|
| Domain         | Ninguno           | Todos                |
| Application    | Domain            | Infrastructure, API  |
| Infrastructure | Domain            | Application, API     |
| API            | All               | -                    |

### ¿Por qué esta estructura?

**Domain no depende de nadie**
- Es el núcleo del negocio
- Define interfaces (repository patterns)
- No conoce detalles de implementación

**Infrastructure implementa abstracciones de Domain**
- Implementa `IRepository<T>` definido en Domain
- Conoce tecnologías específicas (EF Core, PostgreSQL)
- No conoce casos de uso (Application)

**Application orquesta casos de uso**
- Usa abstracciones de Domain
- No conoce detalles de implementación
- Coordina el flujo de negocio

**API es el punto de entrada**
- Configura Dependency Injection
- Conecta todas las capas
- Maneja HTTP, autenticación, etc.

---

## ⚙️ Sistema de Build Centralizado

El proyecto utiliza `Directory.Build.props` y `Directory.Build.targets` para centralizar la configuración de build.

### Directory.Build.props

Ubicación: `/Directory.Build.props`

**Características principales:**

```xml
<!-- Namespace automático: DoorX.{NombreDelProyecto} -->
<RootNamespace>DoorX.$(MSBuildProjectName)</RootNamespace>
```

**Configuraciones incluidas:**
- ✅ Target Framework: .NET 8.0
- ✅ Nullable Reference Types habilitado
- ✅ Implicit Usings habilitado
- ✅ Versión del producto centralizada (1.0.0)
- ✅ Análisis de código .NET habilitado
- ✅ Generación de documentación XML

**Ventajas:**
- No duplicar configuración en cada `.csproj`
- Cambios globales en un solo lugar
- Consistencia entre todos los proyectos

### Directory.Build.targets

Ubicación: `/Directory.Build.targets`

**Targets personalizados:**

1. **ShowBuildInfo**: Muestra información durante la compilación
   ```
   Building: Domain
   Namespace: DoorX.Domain
   Framework: net8.0
   Configuration: Debug
   ```

2. **ValidateProjectStructure**: Valida la estructura antes del build

3. **CleanGenerated**: Limpia archivos generados automáticamente

4. **ValidateArchitectureDependencies**: Valida que se respeten las reglas de arquitectura

---

## 📐 Convenciones de Código

### Namespaces

**Proyectos físicos SIN prefijo:**
```
Domain/
Application/
Infrastructure/
API/
```

**Namespaces CON prefijo automático:**
```csharp
namespace DoorX.Domain;           // ← Automático
namespace DoorX.Application;      // ← Automático
namespace DoorX.Infrastructure;   // ← Automático
namespace DoorX.API;              // ← Automático
```

El namespace se configura automáticamente en `Directory.Build.props`:
```xml
<RootNamespace>DoorX.$(MSBuildProjectName)</RootNamespace>
```

### Organización de Carpetas por Bounded Context

Cada capa organiza el código por **Bounded Context** (DDD):

**Domain Layer:**
```
Domain/
├── PropertyManagement/    # Contexto 1
│   ├── Entities/
│   ├── ValueObjects/
│   └── Repositories/
├── ServiceRequest/        # Contexto 2
├── ContractorManagement/  # Contexto 3
├── IntegrationPlatform/   # Contexto 4
└── AIAssistant/          # Contexto 5
```

**Application Layer:**
```
Application/
├── ServiceRequests/
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── Handlers/
├── Properties/
├── Contractors/
└── Conversations/
```

---

## 🚀 Comandos Útiles

### Compilar y Ejecutar

```bash
# Compilar toda la solución
dotnet build DoorX.sln

# Compilar en Release
dotnet build DoorX.sln -c Release

# Ejecutar la API
dotnet run --project API

# Ejecutar con hot reload
dotnet watch run --project API

# Limpiar build artifacts
dotnet clean DoorX.sln
```

### Gestión de Proyectos

```bash
# Agregar proyecto a la solución
dotnet sln DoorX.sln add NuevoProyecto/NuevoProyecto.csproj

# Ver proyectos en la solución
dotnet sln DoorX.sln list

# Agregar referencia entre proyectos
dotnet add Application/Application.csproj reference Domain/Domain.csproj
```

### Gestión de Paquetes NuGet

```bash
# Agregar paquete
dotnet add Infrastructure/Infrastructure.csproj package Microsoft.EntityFrameworkCore

# Listar paquetes instalados
dotnet list Infrastructure/Infrastructure.csproj package

# Actualizar paquetes
dotnet restore
```

### Testing

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar tests con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Ejecutar tests de un proyecto específico
dotnet test tests/Domain.Tests/Domain.Tests.csproj
```

### Base de Datos (Entity Framework)

```bash
# Crear migración
dotnet ef migrations add NombreMigracion -p Infrastructure -s API

# Aplicar migraciones
dotnet ef database update -p Infrastructure -s API

# Ver migraciones pendientes
dotnet ef migrations list -p Infrastructure -s API

# Revertir última migración
dotnet ef migrations remove -p Infrastructure -s API
```

### Git

```bash
# Ver estado
git status

# Crear commit
git add .
git commit -m "Descripción del cambio"

# Push a branch
git push -u origin nombre-branch

# Ver historial
git log --oneline --graph
```

---

## 🎯 Próximos Pasos

### Fase 1: Estructura Base ✅ COMPLETADO
- [x] Crear solución .NET 8
- [x] Configurar build centralizado
- [x] Crear proyectos Clean Architecture
- [x] Establecer dependencias correctas

### Fase 2: Domain Layer (Siguiente)
- [ ] Crear bounded contexts
- [ ] Definir entidades principales
- [ ] Definir value objects
- [ ] Crear interfaces de repositorios
- [ ] Implementar domain events

### Fase 3: Application Layer
- [ ] Implementar casos de uso (CQRS)
- [ ] Crear DTOs
- [ ] Implementar validaciones (FluentValidation)
- [ ] Configurar AutoMapper

### Fase 4: Infrastructure Layer
- [ ] Configurar Entity Framework Core
- [ ] Implementar repositorios
- [ ] Crear migraciones de BD
- [ ] Integrar OpenAI
- [ ] Integrar Twilio
- [ ] Crear providers de PMS

### Fase 5: API Layer
- [ ] Crear controllers
- [ ] Configurar autenticación JWT
- [ ] Implementar middleware
- [ ] Configurar Swagger/OpenAPI
- [ ] Implementar logging

### Fase 6: Testing
- [ ] Tests unitarios (Domain)
- [ ] Tests de integración (Application)
- [ ] Tests de API (End-to-End)

### Fase 7: DevOps
- [ ] Docker containerization
- [ ] CI/CD pipeline
- [ ] Configuración de ambientes
- [ ] Monitoring y logging

---

## 📚 Referencias y Recursos

### Documentación Oficial
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

### Patrones y Prácticas
- Domain-Driven Design (DDD)
- CQRS (Command Query Responsibility Segregation)
- Repository Pattern
- Factory Pattern
- Dependency Injection

### Libros Recomendados
- "Clean Architecture" - Robert C. Martin
- "Domain-Driven Design" - Eric Evans
- "Implementing Domain-Driven Design" - Vaughn Vernon

---

## 👥 Contribuyendo al Proyecto

### Flujo de Trabajo

1. **Crear branch** desde `main`
   ```bash
   git checkout -b feature/nombre-feature
   ```

2. **Desarrollar** siguiendo las convenciones

3. **Compilar y testear** localmente
   ```bash
   dotnet build
   dotnet test
   ```

4. **Commit** con mensaje descriptivo
   ```bash
   git commit -m "feat: Agregar entidad ServiceRequest"
   ```

5. **Push** y crear Pull Request
   ```bash
   git push -u origin feature/nombre-feature
   ```

### Convenciones de Commits

```
feat: Nueva funcionalidad
fix: Corrección de bug
docs: Cambios en documentación
refactor: Refactorización de código
test: Agregar o modificar tests
chore: Tareas de mantenimiento
```

---

## 📞 Contacto y Soporte

Para preguntas sobre la arquitectura del proyecto:
- Revisar este documento primero
- Consultar el archivo `claude.md` en la raíz del proyecto
- Contactar al equipo de arquitectura

---

**Última actualización**: 2024-11-24
**Versión del documento**: 1.0.0
