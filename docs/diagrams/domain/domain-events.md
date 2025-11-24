# DoorX - Domain Events Flow

## Descripción

Diagrama de Domain Events y sus handlers en el sistema DoorX.

---

## Domain Events Flow

```mermaid
graph LR
    %% Events
    WOCreated[WorkOrderCreated<br/>Event]
    VendorAssigned[VendorAssigned<br/>Event]
    WOCompleted[WorkOrderCompleted<br/>Event]
    BidReceived[VendorBidReceived<br/>Event]

    %% Handlers
    NotifyPM[Notify Property Manager<br/>Handler]
    NotifyVendors[Notify Available Vendors<br/>Handler]
    SyncPMS[Sync to PMS<br/>Handler]
    SendSMS[Send SMS Notification<br/>Handler]
    UpdateMetrics[Update Metrics<br/>Handler]
    NotifyTenant[Notify Tenant<br/>Handler]

    %% Flow
    WOCreated --> NotifyPM
    WOCreated --> NotifyVendors
    WOCreated --> SyncPMS

    VendorAssigned --> SendSMS
    VendorAssigned --> SyncPMS
    VendorAssigned --> NotifyTenant

    WOCompleted --> NotifyTenant
    WOCompleted --> UpdateMetrics
    WOCompleted --> SyncPMS

    BidReceived --> NotifyPM
    BidReceived --> NotifyTenant

    classDef event fill:#f4a742,stroke:#c87e1a,color:#000000
    classDef handler fill:#438dd5,stroke:#2e6295,color:#ffffff

    class WOCreated,VendorAssigned,WOCompleted,BidReceived event
    class NotifyPM,NotifyVendors,SyncPMS,SendSMS,UpdateMetrics,NotifyTenant handler
```

---

## Event Catalog

### WorkOrderCreated
**Raised when:** A new work order is created
**Handlers:**
- Notify Property Manager
- Find and notify available vendors
- Sync to external PMS

### VendorAssigned
**Raised when:** A vendor is assigned to a work order
**Handlers:**
- Send SMS to vendor
- Notify tenant
- Update PMS

### WorkOrderCompleted
**Raised when:** Work is completed
**Handlers:**
- Notify tenant for confirmation
- Update analytics/metrics
- Close work order in PMS

---

## Referencias

- [Domain Events](https://martinfowler.com/eaaDev/DomainEvent.html)
- [DoorX Business Rules](../../BUSINESS_RULES.md)
