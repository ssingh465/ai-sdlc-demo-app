---
name: mutation-testing
description: "Run pinned Stryker.NET mutation tests for the Domain project, report score changes, and strengthen tests against surviving approval-rule mutants."
---

# Mutation Testing

Use to measure whether tests detect defects, especially in approval rules.

1. Install the pinned tool once:

   ```powershell
   dotnet tool install -g dotnet-stryker --version 4.13.0
   ```

2. From `backend/tests/InvoiceApp.Tests`, run:

   ```powershell
   dotnet stryker --project InvoiceApp.Domain.csproj
   ```

3. Find the mutation score in the console summary and in `StrykerOutput/**/reports/mutation-report.html`.
4. Record the `before` and `after` scores as text in `ASSIGNMENT.md`.
5. Never commit `StrykerOutput/`. Check `.gitignore`; add `StrykerOutput/` there if it is missing.
6. For every surviving mutant in approval code, identify the behavior it changes and propose a focused test that would kill it. Add and run that test before claiming the mutant is addressed.
