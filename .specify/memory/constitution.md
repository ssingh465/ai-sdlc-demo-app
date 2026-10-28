<!--
Sync Impact Report
Version change: unversioned template -> 1.0.0
Modified principles: None; this is the initial ratification.
Added sections: Delivery Gates; End-to-End Tests.
Removed sections: None.
Follow-up TODOs: None.
-->
# Invoice Approval Demo App Constitution

## Core Principles

### I. Domain-Owned Business Rules
Business rules MUST reside only in `InvoiceApp.Domain`; API controllers MUST NOT decide policy.
This keeps business decisions consistent across entry points.

### II. Rule-Complete Unit Tests
Every rule MUST have a unit test, including negative rules. A test counts only if it would fail
when the rule it names is broken. This makes tests evidence of behavior, not just execution.

### III. State-Preserving Refusals
A refused operation MUST leave domain state unchanged. This prevents failed requests from
partially applying a decision.

### IV. Server-Authoritative Roles
The server MUST derive the approver's role from the signed-in session and MUST NOT trust a role
supplied in the request body. This keeps authorization under server control.

## Delivery Gates

Every commit MUST build, pass tests, and explain what changed and why.

## End-to-End Tests

End-to-end tests MUST use page objects and role-based locators such as `getByRole` and
`getByLabel`; CSS and XPath selectors MUST NOT be used.

## Governance

Amendments MUST be made as reviewed changes to this constitution with a stated rationale.
Reviewers MUST verify compliance and require tests for changed rules. Versioning follows
semantic versioning: MAJOR for backward-incompatible governance changes, MINOR for new or
materially expanded principles or sections, and PATCH for clarifications and non-semantic
refinements.

**Version**: 1.0.0 | **Ratified**: 2026-10-27 | **Last Amended**: 2026-10-27
