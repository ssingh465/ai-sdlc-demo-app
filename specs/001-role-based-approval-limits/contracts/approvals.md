# Approval API and UI Contract

This feature extends the existing authenticated approvals endpoint and preserves the
existing response and error conventions.

## Authentication and role source

- Approval and rejection calls require the existing signed-in bearer session.
- The approval role is obtained from the server-side `UserSession.Role`.
- The request body does not supply an approval role.
- The approvals controller allows session roles `Manager`, `Director`, and `Admin`.
- Admin may use admin powers but cannot approve invoices.

## Approve

`POST /api/approvals/{invoiceId}/approve`

- Request body: empty JSON object `{}` for the existing UI call; no role or username fields.
- Successful response: HTTP 200 with the updated invoice JSON, including `approvals`.
- A first approval for an invoice above 500,000 returns the invoice as PendingApproval with
  one recorded approval.
- A completed approval returns the invoice as Approved.
- A refusal returns HTTP 409 with JSON `{ "error": "<reason>" }`.
- Admin refusal reason: `Admin has no approval authority`.
- Example Manager limit reason: `Manager limit is 50,000; invoice is 72,500.`
- On refusal, invoice JSON and repository state remain unchanged.

## Reject

`POST /api/approvals/{invoiceId}/reject`

- Request body remains `{ "reason": "<non-empty reason>" }`.
- Rejection eligibility is not restricted by invoice amount.
- Existing successful and validation response behavior remains unchanged.

## Pending approvals UI

- Pending invoice entries continue to expose their existing approve and reject actions.
- When `amount > 500000` and exactly one approval is recorded, display the text
  `1 of 2 approvals`.
- Display an approval refusal's `error` message in the existing `#decision-error` alert.

## Invoice approval data

- Invoice JSON includes `approvals`, a list of records with `approver`, `role`, and `at`.
- Refused attempts do not add records or change invoice fields.
