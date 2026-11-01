# Research: Role-Based Approval Limits

## Domain boundaries and service behavior

- **Decision**: Keep `ApprovalRole`, `ApprovalPolicy`, `ApprovalRecord`,
  `ApprovalRefusedException`, and approval workflow decisions in
  `InvoiceApp.Domain`. Use the requested `ApprovalService.Approve(string invoiceId,
  string approver, ApprovalRole role)` signature.
- **Rationale**: The repository's `AGENTS.md` assigns limits and two-approver logic to the
  Domain. The existing `ApprovalService` already owns invoice lifecycle transitions.
- **Alternatives considered**: Enforcing limits in the controller was rejected because it
  would violate project layering and allow service callers to bypass the rules.

## Refusal validation and HTTP response

- **Decision**: Complete all approval eligibility checks before changing invoice data or
  calling `IInvoiceRepository.Update`. Use `ApprovalRefusedException` with the required
  human-readable reason; preserve the existing 409 response body `{ error: reason }`.
- **Rationale**: The exception derives from `InvalidOperationException`, which the existing
  `ApiControllerBase.Handle` maps to HTTP 409 with its message in the `error` field. The UI
  already renders API error text in `#decision-error`.
- **Alternatives considered**: Adding a new response envelope or a second error display was
  unnecessary and would diverge from existing API/UI behavior.

## Role authority

- **Decision**: Restrict the approvals endpoint to Manager, Director, and Admin, and map
  `UserSession.Role` to the Domain `ApprovalRole` before calling `ApprovalService`.
- **Rationale**: The existing session middleware authenticates API requests and puts a
  `UserSession` in request context; `RequireRoleAttribute` already supports role lists.
  The feature requires the session, not a request value, to be authoritative.
- **Alternatives considered**: Passing a role from a request body was rejected because it
  violates the security rule. Referencing API authentication types from the Domain was
  rejected because the Domain must remain independent of the API.

## Amount and identity comparisons

- **Decision**: Apply inclusive limits at 50,000 and 500,000; use decimal comparisons and
  format amount-bearing refusal messages with `#,##0.##` and invariant culture. Compare
  usernames without regard to case when checking approver distinctness.
- **Rationale**: These choices directly implement the documented boundaries, message
  examples, and distinct-user rule without dependence on machine locale.
- **Alternatives considered**: Culture-dependent formatting and case-sensitive username
  matching were rejected because they would produce inconsistent reasons or let the same
  account count twice.

## Two-approver state and UI

- **Decision**: Store each approval as the requested `ApprovalRecord` in an initialized
  `Invoice.Approvals` list. Keep the invoice pending after the first approval above 500,000;
  show "1 of 2 approvals" in the pending list; approve only after a distinct Director makes
  the second approval.
- **Rationale**: The approval records make the state auditable and allow the rule to be
  evaluated from Domain state. Existing UI renders pending invoices from API invoice data.
- **Alternatives considered**: Storing only one `DecidedBy` value was rejected because it
  cannot represent both approvers.

## Test and repository practices

- **Decision**: Add isolated xUnit tests to the existing test project with
  `FakeInvoiceRepository`, and use the existing Playwright project for the over-limit UI
  refusal scenario.
- **Rationale**: These are the project's established test stacks. The fake repository can be
  instrumented to assert refused attempts do not call `Update`.
- **Alternatives considered**: Introducing a new test framework or end-to-end harness was
  unnecessary.

## Delivery ordering

- **Decision**: Implement Phase 1 single-approver limits, role rename, demo users and seed
  data, and Admin refusal first; Phase 2 two-approver behavior second; then add the
  Playwright refused-over-limit approval test.
- **Rationale**: This preserves the user's requested delivery sequencing while allowing each
  domain behavior to be verified before adding the next lifecycle stage.
- **Alternatives considered**: Combining all behavior into one phase was not selected
  because it would disregard the requested staged delivery.

All technology and behavior choices are specified by the user or already established in the
repository. No unresolved clarifications remain.
