---
name: playwright-e2e
description: "Write and run Playwright end-to-end tests for invoice approval flows using page objects and accessible role, label, and text locators."
---

# Playwright E2E Tests

Use for browser-level scenarios in `e2e/tests`; keep reusable UI actions in page objects under `e2e/pages`.

- Use only `getByRole`, `getByLabel`, and `getByText` for locating elements. Never use CSS selectors or XPath.
- Sign in with the demo accounts listed in the root `AGENTS.md`: `demo.manager`, `demo.director`, `demo.director2`, or `demo.admin`; the demo password is `ChangeMe123!`.
- For a refusal, assert the exact visible reason text and verify the invoice row is unchanged. Capture its text before the action, then assert the row still has that text.
- Put interaction helpers in page objects and keep assertions about scenario outcomes in the test.

Run the suite from PowerShell:

```powershell
cd e2e; npm test
```

Run the demo in a visible browser with:

```powershell
cd e2e; npm run test:headed
```
