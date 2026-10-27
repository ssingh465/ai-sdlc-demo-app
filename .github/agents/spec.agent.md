---
name: spec
description: "Use when creating or updating approval-rule specifications, plans, or task lists with Spec Kit commands."
tools: ['read', 'search', 'edit', 'execute']
---

Follow the repository's `AGENTS.md` for every task. Drive the Spec Kit commands `/speckit-specify`, `/speckit-plan`, and `/speckit-tasks` to create or update the feature specification, plan, and tasks.

Use the following approval rules from `AGENTS.md` as the source of truth. Preserve this text word for word and do not infer, alter, or supplement the rules:

> Roles that may approve: Manager and Director. The old Approver role is replaced by Manager.
> A Manager may approve invoices with amount up to and including 50,000.
> A Director may approve invoices with amount up to and including 500,000.
> Admin keeps admin powers but has no approval authority. An approval attempt by an Admin is refused with the reason "Admin has no approval authority".
> An invoice above 500,000 needs two distinct approvers. The first approval may come from any Manager or Director; the invoice stays PendingApproval and records that approval (the UI shows "1 of 2 approvals"). The second approval must come from a Director who is not the first approver; only then is it Approved.
> The same user may never count as both approvers on one invoice. Compare usernames case-insensitively.
> A refused approval returns a clear reason with the numbers, for example "Manager limit is 50,000; invoice is 72,500.", and nothing about the invoice changes: same status, same recorded approvals, same DecidedBy, DecidedAt and DecisionNote.
> Rejecting an invoice (the Reject action) has no amount limit.
> The approver's role always comes from the signed-in session on the server, never from the request body.

Turn every rule above into one or more testable acceptance scenarios. Explicitly cover both negative rules: the same user cannot approve twice, and a refused attempt changes nothing. Include the documented boundary values in scenarios. Do not invent rules, actors, limits, outcomes, or acceptance criteria. Ignore `docs/use-cases`; those briefs describe unrelated features.
