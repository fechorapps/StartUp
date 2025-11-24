# DoorX - Database Schema

## Descripción

Esquema relacional de la base de datos PostgreSQL.

---

## Entity Relationship Diagram

```mermaid
erDiagram
    Landlords ||--o{ Properties : owns
    Properties ||--o{ Tenants : hosts
    Properties ||--o{ WorkOrders : has
    Tenants ||--o{ WorkOrders : creates
    Tenants ||--o{ Conversations : initiates
    WorkOrders ||--o{ VendorBids : receives
    WorkOrders ||--o{ Messages : contains
    Vendors ||--o{ VendorBids : submits
    Vendors ||--o{ ServiceOfferings : offers
    Vendors ||--o{ ServiceAreas : covers
    WorkOrders }o--|| Vendors : assigned_to
    Conversations ||--o{ ConversationMessages : contains

    Landlords {
        uuid Id PK
        string Name
        string Email
        string PhoneNumber
        string DefaultPMS
        timestamp CreatedAt
        timestamp ModifiedAt
    }

    Properties {
        uuid Id PK
        uuid LandlordId FK
        string Address
        string City
        string State
        string ZipCode
        string PropertyType
        string PMSConfiguration
        string ExternalPropertyId
        timestamp CreatedAt
        timestamp ModifiedAt
    }

    Tenants {
        uuid Id PK
        uuid PropertyId FK
        string FirstName
        string LastName
        string Email
        string PhoneNumber
        string PreferredChannel
        string Language
        timestamp CreatedAt
        timestamp ModifiedAt
    }

    WorkOrders {
        uuid Id PK
        uuid TenantId FK
        uuid PropertyId FK
        uuid AssignedVendorId FK
        string IssueDescription
        string ServiceCategory
        string Priority
        string Status
        decimal FinalCost
        timestamp CreatedAt
        timestamp ModifiedAt
        timestamp CompletedAt
    }

    VendorBids {
        uuid Id PK
        uuid WorkOrderId FK
        uuid VendorId FK
        decimal BidAmount
        string Message
        string Status
        timestamp SubmittedAt
    }

    Vendors {
        uuid Id PK
        string CompanyName
        string ContactName
        string Email
        string PhoneNumber
        decimal Rating
        int CompletedJobs
        timestamp CreatedAt
        timestamp ModifiedAt
    }

    ServiceOfferings {
        uuid Id PK
        uuid VendorId FK
        string ServiceCategory
        bool IsActive
    }

    ServiceAreas {
        uuid Id PK
        uuid VendorId FK
        string ZipCode
        int RadiusMiles
    }

    Messages {
        uuid Id PK
        uuid WorkOrderId FK
        string SenderType
        string Content
        timestamp SentAt
    }

    Conversations {
        uuid Id PK
        uuid TenantId FK
        string Channel
        string Status
        json Context
        timestamp StartedAt
        timestamp LastMessageAt
    }

    ConversationMessages {
        uuid Id PK
        uuid ConversationId FK
        string Role
        string Content
        timestamp SentAt
    }
```

---

## Key Tables

### WorkOrders
Primary aggregate for maintenance requests
- Indexes: `TenantId`, `PropertyId`, `Status`, `CreatedAt`
- Full-text search: `IssueDescription`

### Vendors
Contractor information and ratings
- Indexes: `Rating`, `CompletedJobs`
- Composite: `(ServiceCategory, ZipCode)` for matching

### Conversations
AI chat sessions with context
- JSONB column for flexible context storage
- Index on `TenantId`, `Status`

---

## Referencias

- [Domain Model](../../DOMAIN_MODEL.md)
- [Entity Configurations](../../../src/Infrastructure/Persistence/Configurations/)
