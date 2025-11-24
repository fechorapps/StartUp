# DoorX - Assign Vendor Sequence

## Descripción

Secuencia detallada del proceso de asignación de un vendor a un work order.

---

## Sequence Diagram

```mermaid
sequenceDiagram
    actor PM as Property Manager
    participant API as API Controller
    participant Handler as Command Handler
    participant WORepo as WorkOrder Repository
    participant VRepo as Vendor Repository
    participant Domain as Work Order Aggregate
    participant UoW as Unit of Work
    participant DB as PostgreSQL
    participant Events as Event Dispatcher
    participant SMS as SMS Service
    participant PMS as PMS Provider

    PM->>API: PUT /api/workorders/{id}/assign<br/>{vendorId}

    API->>Handler: Handle(AssignVendorCommand)

    %% Step 1: Load work order with bids
    Handler->>WORepo: GetWithBidsAsync(workOrderId)
    WORepo->>DB: SELECT wo.*, vb.* FROM WorkOrders wo<br/>LEFT JOIN VendorBids vb ON vb.WorkOrderId = wo.Id<br/>WHERE wo.Id = ?
    DB-->>WORepo: Work order + bids data
    WORepo-->>Handler: WorkOrder aggregate

    alt Work order not found
        Handler-->>API: Error.NotFound("WorkOrder.NotFound")
        API-->>PM: 404 Not Found
    end

    %% Step 2: Verify vendor exists
    Handler->>VRepo: GetByIdAsync(vendorId)
    VRepo->>DB: SELECT * FROM Vendors WHERE Id = ?
    DB-->>VRepo: Vendor data
    VRepo-->>Handler: Vendor

    alt Vendor not found
        Handler-->>API: Error.NotFound("Vendor.NotFound")
        API-->>PM: 404 Not Found
    end

    %% Step 3: Verify vendor is qualified
    Handler->>Handler: Check vendor qualifications<br/>(service category, location)

    alt Vendor not qualified
        Handler-->>API: Error.Validation("Vendor.NotQualified")
        API-->>PM: 400 Bad Request
    end

    %% Step 4: Assign vendor (domain logic)
    Handler->>Domain: workOrder.AssignVendor(vendorId)
    Domain->>Domain: Validate business rules<br/>- Current status allows assignment<br/>- Vendor has submitted bid (optional)
    Domain->>Domain: Update Status to VendorAssigned
    Domain->>Domain: Set AssignedVendorId
    Domain->>Domain: Reject other bids
    Domain->>Domain: AddDomainEvent(VendorAssignedEvent)
    Domain-->>Handler: Success

    %% Step 5: Persist changes
    Handler->>WORepo: Update(workOrder)
    Handler->>UoW: SaveChangesAsync()
    UoW->>DB: BEGIN TRANSACTION
    UoW->>DB: UPDATE WorkOrders SET<br/>AssignedVendorId = ?,<br/>Status = 'VendorAssigned',<br/>ModifiedAt = NOW()<br/>WHERE Id = ?
    UoW->>DB: UPDATE VendorBids SET Status = 'Rejected'<br/>WHERE WorkOrderId = ? AND VendorId != ?
    UoW->>DB: UPDATE VendorBids SET Status = 'Accepted'<br/>WHERE WorkOrderId = ? AND VendorId = ?
    DB-->>UoW: Success
    UoW->>DB: COMMIT TRANSACTION
    UoW-->>Handler: Changes saved

    %% Step 6: Domain events
    Handler->>Events: Publish(VendorAssignedEvent)

    par Notify vendor
        Events->>SMS: Send SMS to assigned vendor
        SMS->>SMS: "You've been assigned to Work Order #12345"
    and Sync to PMS
        Events->>PMS: Update work order in external PMS
        PMS->>PMS: UpdateWorkOrderStatusAsync(externalId, VendorAssigned)
    and Notify tenant
        Events->>SMS: Send SMS to tenant
        SMS->>SMS: "ABC HVAC has been assigned to your work order"
    and Notify other vendors
        Events->>SMS: Send SMS to other vendors
        SMS->>SMS: "Thank you for your bid. Another vendor was selected."
    end

    %% Step 7: Response
    Handler-->>API: Success
    API-->>PM: 200 OK<br/>{workOrder with assigned vendor}
```

---

## Request/Response Examples

### Request
```http
PUT /api/workorders/770e8400-e29b-41d4-a716-446655440000/assign HTTP/1.1
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "vendorId": "880e8400-e29b-41d4-a716-446655440000"
}
```

### Response (Success)
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "770e8400-e29b-41d4-a716-446655440000",
  "status": "VendorAssigned",
  "assignedVendor": {
    "id": "880e8400-e29b-41d4-a716-446655440000",
    "companyName": "ABC HVAC Services",
    "contactName": "John Smith",
    "phoneNumber": "+15559876543",
    "rating": 4.8,
    "bidAmount": 200.00
  },
  "assignedAt": "2024-01-15T11:00:00Z",
  "modifiedAt": "2024-01-15T11:00:00Z"
}
```

---

## Business Rules

1. **Work Order Status**
   - Must be in `Open` or `BiddingInProgress` status
   - Cannot assign if already `VendorAssigned`, `InProgress`, `Completed`, or `Cancelled`

2. **Vendor Qualifications**
   - Vendor must be active (not suspended)
   - Vendor must offer the required service category
   - Vendor's service area must include property location
   - Vendor rating must be ≥ 3.5 (configurable)

3. **Bid Requirements (Optional)**
   - If `requireBid` is enabled, vendor must have submitted a bid
   - Bid amount must be within budget (if budget set)

4. **Multiple Assignments**
   - Only one vendor can be assigned at a time
   - Assigning a new vendor rejects all other pending bids

5. **Notifications**
   - Assigned vendor receives SMS + Email
   - Tenant receives notification with vendor details
   - Other bidding vendors receive rejection notification
   - Property manager receives confirmation

---

## Error Scenarios

| Error | HTTP Code | Message |
|-------|-----------|---------|
| Work order not found | 404 | `WorkOrder.NotFound` |
| Vendor not found | 404 | `Vendor.NotFound` |
| Invalid status transition | 400 | `WorkOrder.InvalidStatusTransition` |
| Vendor not qualified | 400 | `Vendor.NotQualified` |
| Vendor inactive | 400 | `Vendor.Inactive` |
| No bid submitted | 400 | `Vendor.BidRequired` |
| Already assigned | 409 | `WorkOrder.AlreadyAssigned` |

---

## State Transitions

```mermaid
stateDiagram-v2
    [*] --> Open
    Open --> BiddingInProgress: Request bids
    BiddingInProgress --> VendorAssigned: Assign vendor
    Open --> VendorAssigned: Auto-assign (single vendor)

    VendorAssigned --> InProgress: Vendor starts work

    note right of VendorAssigned
        All other bids automatically rejected
        Notifications sent to all parties
    end note
```

---

## Referencias

- [AssignVendorCommandHandler](../../../src/Application/WorkOrders/Commands/AssignVendor/AssignVendorCommandHandler.cs)
- [WorkOrder.AssignVendor()](../../../src/Domain/WorkOrders/Entities/WorkOrder.cs)
- [VendorAssignedEvent](../../../src/Domain/WorkOrders/Events/VendorAssignedEvent.cs)
