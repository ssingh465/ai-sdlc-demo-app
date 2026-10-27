# Tasks: Role-Based Approval Limits

**Input**: Design documents from `/specs/001-role-based-approval-limits/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`,
`contracts/approvals.md`, `quickstart.md`

**Organization**: Tasks are grouped into the three requested delivery phases. Phase 1
covers single-approver limits and demo setup; Phase 2 adds the high-value two-approver
workflow; Phase 3 adds Playwright coverage. The existing .NET solution, in-memory
repository, authentication/session middleware, and Playwright project are reused.

## Format

Every task follows `- [ ] T### [P?] [US#] Description with exact file path(s)`. `[P]` is
used only when the task can proceed independently on different files. Story labels refer
to the user stories in `spec.md`.

## Phase 1: Single-Approver Limits and Demo Setup

**Goal**: Establish Manager and Director limits, Admin refusal, authoritative session role,
refusal state preservation, demo roles, and the specified pending invoice seeds.

**Independent Test**: `dotnet test backend/InvoiceApp.sln` passes with the approval-role
boundaries, exact refusal reasons, unchanged invoice snapshots, demo account roles, and
amount-unlimited rejection covered.

### Tests for Phase 1

- [ ] T001 [P] [US1] Add `ApprovalPolicyTests` for Manager limit 50,000, Director limit 500,000, and Admin null limit in `backend/tests/InvoiceApp.Tests/ApprovalPolicyTests.cs`.
- [ ] T002 [P] [US1] Update `ApprovalServiceTests` for permitted limits, 50,000 and 50,000.01 boundaries, the 500,000 Director boundary, invariant formatted refusal reasons, Admin refusal, complete unchanged invoice snapshots on refusals, and rejection of an invoice above 500,000 without an amount limit in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`.
- [ ] T003 [US3] Add a project reference to the API project so authenticated-session controller behavior can be unit tested in `backend/tests/InvoiceApp.Tests/InvoiceApp.Tests.csproj`.
- [ ] T004 [US3] Add `ApprovalsControllerTests` proving the controller uses the role on `UserSession` for approval decisions and refuses an Admin session; verify the action accepts no client-supplied role in `backend/tests/InvoiceApp.Tests/ApprovalsControllerTests.cs`.

### Implementation for Phase 1

- [ ] T005 [P] [US1] Define `ApprovalRole { Manager, Director, Admin }` in `backend/src/InvoiceApp.Domain/ApprovalRole.cs`.
- [ ] T006 [US1] Implement `ApprovalPolicy.LimitFor(ApprovalRole role)` with 50,000 for Manager, 500,000 for Director, and null for Admin in `backend/src/InvoiceApp.Domain/ApprovalPolicy.cs`.
- [ ] T007 [P] [US1] Add `ApprovalRefusedException : InvalidOperationException` in `backend/src/InvoiceApp.Domain/ApprovalRefusedException.cs`.
- [ ] T008 [P] [US1] Track repository update calls in `FakeInvoiceRepository` so refusal tests assert no persistence in `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`.
- [ ] T009 [US1] Change `ApprovalService.Approve` to accept invoice ID, approver, and `ApprovalRole`; check pending status, Admin authority, and applicable limits before any invoice mutation or repository update, formatting amount reasons with `#,##0.##` and invariant culture in `backend/src/InvoiceApp.Domain/ApprovalService.cs`.
- [ ] T010 [US3] Restrict approvals to Manager, Director, and Admin and map `HttpContext.GetSession().Role` to `ApprovalRole` before calling the service; do not read approval role from the request body in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`.
- [ ] T011 [P] [US3] Configure `demo.manager` as Manager, `demo.director` and `demo.director2` as Director, and `demo.admin` as Admin in `backend/src/InvoiceApp.Api/appsettings.json`.
- [ ] T012 [P] [US3] Seed pending invoices for 12,500, 72,500, and 650,000 in `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs`.
- [ ] T013 [US1] Run `dotnet test backend/InvoiceApp.sln` and resolve failures before completing Phase 1; record the verification against `backend/InvoiceApp.sln`.

**Phase 1 checkpoint**: Solution tests pass; single-approver role limits, Admin refusal,
session-derived roles, refusal immutability, rejection, and demo configuration are covered.

## Phase 2: Two-Approver Rule

**Goal**: Record high-value invoice approvals, keep the first approval pending, and approve
only after a distinct Director records the second approval.

**Independent Test**: `dotnet test backend/InvoiceApp.sln` passes for Manager or Director
first approvals, second Director approval, casing-insensitive same-user refusal, and
unchanged invoice state for every refusal. The pending UI is verified in Phase 3.

### Tests for Phase 2

- [ ] T014 [US2] Add service tests for first approval by Manager and Director, second approval by a distinct Director, refusal of a non-Director second approver, same-user repeat with different username casing, and complete unchanged invoice snapshots plus no repository update on each refusal in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`.

### Implementation for Phase 2

- [ ] T015 [US2] Add immutable `ApprovalRecord(string Approver, ApprovalRole Role, DateTime At)` in `backend/src/InvoiceApp.Domain/ApprovalRecord.cs`.
- [ ] T016 [US2] Add an initialized `List<ApprovalRecord>` named `Approvals` to `Invoice` in `backend/src/InvoiceApp.Domain/Invoice.cs`.
- [ ] T017 [US2] Implement high-value approval validation and transitions: first approval by Manager or Director records one approval and leaves PendingApproval; second approval requires a Director with a case-insensitively distinct username; validate all refusal conditions before changing fields or updating the repository in `backend/src/InvoiceApp.Domain/ApprovalService.cs`.
- [ ] T018 [US2] Display "1 of 2 approvals" for a pending invoice above 500,000 with exactly one approval in `backend/src/InvoiceApp.Api/wwwroot/app.js`.
- [ ] T019 [US2] Run `dotnet test backend/InvoiceApp.sln` and resolve failures before completing Phase 2; record the verification against `backend/InvoiceApp.sln`.

**Phase 2 checkpoint**: Solution tests pass; the high-value workflow records distinct
approvers and keeps first-approval invoices pending with the required UI text.

## Phase 3: Playwright Over-Limit Approval Test

**Goal**: Verify the user-visible Manager over-limit refusal and confirm that the API uses
the signed-in Admin role rather than a client-supplied Director role.

**Independent Test**: Playwright signs in as `demo.manager`, attempts to approve the seeded
72,500 invoice, and observes the exact reason in the decision alert. Additional browser
scenarios verify the "1 of 2 approvals" display and prove a client-supplied Director role
cannot override the signed-in Admin role.

### Tests and page objects for Phase 3

- [ ] T020 [US3] Create a page object using accessible role and label locators for demo sign-in, opening pending approvals, attempting an invoice approval, and reading the decision alert in `e2e/pages/approvals-page.ts`.
- [ ] T021 [US3] Add Playwright tests for all four configured demo-user roles, the seeded 72,500 Manager refusal, the 650,000 first approval displaying "1 of 2 approvals", and Admin role spoof refusal; assert exact messages and pending status through accessible locators and authenticated API requests in `e2e/tests/approval-limits.spec.ts`.
- [ ] T022 [US3] Run `dotnet test backend/InvoiceApp.sln` and `npm test` from `e2e/`; resolve failures before completing Phase 3 and verify the corresponding commands and outcomes in `specs/001-role-based-approval-limits/quickstart.md`.

**Phase 3 checkpoint**: Domain/API tests and Playwright tests pass; the over-limit reason is
visible to the user and a client-supplied role cannot elevate approval authority.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No dependency on feature implementation; delivers single-approver rules and
  demo configuration.
- **Phase 2**: Depends on Phase 1's role model, approval service signature, and passing
  solution tests.
- **Phase 3**: Depends on Phases 1 and 2, the configured demo accounts and seeds, and the
  final pending-approval UI.

### User Story Dependencies

- **US1 (P1)**: Delivered in Phase 1. Manager and Director limits and refusal behavior are
  independently verified by the xUnit suite.
- **US2 (P1)**: Delivered in Phase 2. Depends on the Domain role/service foundations from
  Phase 1.
- **US3 (P1)**: Admin/demo/session/rejection requirements are implemented and covered in
  Phase 1; browser-visible refusal and role-source behavior are verified in Phase 3.

### Within Each Phase

- Write or extend the phase-specific tests before its implementation and verify the tests
  fail for the missing rule.
- Implement Domain behavior before wiring or displaying it in dependent layers.
- Run the phase's specified test command(s) and resolve all failures before proceeding.

## Parallel Opportunities

- **Phase 1**: T001 (policy tests) and T002 (service behavior tests) touch different files
  and can be drafted in parallel. T005 (role enum), T007 (refusal exception), and T008
  (fake-repository tracking) are separate files and can be prepared in parallel; T006
  depends on T005. T011 (demo user configuration) and T012 (invoice seeds) touch different
  files and can be changed in parallel.
- **Phase 2**: Tests and implementation use the same service test/source files in places;
  keep dependencies sequential: T014 tests, T015 record, T016 invoice property, T017
  service, then T018 UI. No task is marked parallel in this phase.
- **Phase 3**: T020 and T021 share the new page object and test workflow, so implement the
  page object first, then the Playwright tests; they are not parallel tasks.

## Implementation Strategy

1. Complete Phase 1 and stop at its passing xUnit checkpoint. This is the MVP: role-limited
   single approvals, Admin refusal, correct demo roles, and no state changes on refusal.
2. Complete Phase 2 and stop at its passing xUnit checkpoint. This adds the distinct
   two-approver workflow and its pending UI status.
3. Complete Phase 3 and run both solution tests and Playwright. This verifies the refused
   over-limit experience and session-authoritative Admin role end to end.
