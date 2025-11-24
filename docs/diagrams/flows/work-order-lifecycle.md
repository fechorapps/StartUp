# DoorX - Work Order Lifecycle Flow

## Descripción

Flujo completo del ciclo de vida de un Work Order desde su creación hasta su cierre.

---

## Complete Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Open: Tenant reports issue

    Open --> BiddingInProgress: Request bids from vendors
    BiddingInProgress --> BiddingInProgress: Vendor submits bid
    BiddingInProgress --> VendorAssigned: Property Manager assigns vendor

    VendorAssigned --> InProgress: Vendor starts work
    InProgress --> Completed: Vendor completes work

    Completed --> Closed: Tenant confirms completion
    Completed --> InProgress: Tenant requests changes

    Open --> Cancelled: Tenant/PM cancels
    BiddingInProgress --> Cancelled: No vendors available
    VendorAssigned --> Cancelled: Vendor unavailable

    Closed --> [*]
    Cancelled --> [*]

    note right of Open
        AI categorizes issue
        Determines priority
    end note

    note right of BiddingInProgress
        Up to 5 bids allowed
        Auto-assign if single vendor
    end note

    note right of Completed
        Requires photo proof
        Final cost recorded
    end note
```

---

## Detailed Flow with Actors

```mermaid
sequenceDiagram
    participant T as Tenant
    participant AI as Aimee (AI)
    participant SYS as DoorX System
    participant PM as Property Manager
    participant V1 as Vendor 1
    participant V2 as Vendor 2
    participant PMS as External PMS

    %% Step 1: Report Issue
    T->>AI: "My AC is broken"
    AI->>SYS: Categorize: HVAC, Priority: High
    SYS->>SYS: Create WorkOrder (Open)
    SYS->>PMS: Sync work order
    SYS->>PM: Notify: New urgent work order
    AI->>T: "I understand. Finding technicians..."

    %% Step 2: Find Vendors
    SYS->>SYS: Find vendors (HVAC, Location)
    SYS->>V1: Request bid
    SYS->>V2: Request bid
    SYS->>SYS: Status: BiddingInProgress

    %% Step 3: Collect Bids
    V1->>SYS: Submit bid: $250
    SYS->>PM: Notify: Bid received
    V2->>SYS: Submit bid: $200
    SYS->>PM: Notify: Bid received

    %% Step 4: Assign Vendor
    PM->>SYS: Assign V2 (lower bid)
    SYS->>SYS: Status: VendorAssigned
    SYS->>V2: SMS: "Assigned! Tenant: John, Address: ..."
    SYS->>T: SMS: "ABC HVAC will visit tomorrow 2 PM"
    SYS->>PMS: Update: Vendor assigned

    %% Step 5: Work Progress
    V2->>SYS: Start work
    SYS->>SYS: Status: InProgress
    V2->>SYS: Complete work + upload photo
    SYS->>SYS: Status: Completed

    %% Step 6: Confirmation
    SYS->>T: SMS: "Work completed. Confirm?"
    T->>SYS: "Yes, working great!"
    SYS->>SYS: Status: Closed
    SYS->>PM: Notify: Work order closed
    SYS->>PMS: Sync: Closed
    SYS->>V2: Release payment
```

---

## State Transition Rules

| Current Status | Valid Next States | Trigger |
|----------------|-------------------|---------|
| Open | BiddingInProgress, Cancelled | Request bids or cancel |
| BiddingInProgress | VendorAssigned, Cancelled | Assign vendor or no vendors |
| VendorAssigned | InProgress, Cancelled | Vendor starts or cancels |
| InProgress | Completed, Cancelled | Work done or issue |
| Completed | Closed, InProgress | Tenant confirms or rejects |
| Closed | - | Terminal state |
| Cancelled | - | Terminal state |

---

## Auto-Assignment Logic

```mermaid
flowchart TD
    Start[Work Order Created] --> CheckBids{How many<br/>vendors available?}

    CheckBids -->|0 vendors| Escalate[Escalate to PM<br/>Find external vendor]
    CheckBids -->|1 vendor| AutoAssign[Auto-assign to vendor<br/>Skip bidding]
    CheckBids -->|2-5 vendors| Bidding[Request bids<br/>BiddingInProgress]

    Escalate --> Manual[Manual assignment by PM]
    AutoAssign --> Assigned[VendorAssigned]
    Bidding --> CollectBids[Collect bids for 24h]

    CollectBids --> HasBids{Bids received?}
    HasBids -->|Yes| PMReview[PM reviews bids]
    HasBids -->|No| Escalate

    PMReview --> Assigned
```

---

## Priority-Based SLA

| Priority | Response Time | Resolution Time | Auto-Escalation |
|----------|---------------|-----------------|-----------------|
| Emergency | 1 hour | 4 hours | 30 min if no vendor |
| High | 4 hours | 24 hours | 2 hours if no vendor |
| Medium | 24 hours | 3 days | Next business day |
| Low | 3 days | 7 days | After 5 days |

---

## Cost Approval Rules

```mermaid
flowchart TD
    Start[Vendor submits bid] --> CheckAmount{Bid amount?}

    CheckAmount -->|< $100| AutoApprove[Auto-approve<br/>Assign vendor]
    CheckAmount -->|$100-$500| TenantApprove{Tenant approves?}
    CheckAmount -->|> $500| PMApprove{PM approves?}

    TenantApprove -->|Yes| AutoApprove
    TenantApprove -->|No| Reject[Reject bid]

    PMApprove -->|Yes| Assign[Assign vendor]
    PMApprove -->|No| Reject

    Reject --> FindAnother[Find another vendor]
```

---

## Referencias

- [DoorX Business Rules](../../BUSINESS_RULES.md)
- [Work Order Entity](../../../src/Domain/WorkOrders/Entities/WorkOrder.cs)
