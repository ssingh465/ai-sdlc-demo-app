# Feature Specification: Role-Based Approval Limits

**Feature Branch**: `Not created`

**Created**: 2026-10-27

**Status**: Draft

**Input**: User description: "Approval limits by role for the invoice approval service, including amount boundaries, two distinct approvals for invoices above 500,000, no approval authority for Admin, unchanged invoice state on refusal, and amount-unlimited rejection."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Approve within role limits (Priority: P1)

Managers and Directors need approval decisions to follow their respective amount limits so
invoices are approved only within the authority granted to their role.

**Why this priority**: Role limits determine which invoices can be approved and are the
foundation of reliable approval decisions.

**Independent Test**: Attempt approval at and around each role's stated amount boundary and
verify the resulting approval or refusal.

**Acceptance Scenarios**:

1. **Given** a PendingApproval invoice for 50,000 and a signed-in Manager, **When** the
   Manager approves it, **Then** the invoice becomes Approved.
2. **Given** a PendingApproval invoice for 50,000.01 and a signed-in Manager, **When** the
   Manager attempts approval, **Then** the attempt is refused with the reason
   "Manager limit is 50,000; invoice is 50,000.01."
3. **Given** a PendingApproval invoice for 500,000 and a signed-in Director, **When** the
   Director approves it, **Then** the invoice becomes Approved.
4. **Given** a PendingApproval invoice for 500,000.01 and a signed-in Director who has not
   approved it before, **When** the Director approves it, **Then** the first approval is
   recorded, the invoice remains PendingApproval, and the UI shows "1 of 2 approvals".

---

### User Story 2 - Complete high-value approvals with distinct approvers (Priority: P1)

Approvers need a two-person approval path for invoices above 500,000 so that the required
second decision comes from a different Director.

**Why this priority**: Requiring two distinct people for high-value invoices prevents a single
user from completing both decisions.

**Independent Test**: Record a first approval on an invoice above 500,000, then test both a
repeat attempt by that user and a second approval by a different Director.

**Acceptance Scenarios**:

1. **Given** an invoice above 500,000 with one approval from a Manager, **When** a different
   Director approves it, **Then** the second approval is recorded and the invoice becomes
   Approved.
2. **Given** an invoice above 500,000 with one approval from a Director, **When** a different
   Director approves it, **Then** the second approval is recorded and the invoice becomes
   Approved.
3. **Given** an invoice above 500,000 with one approval by `demo.director`, **When** the
   same user attempts a second approval as `DEMO.DIRECTOR`, **Then** the user is not counted
   as a second approver, the invoice remains PendingApproval with one approval, and no
   invoice field changes.

---

### User Story 3 - Respect approval authority and rejection rules (Priority: P1)

Users need the system to apply the signed-in role, preserve invoice data after refused
approvals, and allow rejection without an amount limit.

**Why this priority**: Correct authority and consistent refusal behavior protect invoice
decisions and their recorded state.

**Independent Test**: Attempt approval as Admin, attempt approval with a role claimed by the
client that differs from the signed-in role, inspect the complete invoice before and after a
refusal, and reject an invoice above 500,000.

**Acceptance Scenarios**:

1. **Given** a PendingApproval invoice and a signed-in Admin, **When** the Admin attempts to
   approve it, **Then** the attempt is refused with the reason
   "Admin has no approval authority".
2. **Given** the signed-in user is an Admin and the client supplies Director as its claimed
   role, **When** that user attempts approval, **Then** the server applies the signed-in
   Admin role and refuses the attempt with the reason "Admin has no approval authority".
3. **Given** a Manager attempts to approve a 50,000.01 invoice, **When** the approval is
   refused, **Then** every invoice field is identical to its value before the attempt,
   including status, all recorded approvals, DecidedBy, DecidedAt, and DecisionNote.
4. **Given** a PendingApproval invoice above 500,000 and a user permitted to reject it,
   **When** the user chooses Reject, **Then** the rejection is not refused because of the
   invoice amount.
5. **Given** the demo users, **When** their approval roles are used, **Then**
   `demo.manager` is a Manager, `demo.director` and `demo.director2` are Directors, and
   `demo.admin` is an Admin; the former Approver role is replaced by Manager.

### Edge Cases

- At 50,000, a Manager is within the limit; at 50,000.01, the Manager is over the limit.
- At 500,000, a Director is within the limit; at 500,000.01, the invoice requires two
  distinct approvals.
- A username differing only by letter casing identifies the same approver for the
  two-approval rule.
- A refused approval must preserve every invoice field, not only its status.
- Rejection is not subject to an invoice amount limit.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A Manager MUST be permitted to approve an invoice with an amount up to and
  including 50,000.
- **FR-002**: A Manager approval attempt for an invoice above 50,000 and not above 500,000
  MUST be refused with a reason stating the Manager limit and invoice amount.
- **FR-003**: A Director MUST be permitted to approve an invoice with an amount up to and
  including 500,000.
- **FR-004**: An invoice above 500,000 MUST require two distinct approvers. The first
  approval may be from any Manager or Director, and MUST leave the invoice
  PendingApproval while recording the approval and showing "1 of 2 approvals".
- **FR-005**: The second approval for an invoice above 500,000 MUST be from a Director
  distinct from the first approver; upon that approval, the invoice MUST become Approved.
- **FR-006**: The same user MUST NOT be counted as both approvers for one invoice.
  Usernames MUST be compared case-insensitively.
- **FR-007**: An approval attempt by an Admin MUST be refused with the exact reason
  "Admin has no approval authority".
- **FR-008**: A refused approval MUST return a clear reason that identifies the role's
  applicable limit and the invoice amount when the refusal is due to an amount limit.
- **FR-009**: A refused approval MUST leave every invoice field unchanged, including status,
  all recorded approvals, DecidedBy, DecidedAt, and DecisionNote.
- **FR-010**: Rejecting an invoice MUST NOT be refused due to its amount.
- **FR-011**: The approval role MUST be determined from the signed-in session and MUST NOT
  be trusted from a role supplied by the client.
- **FR-012**: The demo users MUST have these roles: `demo.manager` as Manager,
  `demo.director` and `demo.director2` as Director, and `demo.admin` as Admin. The former
  Approver role is replaced by Manager.

### Key Entities

- **Invoice**: An item being approved or rejected, with an amount, status, recorded
  approvals, and decision details.
- **Approver**: A signed-in user identified by username and role whose approval may be
  recorded against an invoice.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All four specified boundary values (50,000; 50,000.01; 500,000; and
  500,000.01) produce the approval or pending-approval outcomes defined in the acceptance
  scenarios.
- **SC-002**: Every refused approval in the acceptance scenarios is refused for the
  specified reason and leaves 100% of invoice fields unchanged.
- **SC-003**: An invoice above 500,000 becomes Approved only after two distinct approvers,
  with the second approver a Director.
- **SC-004**: All listed demo users are associated with their specified roles, and a
  client-supplied role cannot change the role used for an approval decision.
- **SC-005**: A rejection attempt on an invoice above 500,000 is not blocked by an amount
  limit.

## Assumptions

- The existing signed-in session identifies the user and supplies the role used for an
  approval decision.
- The invoice's recorded approvals and decision details are part of its state and are
  included when checking whether a refused attempt changed anything.
- The feature applies to the existing invoice approval and Reject actions.
