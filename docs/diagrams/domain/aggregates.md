# DoorX - Aggregates Design

## Descripción

Diagrama de los principales Aggregate Roots del sistema DoorX y sus entidades internas.

---

## Main Aggregates

```mermaid
graph TB
    subgraph WorkOrderAggregate ["WorkOrder Aggregate"]
        WO[WorkOrder<br/>Aggregate Root]
        VB1[VendorBid]
        VB2[VendorBid]
        VB3[VendorBid]

        WO --> VB1
        WO --> VB2
        WO --> VB3
    end

    subgraph VendorAggregate ["Vendor Aggregate"]
        V[Vendor<br/>Aggregate Root]
        SO[Service Offerings]
        SA[Service Areas]
        CERT[Certifications]

        V --> SO
        V --> SA
        V --> CERT
    end

    subgraph PropertyAggregate ["Property Aggregate"]
        P[Property<br/>Aggregate Root]
        UNIT[Units]
        FEAT[Features]

        P --> UNIT
        P --> FEAT
    end

    subgraph TenantAggregate ["Tenant Aggregate"]
        T[Tenant<br/>Aggregate Root]
        CI[Contact Info]
        PREF[Preferences]

        T --> CI
        T --> PREF
    end

    subgraph ConversationAggregate ["Conversation Aggregate"]
        C[Conversation<br/>Aggregate Root]
        M1[Message]
        M2[Message]
        M3[Message]
        CTX[Context]

        C --> M1
        C --> M2
        C --> M3
        C --> CTX
    end

    %% Cross-aggregate references (IDs only)
    WO -.->|TenantId| T
    WO -.->|PropertyId| P
    WO -.->|AssignedVendorId| V
    VB1 -.->|VendorId| V
    C -.->|TenantId| T

    classDef aggregateRoot fill:#f4a742,stroke:#c87e1a,color:#000000
    classDef entity fill:#438dd5,stroke:#2e6295,color:#ffffff
    classDef reference stroke-dasharray: 5 5

    class WO,V,P,T,C aggregateRoot
    class VB1,VB2,VB3,SO,SA,CERT,UNIT,FEAT,CI,PREF,M1,M2,M3,CTX entity
```

---

## Aggregate Details

### WorkOrder Aggregate

```mermaid
classDiagram
    class WorkOrder {
        +WorkOrderId Id
        +TenantId TenantId
        +PropertyId PropertyId
        +ServiceCategory Category
        +Priority Priority
        +WorkOrderStatus Status
        +List~VendorBid~ Bids
        +Create() WorkOrder
        +AddBid() ErrorOr
        +AssignVendor() ErrorOr
        +Complete() ErrorOr
    }

    class VendorBid {
        +VendorBidId Id
        +VendorId VendorId
        +Money Amount
        +BidStatus Status
    }

    WorkOrder "1" *-- "0..5" VendorBid
```

**Invariants:**
- Maximum 5 bids per work order
- Cannot add bids after vendor assigned
- Status transitions must be valid

---

## Referencias

- [DDD Aggregates](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [Effective Aggregate Design](https://www.dddcommunity.org/library/vernon_2011/)
