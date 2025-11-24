# DoorX - Integration Architecture (PMS)

## Descripción

Arquitectura de integraciones con Property Management Systems externos usando patrón Factory + Adapter.

---

## Integration Pattern

```mermaid
graph TB
    subgraph DoorX["DoorX System"]
        WorkOrder[Work Order<br/>Aggregate]
        Factory[Provider Factory]

        Factory -->|creates| IProvider[ITicketSystemProvider<br/>Interface]
    end

    subgraph Adapters["Adapters Layer"]
        IProvider --> Buildium[Buildium Adapter]
        IProvider --> Hostify[Hostify Adapter]
        IProvider --> AppFolio[AppFolio Adapter]
    end

    subgraph External["External PMS APIs"]
        Buildium --> BuildiumAPI[Buildium API<br/>OAuth 2.0]
        Hostify --> HostifyAPI[Hostify API<br/>API Key]
        AppFolio --> AppFolioAPI[AppFolio API<br/>OAuth 2.0]
    end

    WorkOrder -->|PropertyId| Factory

    classDef internal fill:#438dd5,stroke:#2e6295,color:#ffffff
    classDef adapter fill:#f4a742,stroke:#c87e1a,color:#000000
    classDef external fill:#999999,stroke:#666666,color:#ffffff

    class WorkOrder,Factory,IProvider internal
    class Buildium,Hostify,AppFolio adapter
    class BuildiumAPI,HostifyAPI,AppFolioAPI external
```

---

## Factory Selection Logic

```csharp
public async Task<ITicketSystemProvider> GetProviderForPropertyAsync(PropertyId propertyId)
{
    // 1. Get property configuration
    var property = await _propertyRepository.GetByIdAsync(propertyId);

    // 2. Determine ERP type (cascade: Property → Client → Landlord)
    var erpType = property.PMSConfiguration?.ERPType
                  ?? property.Client?.PrimaryERP
                  ?? property.Landlord?.DefaultPMS
                  ?? ERPType.None;

    // 3. Return appropriate provider
    return erpType switch
    {
        ERPType.Buildium => _buildiumProvider,
        ERPType.Hostify => _hostifyProvider,
        ERPType.AppFolio => _appFolioProvider,
        ERPType.None => _nullProvider, // No external sync
        _ => throw new NotSupportedException($"ERP type {erpType} not supported")
    };
}
```

---

## Adapter Interface

```csharp
public interface ITicketSystemProvider
{
    // Work Order operations
    Task<string> CreateWorkOrderAsync(WorkOrder workOrder);
    Task UpdateWorkOrderStatusAsync(string externalId, WorkOrderStatus status);
    Task<WorkOrder> SyncWorkOrderAsync(string externalId);

    // Vendor operations
    Task<IEnumerable<ExternalVendor>> GetVendorsAsync(ServiceCategory category, Address location);
    Task<ExternalVendor> GetVendorDetailsAsync(string externalVendorId);

    // Property operations
    Task<IEnumerable<ExternalProperty>> GetPropertiesAsync();
    Task SyncPropertyAsync(string externalPropertyId);
}
```

---

## Bidirectional Sync

```mermaid
sequenceDiagram
    participant DX as DoorX
    participant Adapter as PMS Adapter
    participant PMS as External PMS

    Note over DX,PMS: Work Order Created in DoorX

    DX->>Adapter: CreateWorkOrderAsync(workOrder)
    Adapter->>Adapter: Transform to PMS format
    Adapter->>PMS: POST /work-orders
    PMS-->>Adapter: { "id": "WO-12345" }
    Adapter-->>DX: Return external ID
    DX->>DX: Store mapping (internal ↔ external ID)

    Note over DX,PMS: Status Updated in PMS

    PMS->>Adapter: Webhook: work_order.completed
    Adapter->>Adapter: Transform to DoorX format
    Adapter->>DX: UpdateWorkOrderStatus(id, Completed)
    DX->>DX: Update work order status
    DX-->>Adapter: 200 OK
```

---

## Data Transformation Example

### DoorX → Buildium

```json
// DoorX Work Order
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "tenantId": "tenant_123",
  "propertyId": "prop_456",
  "category": "HVAC",
  "priority": "High",
  "description": "AC not cooling",
  "status": "Open"
}

// Transformed to Buildium format
{
  "property_id": "12345",
  "unit_id": "67890",
  "category": "HVAC",
  "priority": "Urgent",
  "description": "AC not cooling",
  "entry_contact": {
    "name": "John Doe",
    "phone": "+15551234567"
  }
}
```

---

## Webhook Handling

```mermaid
flowchart TD
    Webhook[Incoming Webhook] --> Validate{Valid<br/>Signature?}

    Validate -->|No| Reject[401 Unauthorized]
    Validate -->|Yes| Parse[Parse payload]

    Parse --> EventType{Event<br/>Type}

    EventType -->|work_order.created| Create[Handle WO Created]
    EventType -->|work_order.updated| Update[Handle WO Updated]
    EventType -->|work_order.completed| Complete[Handle WO Completed]
    EventType -->|vendor.added| VendorAdd[Import Vendor]

    Create --> Sync[Sync to DoorX]
    Update --> Sync
    Complete --> Sync
    VendorAdd --> Sync

    Sync --> ACK[200 OK]
```

---

## Error Handling & Retry

```yaml
retryPolicy:
  maxAttempts: 3
  backoff: exponential
  initialDelay: 1s
  maxDelay: 30s

circuitBreaker:
  threshold: 5 failures in 1 minute
  timeout: 30s
  halfOpenAfter: 60s
```

---

## Referencias

- [Factory Pattern Implementation](../../../src/Infrastructure/Providers/)
- [Integration Tests](../../../tests/Integration/Infrastructure.IntegrationTests/)
