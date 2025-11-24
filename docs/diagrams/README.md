# DoorX - Diagrams Index

Esta carpeta contiene todos los diagramas de arquitectura, diseño y flujos del sistema DoorX.

## 📂 Estructura de Carpetas

### 📐 [C4 Model Diagrams](./c4/)
Diagramas jerárquicos del modelo C4 (Context, Containers, Components, Code):

- **[01-context-diagram.md](./c4/01-context-diagram.md)** - Nivel 1: Sistema en contexto con actores externos
- **[02-container-diagram.md](./c4/02-container-diagram.md)** - Nivel 2: Aplicaciones y servicios principales
- **[03-component-diagram.md](./c4/03-component-diagram.md)** - Nivel 3: Componentes internos de cada contenedor
- **[04-code-diagram.md](./c4/04-code-diagram.md)** - Nivel 4: Diagramas de clases (opcional)

### 🎯 [Domain Diagrams](./domain/)
Diagramas relacionados con Domain-Driven Design:

- **[bounded-contexts.md](./domain/bounded-contexts.md)** - Mapa de bounded contexts y sus relaciones
- **[aggregates.md](./domain/aggregates.md)** - Aggregates roots y sus entidades
- **[domain-events.md](./domain/domain-events.md)** - Domain events y sus handlers

### 🔄 [Flow Diagrams](./flows/)
Diagramas de flujos de negocio y procesos:

- **[work-order-lifecycle.md](./flows/work-order-lifecycle.md)** - Ciclo de vida completo de un Work Order
- **[vendor-bidding-process.md](./flows/vendor-bidding-process.md)** - Proceso de licitación de vendors
- **[ai-conversation-flow.md](./flows/ai-conversation-flow.md)** - Flujo de conversación con IA (Aimee)

### 🏗️ [Infrastructure Diagrams](./infrastructure/)
Diagramas técnicos de infraestructura:

- **[deployment-diagram.md](./infrastructure/deployment-diagram.md)** - Diagrama de deployment
- **[database-schema.md](./infrastructure/database-schema.md)** - Esquema de base de datos
- **[integration-architecture.md](./infrastructure/integration-architecture.md)** - Arquitectura de integraciones con PMS externos

### 🔀 [Sequence Diagrams](./sequences/)
Diagramas de secuencia para casos de uso específicos:

- **[create-work-order.md](./sequences/create-work-order.md)** - Secuencia: Crear work order
- **[assign-vendor.md](./sequences/assign-vendor.md)** - Secuencia: Asignar vendor
- **[complete-work-order.md](./sequences/complete-work-order.md)** - Secuencia: Completar work order

---

## 🎨 Formato de Diagramas

Todos los diagramas están escritos en **Mermaid**, un lenguaje de diagramación que se renderiza automáticamente en:
- GitHub
- GitLab
- Visual Studio Code (con extensión)
- Notion
- Confluence

### Ejemplo básico de sintaxis Mermaid:

\`\`\`mermaid
graph TD
    A[Tenant] --> B[DoorX System]
    B --> C[Vendor]
\`\`\`

Para más información sobre Mermaid: https://mermaid.js.org/

---

## 🔗 Documentación Relacionada

- [ARCHITECTURE.md](../ARCHITECTURE.md) - Descripción detallada de la arquitectura
- [DOMAIN_MODEL.md](../DOMAIN_MODEL.md) - Modelo de dominio completo
- [BUSINESS_RULES.md](../BUSINESS_RULES.md) - Reglas de negocio
- [UBIQUITOUS_LANGUAGE.md](../UBIQUITOUS_LANGUAGE.md) - Glosario del lenguaje ubicuo
- [CLAUDE.md](../../claude.md) - Guía de desarrollo para IA

---

## 📝 Guía de Contribución

Al agregar un nuevo diagrama:

1. Colócalo en la carpeta correspondiente según su tipo
2. Usa nombres descriptivos en kebab-case (ej: `vendor-matching-algorithm.md`)
3. Actualiza este README.md agregando un link al nuevo diagrama
4. Incluye un título y descripción clara en el archivo
5. Usa Mermaid como formato preferido
6. Agrega notas explicativas cuando sea necesario

---

Última actualización: 2025-11-24
