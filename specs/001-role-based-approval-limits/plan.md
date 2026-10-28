# Implementation Plan: Role-Based Approval Limits

**Branch**: `feature/approval-limits` | **Date**: 2026-10-27 | **Spec**:
[spec.md](./spec.md)

**Input**: Feature specification from
`/specs/001-role-based-approval-limits/spec.md`

## Summary

Add role-based approval limits and a two-distinct-approver workflow for invoices above
500,000, while preserving state on refused approvals and allowing amount-unlimited rejection.
The Domain owns the rules, the API derives the role from the signed-in session, and the UI
displays recorded approvals. Deliver the change in the requested phases: (1) single-approver
limits, role rename, demo users and invoice seeds, and Admin refusal; (2) two approvals for
invoices above 500,000; then add a Playwright test for a refused over-limit approval.

## Technical Context

**Language/Version**: C# / .NET 8; TypeScript for Playwright end-to-end tests

**Primary Dependencies**: ASP.NET Core Web API, xUnit, Playwright, in-memory invoice
repository

**Storage**: In-memory repository; demo user configuration in `appsettings.json`

**Testing**: xUnit domain tests; Playwright end-to-end tests

**Target Platform**: ASP.NET Core web service and static browser UI

**Project Type**: Web application (API and static UI)

**Performance Goals**: No new performance target specified; preserve the existing demo
application's interactive behavior.

**Constraints**: All approval decisions and limits reside in the Domain. All checks precede
any invoice mutation or repository update. Amounts in refusal messages use invariant culture
and the `#,##0.##` format. API authorization and approval role use the signed-in session.
Use the specified design names and preserve existing lifecycle behavior outside the new rules.

**Scale/Scope**: Approval lifecycle, demo users and seeded invoices, pending-approval UI,
domain unit tests, and a refused-approval browser test.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain-Owned Business Rules**: PASS. Role limits and both approval decisions are
  implemented in `InvoiceApp.Domain`; the controller only maps and forwards session
  identity and role.
- **Rule-Complete Unit Tests**: PASS. Plan boundary, refusal, distinct-user, role, and
  unchanged-state tests that fail when their named rules are broken.
- **State-Preserving Refusals**: PASS. Validate all applicable conditions before changing
  invoice fields or calling repository update; refusal tests snapshot the complete invoice.
- **Server-Authoritative Roles**: PASS. The API obtains `UserSession.Role` from the
  authenticated request context and does not accept a client role for approval.
- **Delivery Gates**: PASS. Validate with the solution test command and E2E command; each
  implementation commit must build, pass tests, and explain its change and rationale.
- **End-to-End Tests**: PASS. The browser test uses the existing page-object approach and
  accessible role/label locators, without CSS or XPath selectors.

**Pre-design gate**: PASS. The project structure and provided design establish the
implementation boundaries; no unresolved technical choices remain.

## Project Structure

### Documentation (this feature)

```text
specs/001-role-based-approval-limits/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── approvals.md
└── tasks.md
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── InvoiceApp.Domain/
│   │   ├── ApprovalRole.cs
│   │   ├── ApprovalPolicy.cs
│   │   ├── ApprovalRecord.cs
│   │   ├── ApprovalRefusedException.cs
│   │   ├── ApprovalService.cs
│   │   └── Invoice.cs
│   └── InvoiceApp.Api/
│       ├── Controllers/ApprovalsController.cs
│       ├── Auth/UserSession.cs
│       ├── Auth/RequireRoleAttribute.cs
│       ├── InMemoryInvoiceRepository.cs
│       ├── appsettings.json
│       └── wwwroot/app.js
└── tests/InvoiceApp.Tests/
    ├── ApprovalPolicyTests.cs
    ├── ApprovalServiceTests.cs
    └── FakeInvoiceRepository.cs

e2e/
├── pages/
└── tests/
    └── approval-limits.spec.ts
```

**Structure Decision**: Extend the existing ASP.NET Core solution, keeping business rules
in the Domain project, session mapping and role restrictions in the API, in-memory demo
data in the Api project, xUnit coverage in the existing test project, and the browser test
in the existing Playwright project.

## Delivery Phases

### Phase 1: Single-Approver Limits and Demo Setup

- Add `ApprovalRole { Manager, Director, Admin }` and
  `ApprovalPolicy.LimitFor(ApprovalRole role)` returning `decimal?` and `null` for Admin.
- Add `ApprovalRefusedException : InvalidOperationException`; use its message as the refusal
  reason. Format role-limit messages with `#,##0.##` and invariant culture.
- Change `ApprovalService.Approve` to accept invoice ID, approver username, and
  `ApprovalRole`; enforce role-specific limits and Admin refusal before mutation/update.
- Rename the demo Approver role to Manager, configure the four users specified in the
  feature, and seed pending invoices for 12,500, 72,500, and 650,000.
- Restrict the approvals controller to Manager, Director, and Admin; convert the role from
  the signed-in `UserSession` and pass it to the Domain. Keep the existing conflict response
  shape `{ error: reason }`.
- Add focused tests for policy limits, boundary values, Admin refusal, refusal reasons,
  unchanged invoice fields, and session-derived role mapping.

### Phase 2: Two-Approver Rule

- Add `ApprovalRecord(string Approver, ApprovalRole Role, DateTime At)` and initialize
  `Invoice.Approvals` as a list of records.
- For invoices above 500,000, record the first approval by either Manager or Director and
  keep the invoice PendingApproval. On the second approval, require a Director different
  from the first approver; compare usernames case-insensitively.
- Run all approval validations before any invoice field mutation or repository update.
- Add tests for first approvals by Manager and Director, second approval by a different
  Director, rejected second approval by a non-Director, same-user repeat including username
  casing, state snapshots for every refusal, and repository update behavior.
- Show "1 of 2 approvals" for a pending invoice above 500,000 with one recorded approval.

### Follow-up: Browser Refusal Coverage

- Add a Playwright test that signs in as `demo.manager`, attempts an over-limit approval,
  and verifies the clear refusal reason is shown in the existing `#decision-error` alert.
- Use a page object and accessible role/label locators.

## Constitution Check (Post-Design)

- **Domain-Owned Business Rules**: PASS. `ApprovalPolicy` and `ApprovalService` remain
  Domain-only; the API translates session identity and does not decide the limit.
- **Rule-Complete Unit Tests**: PASS. Every approval rule and refusal path has targeted
  unit-test coverage; the UI refusal has browser coverage.
- **State-Preserving Refusals**: PASS. Validation precedes mutation and repository update;
  tests compare complete invoice snapshots and verify no update on refusal.
- **Server-Authoritative Roles**: PASS. The role passed to the Domain comes from the
  authenticated session; the request contains no approval role.
- **Delivery Gates**: PASS. Validate solution build/tests and Playwright; keep each
  implementation commit independently green and explain its purpose.
- **End-to-End Tests**: PASS. Use page objects and accessible role-based locators only.

**Post-design gate**: PASS. No constitution violations or unjustified complexity.

## Complexity Tracking

No constitution violations. The new approval record list is required to represent the
specified two-approver workflow; no additional project or storage layer is introduced.
