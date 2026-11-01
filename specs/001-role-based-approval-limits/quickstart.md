# Quickstart: Role-Based Approval Limits

## Prerequisites

- .NET 8 SDK
- Node.js and npm
- Chromium installed for Playwright

## Run the application

From the repository root in PowerShell:

```powershell
dotnet run --project backend/src/InvoiceApp.Api
```

Open `http://localhost:5000` and sign in with one of the configured demo accounts:

- `demo.manager` (Manager)
- `demo.director` (Director)
- `demo.director2` (Director)
- `demo.admin` (Admin)

Use the repository's demo-only password configuration; do not put real credentials into
source, tests, or prompts.

The pending invoice seeds include amounts 12,500, 72,500, and 650,000 to exercise ordinary
approval eligibility and role limits. During this phase, amounts above the approver's limit
are refused; the two-approver workflow for invoices above 500,000 is not implemented yet.

## Validate domain and API behavior

From the repository root:

```powershell
dotnet test backend/InvoiceApp.sln
```

The xUnit tests cover role limits and exact boundaries, Admin refusal, invariant amount
reasons, complete invoice snapshots after refusals, session-derived controller roles, and
rejection without an amount limit.

For approval API shapes and refusal statuses, see [contracts/approvals.md](contracts/approvals.md).
For entities and state invariants, see [data-model.md](data-model.md).

## Expected outcomes

- A Manager can approve 50,000 but is refused at 50,000.01.
- A Director can approve 500,000 but is refused at 500,000.01.
- Admin approval is refused with `Admin has no approval authority`.
- Refused approvals do not change invoice state or persist an update.
- Rejection is not blocked by invoice amount.
