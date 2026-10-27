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
approval eligibility and the high-value workflow.

## Validate domain and API behavior

From the repository root:

```powershell
dotnet test backend/InvoiceApp.sln
```

The xUnit tests cover role limits and exact boundaries, Admin refusal, invariant amount
reasons, complete invoice snapshots after refusals, distinct approvers with case-insensitive
usernames, the first and second approval stages, and rejection without an amount limit.

For approval API shapes and refusal statuses, see [contracts/approvals.md](contracts/approvals.md).
For entities and state invariants, see [data-model.md](data-model.md).

## Validate the browser refusal flow

From the repository root:

```powershell
Set-Location e2e
npm test
```

The Playwright configuration starts the API application at `http://localhost:5000`.
The over-limit approval scenario signs in as `demo.manager`, attempts approval of an invoice
above 50,000, and verifies that the role-specific refusal reason appears in the decision
error alert while the invoice remains pending.

## Expected outcomes

- A Manager can approve 50,000 but is refused at 50,000.01.
- A Director can approve 500,000; at 500,000.01 the first approval leaves the invoice pending
  with "1 of 2 approvals".
- An invoice above 500,000 is approved only after a distinct Director records the second
  approval.
- Admin approval is refused with `Admin has no approval authority`.
- Refused approvals do not change invoice state or persist an update.
- Rejection is not blocked by invoice amount.
