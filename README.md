# Invoice Approval Demo App

A small invoice approval app used for live AI-in-SDLC demos. It is deliberately simple: a .NET 8 API, a plain web UI, and an in-memory store, so it runs with one command and no database.

## Run it

```
dotnet run --project backend/src/InvoiceApp.Api
```

Open http://localhost:5000 and sign in:

| User | Password | Role |
| --- | --- | --- |
| `demo.manager` | `ChangeMe123!` | Manager |
| `demo.director` | `ChangeMe123!` | Director |
| `demo.director2` | `ChangeMe123!` | Director |
| `demo.admin` | `ChangeMe123!` | Admin |

These are demo accounts for a local, in-memory app. They are not real credentials.

## What it does

1. Create an invoice (it starts as a draft).
2. Submit it for approval.
3. A Manager can approve invoices up to 50,000, and a Director can approve invoices up to 500,000. Invoices above 500,000 need two different people, with a Director approving second; until then, the UI shows "1 of 2 approvals". Admins cannot approve invoices. If an approval is refused, nothing about the invoice changes. An invoice can also be rejected with a reason, regardless of amount.

Invoices move through Draft, Pending Approval, then Approved or Rejected.

## Tests

```
dotnet test backend/InvoiceApp.sln
```

For end-to-end tests:

```
cd e2e; npm install; npx playwright install chromium; npm test
```

For mutation testing with Stryker.NET (install once, then run from `backend/tests/InvoiceApp.Tests`):

```
dotnet tool install -g dotnet-stryker --version 4.13.0
cd backend/tests/InvoiceApp.Tests
dotnet stryker --project InvoiceApp.Domain.csproj
```

## Project layout

```
backend/
  src/InvoiceApp.Domain   business rules
  src/InvoiceApp.Api      controllers, sign-in, web UI (wwwroot)
  tests/InvoiceApp.Tests  xUnit tests
e2e/                      Playwright end-to-end tests
specs/                    feature specifications
.github/                  skills, agents, hooks
logs/                     AI interaction logs and chat exports
docs/use-cases            short feature briefs
ASSIGNMENT.md             assignment brief
AGENTS.md                 conventions for AI coding agents
```

## Use cases

- [UC-1 Bulk invoice approval](docs/use-cases/UC-1-bulk-approval.md)
- [UC-2 Partial approval](docs/use-cases/UC-2-partial-approval.md)
