# Data Model: Role-Based Approval Limits

## ApprovalRole

Domain enum describing the role passed by the API from the signed-in session.

- Values: `Manager`, `Director`, `Admin`.
- Approval authority and limits are governed by `ApprovalPolicy.LimitFor(ApprovalRole)`.
- `LimitFor` returns `null` for Admin.

## ApprovalPolicy

Static Domain policy for role approval limits.

- `LimitFor(Manager)` returns 50,000.
- `LimitFor(Director)` returns 500,000.
- `LimitFor(Admin)` returns `null`; Admin approval attempts are refused with
  `Admin has no approval authority`.
- Boundaries are inclusive: exactly 50,000 is within the Manager limit and exactly 500,000
  is within the Director limit.
- Amount-bearing refusal messages use `#,##0.##` with invariant culture.

## ApprovalRecord

Immutable record for an approval action.

- `Approver`: username of the signed-in approver.
- `Role`: the session-derived approval role.
- `At`: timestamp of the recorded approval.
- `Invoice.Approvals` contains approval records in the order they were recorded.
- Usernames are compared case-insensitively when determining distinct approvers.

## Invoice

Existing aggregate representing an invoice.

- Existing fields remain: `Id`, `CustomerName`, `Amount`, `Status`, `CreatedAt`,
  `DecidedBy`, `DecidedAt`, and `DecisionNote`.
- Add `Approvals`, an initialized list of `ApprovalRecord`.
- An approval refusal leaves every existing invoice field and the approval list unchanged.
- For invoices above 500,000, a first valid approval leaves status PendingApproval with one
  recorded approval; the UI displays "1 of 2 approvals".
- A distinct Director's valid second approval changes status to Approved.
- A user with the same username under different casing cannot add a second approval.

## ApprovalRefusedException

Domain refusal signal inheriting from `InvalidOperationException`.

- `Message` is the reason returned to the caller.
- Amount-limit reasons identify the role, its limit, and the invoice amount.
- Admin refusal message is exactly `Admin has no approval authority`.
- Refusal is raised before any invoice mutation or repository update.

## ApprovalService

Owns invoice approval lifecycle and validates before mutation.

- Approval entry point: `Approve(string invoiceId, string approver, ApprovalRole role)`.
- Applies role limits for ordinary approvals and the two-distinct-approver rule above
  500,000.
- First approval above 500,000 may be from a Manager or Director and leaves the invoice
  pending.
- Second approval above 500,000 must be by a Director distinct from the first approver.
- Reject remains separate from approval limits; no amount limit applies to Reject.

## State and invariants

| Condition | Result |
|---|---|
| Pending invoice amount at or below 50,000; role Manager | Approval is permitted |
| Pending invoice amount above 50,000 through 500,000; role Director | Approval is permitted |
| Invoice amount above 500,000; first Manager or Director approval | Record first approval; remain PendingApproval |
| Invoice amount above 500,000; distinct Director approves second | Record second approval; status becomes Approved |
| Second attempt uses same username, including different casing | Refuse; do not alter invoice or persist |
| Approval role is Admin | Refuse with exact Admin reason; do not alter invoice or persist |
| Any approval refusal | Preserve all invoice fields and do not update repository |
| Reject action on an invoice at any amount | Amount alone does not refuse rejection |

## Associations

- One Invoice has zero or more ApprovalRecords.
- Each ApprovalRecord refers to one invoice by its containing Invoice and identifies one
  approver and their session-derived role.
