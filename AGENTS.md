# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only; approvals allow Manager, Director or Admin (limits are decided in the Domain; see Approval rules).
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`
- On Windows PowerShell 5.1, chain commands with `;` instead of `&&` (for example `cd e2e; npm test`).
- Mutation testing (Stryker.NET, pinned): install once with `dotnet tool install -g dotnet-stryker --version 4.13.0`, then run from `backend/tests/InvoiceApp.Tests`: `dotnet stryker --project InvoiceApp.Domain.csproj`. Do not upgrade Stryker; newer versions fail on .NET 8.

## Demo accounts (not real credentials)

- `demo.manager` / `ChangeMe123!` (role Manager)
- `demo.director` / `ChangeMe123!` (role Director)
- `demo.director2` / `ChangeMe123!` (role Director)
- `demo.admin` / `ChangeMe123!` (role Admin)

## Approval rules

- Roles that may approve: Manager and Director. The old Approver role is replaced by Manager.
- A Manager may approve invoices with amount up to and including 50,000.
- A Director may approve invoices with amount up to and including 500,000.
- Admin keeps admin powers but has no approval authority. An approval attempt by an Admin is refused with the reason "Admin has no approval authority".
- An invoice above 500,000 needs two distinct approvers. The first approval may come from any Manager or Director; the invoice stays PendingApproval and records that approval (the UI shows "1 of 2 approvals"). The second approval must come from a Director who is not the first approver; only then is it Approved.
- The same user may never count as both approvers on one invoice. Compare usernames case-insensitively.
- A refused approval returns a clear reason with the numbers, for example "Manager limit is 50,000; invoice is 72,500.", and nothing about the invoice changes: same status, same recorded approvals, same DecidedBy, DecidedAt and DecisionNote.
- Rejecting an invoice (the Reject action) has no amount limit.
- The approver's role always comes from the signed-in session on the server, never from the request body.

## Layering for approval rules

- Limits and the two-approver logic live in backend/src/InvoiceApp.Domain. Controllers only pass the signed-in username and role to ApprovalService. The Domain project never references Api types.

## Tests for approval rules

- Every rule above has a unit test, including the two negative rules (same user twice; refused attempt changes nothing).
- Every refusal test snapshots ALL invoice properties (including recorded approvals) before the call and asserts each one is unchanged after it.
- Test the boundaries: 50,000 and 50,000.01; 500,000 and 500,000.01.

## Review expectations

- Every review finding gets exactly one verdict: fixed, dismissed with a reason, or deferred with a reason.
- A test only counts if it would fail when the rule it names is broken.

## Commits

- Each commit stands on its own (builds, tests pass) and its message says what changed and why.

## AI interaction log

- Hooks in .github/hooks append every prompt, tool call and agent stop to logs/ai-interactions.jsonl. Chat exports go in logs/chat-exports/. Never put secrets in prompts.

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.
