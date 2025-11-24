# DoorX - Complete Work Order Sequence

## Descripción

Secuencia del proceso de completar un work order, incluyendo confirmación del tenant y cierre final.

---

## Sequence Diagram

```mermaid
sequenceDiagram
    actor Vendor
    participant API as API Controller
    participant Handler as Command Handler
    participant Repo as Repository
    participant Domain as Work Order Aggregate
    participant UoW as Unit of Work
    participant DB as PostgreSQL
    participant Storage as Blob Storage
    participant Events as Event Dispatcher
    participant SMS as SMS Service
    participant PMS as PMS Provider
    actor Tenant

    %% Step 1: Vendor submits completion
    Vendor->>API: POST /api/workorders/{id}/complete<br/>{finalCost, notes, photos[]}

    API->>Handler: Handle(CompleteWorkOrderCommand)

    %% Step 2: Load work order
    Handler->>Repo: GetByIdAsync(workOrderId)
    Repo->>DB: SELECT * FROM WorkOrders WHERE Id = ?
    DB-->>Repo: Work order data
    Repo-->>Handler: WorkOrder

    alt Work order not found
        Handler-->>API: Error.NotFound
        API-->>Vendor: 404 Not Found
    end

    alt Not assigned to this vendor
        Handler-->>API: Error.Forbidden
        API-->>Vendor: 403 Forbidden
    end

    %% Step 3: Upload proof photos
    loop For each photo
        Handler->>Storage: Upload photo
        Storage-->>Handler: Photo URL
    end

    %% Step 4: Complete work order (domain)
    Handler->>Domain: workOrder.Complete(finalCost, notes, photoUrls)
    Domain->>Domain: Validate business rules<br/>- Must be InProgress<br/>- FinalCost <= EstimatedCost * 1.2
    Domain->>Domain: Update Status to Completed
    Domain->>Domain: Set FinalCost and CompletedAt
    Domain->>Domain: AddDomainEvent(WorkOrderCompletedEvent)
    Domain-->>Handler: Success

    %% Step 5: Persist
    Handler->>Repo: Update(workOrder)
    Handler->>UoW: SaveChangesAsync()
    UoW->>DB: BEGIN TRANSACTION
    UoW->>DB: UPDATE WorkOrders SET<br/>Status = 'Completed',<br/>FinalCost = ?,<br/>CompletedAt = NOW()<br/>WHERE Id = ?
    UoW->>DB: INSERT INTO WorkOrderPhotos VALUES (...)
    DB-->>UoW: Success
    UoW->>DB: COMMIT TRANSACTION
    UoW-->>Handler: Changes saved

    %% Step 6: Domain events
    Handler->>Events: Publish(WorkOrderCompletedEvent)

    par Notify tenant for confirmation
        Events->>SMS: Send SMS to tenant
        SMS->>Tenant: "Work completed! Please confirm:<br/>✅ Approve<br/>❌ Request changes"
    and Update PMS
        Events->>PMS: Sync completion to PMS
        PMS->>PMS: UpdateWorkOrderStatusAsync(Completed)
    end

    %% Step 7: Response to vendor
    Handler-->>API: Success
    API-->>Vendor: 200 OK<br/>"Work marked as completed.<br/>Awaiting tenant confirmation."

    %% === TENANT CONFIRMATION FLOW ===

    Note over Tenant,PMS: Tenant Confirmation (separate request)

    Tenant->>API: POST /api/workorders/{id}/confirm<br/>{approved: true, rating: 5}

    API->>Handler: Handle(ConfirmWorkOrderCommand)
    Handler->>Repo: GetByIdAsync(workOrderId)
    Repo-->>Handler: WorkOrder

    Handler->>Domain: workOrder.Close()
    Domain->>Domain: Update Status to Closed
    Domain->>Domain: AddDomainEvent(WorkOrderClosedEvent)

    Handler->>UoW: SaveChangesAsync()
    UoW->>DB: UPDATE WorkOrders SET Status = 'Closed'
    UoW->>DB: UPDATE Vendors SET<br/>Rating = (calculate new rating),<br/>CompletedJobs = CompletedJobs + 1

    Handler->>Events: Publish(WorkOrderClosedEvent)
    Events->>SMS: Notify vendor: "Payment released"
    Events->>PMS: Close work order in PMS

    Handler-->>API: Success
    API-->>Tenant: 200 OK "Thank you for confirming!"
```

---

## Request/Response Examples

### Request (Vendor Completes)
```http
POST /api/workorders/770e8400-e29b-41d4-a716-446655440000/complete HTTP/1.1
Authorization: Bearer eyJhbGc...
Content-Type: multipart/form-data

{
  "finalCost": 195.00,
  "notes": "Replaced refrigerant and fixed leak in condenser coil",
  "photos": [
    <binary photo 1>,
    <binary photo 2>
  ]
}
```

### Response (Success)
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "770e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "finalCost": 195.00,
  "completedAt": "2024-01-16T14:30:00Z",
  "photos": [
    "https://storage.doorx.com/photos/wo-12345-1.jpg",
    "https://storage.doorx.com/photos/wo-12345-2.jpg"
  ],
  "message": "Work marked as completed. Awaiting tenant confirmation."
}
```

### Request (Tenant Confirms)
```http
POST /api/workorders/770e8400-e29b-41d4-a716-446655440000/confirm HTTP/1.1
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "approved": true,
  "rating": 5,
  "feedback": "Great service! Fixed quickly and professionally."
}
```

---

## Business Rules

### Completion Requirements
1. **Work Order Status**
   - Must be `InProgress` to mark as completed
   - Cannot complete if `Open`, `Cancelled`, or already `Completed`

2. **Authorization**
   - Only assigned vendor can mark as completed
   - Property manager can override if needed

3. **Final Cost**
   - Must be provided
   - Warning if exceeds estimated cost by >20%
   - Auto-approve if < $100
   - Requires PM approval if > $500

4. **Proof of Work**
   - At least 1 photo required
   - Photos stored in Azure Blob Storage
   - Maximum 5 photos per work order

### Tenant Confirmation
1. **Approval Options**
   - ✅ Approve: Work order closed, payment released
   - ❌ Request changes: Status back to `InProgress`
   - ⏰ No response after 48h: Auto-approve

2. **Rating**
   - Required (1-5 stars)
   - Updates vendor's overall rating
   - Increments vendor's `CompletedJobs` counter

3. **Auto-Approval**
   - After 48 hours without tenant response
   - Automatic 4-star rating
   - Notification sent to PM

---

## State Transitions

```mermaid
stateDiagram-v2
    InProgress --> Completed: Vendor completes work
    Completed --> Closed: Tenant approves
    Completed --> InProgress: Tenant requests changes
    Completed --> Closed: Auto-approve (48h timeout)

    note right of Completed
        Awaiting tenant confirmation
        Photos uploaded
        Final cost recorded
    end note

    note right of Closed
        Payment released to vendor
        Vendor rating updated
        Terminal state
    end note
```

---

## Payment Release Logic

```mermaid
flowchart TD
    Complete[Work Completed] --> Cost{Final Cost?}

    Cost -->|< $100| AutoRelease[Auto-release payment]
    Cost -->|$100-$500| TenantApprove{Tenant<br/>approves?}
    Cost -->|> $500| PMApprove{PM<br/>approves?}

    TenantApprove -->|Yes| AutoRelease
    TenantApprove -->|No response 48h| AutoRelease
    TenantApprove -->|Rejects| Review[Manual review]

    PMApprove -->|Yes| Release[Release payment]
    PMApprove -->|No| Dispute[Dispute resolution]

    AutoRelease --> VendorPaid[Vendor receives payment]
    Release --> VendorPaid
```

---

## Error Scenarios

| Error | HTTP Code | Message |
|-------|-----------|---------|
| Work order not found | 404 | `WorkOrder.NotFound` |
| Not in InProgress status | 400 | `WorkOrder.InvalidStatus` |
| Not assigned vendor | 403 | `Vendor.Unauthorized` |
| Final cost too high | 400 | `WorkOrder.CostExceedsLimit` |
| No photos provided | 400 | `WorkOrder.PhotosRequired` |
| Photo upload failed | 500 | `Storage.UploadFailed` |

---

## Metrics Tracked

- **Completion Time:** CompletedAt - CreatedAt
- **On-Budget Rate:** FinalCost ≤ EstimatedCost
- **First-Time Fix Rate:** Completed without reopening
- **Tenant Satisfaction:** Average rating per vendor
- **Response Time:** CompletedAt - AssignedAt

---

## Referencias

- [CompleteWorkOrderCommandHandler](../../../src/Application/WorkOrders/Commands/CompleteWorkOrder/)
- [WorkOrder.Complete()](../../../src/Domain/WorkOrders/Entities/WorkOrder.cs)
- [WorkOrderCompletedEvent](../../../src/Domain/WorkOrders/Events/WorkOrderCompletedEvent.cs)
