# AI in SDLC — Approval limits by role

## Summary
Managers can approve invoices up to 50,000; Directors up to 500,000.
Invoices above 500,000 need approvals from two distinct people, with the second approver a Director.
Approval rules are enforced in the Domain, and refused approvals leave the invoice unchanged.

## Demo video
{{VIDEO_LINK}}

## How the agent was set up
### Context (AGENTS.md)
`AGENTS.md` gained the role limits, two-approver rule, Admin restrictions, unchanged-on-refusal rule, server-derived role and Domain-layering guidance, plus required test cases and boundaries.

Question asked twice: once on upstream `main` without the new AGENTS.md context, once on the feature branch with it:

> 1. Can a Manager approve an invoice of 72,500? 2. Who has to approve an invoice of 600,000, and can one person do both approvals? 3. If an approval is refused, what happens to the invoice? 4. In which project must these rules be implemented?

**Before:** No Manager role or limit exists; an unrelated UC-1 rule says a $1,000 invoice needs a second approver; whether one person can do both approvals is open; a refused invoice becomes Rejected with a note; the rules belong in `InvoiceApp.Domain`. Answers 1–3 were wrong or borrowed from an unrelated use case; only 4 was right.

**After:** No; a Manager's limit is 50,000. An invoice of 600,000 needs two distinct people: first a Manager or Director, then a different Director. A refused approval leaves the invoice unchanged. The rules belong in `InvoiceApp.Domain`. All four answers are correct.

### Skills
- `.github/skills/approval-rule-tests/SKILL.md` — write approval-rule unit tests.
- `.github/skills/commit-message/SKILL.md` — prepare project-convention commits.
- `.github/skills/interaction-log/SKILL.md` — summarize AI interaction hooks and exports.
- `.github/skills/mutation-testing/SKILL.md` — run and improve pinned Stryker.NET checks.
- `.github/skills/playwright-e2e/SKILL.md` — write and run Playwright approval-flow tests.
- `.github/skills/speckit-analyze/SKILL.md`, `.github/skills/speckit-checklist/SKILL.md`, `.github/skills/speckit-clarify/SKILL.md`, `.github/skills/speckit-converge/SKILL.md`, `.github/skills/speckit-constitution/SKILL.md`, `.github/skills/speckit-implement/SKILL.md`, `.github/skills/speckit-plan/SKILL.md`, `.github/skills/speckit-specify/SKILL.md`, `.github/skills/speckit-tasks/SKILL.md`, `.github/skills/speckit-taskstoissues/SKILL.md` — Spec Kit specification, planning, task, and analysis workflows.

### Agents and sub-agents
- `.github/agents/implementer.agent.md` — implements a task slice; calls the test reviewer as a sub-agent.
- `.github/agents/pr-reviewer.agent.md` — reviews the assignment pull request.
- `.github/agents/spec.agent.md` — works with feature specifications.
- `.github/agents/test-reviewer.agent.md` — checks whether approval-rule tests catch broken rules.

### Hooks
- `.github/hooks/ai-logging.json` — configures interaction logging.
- `.github/hooks/format-dotnet.json` — configures the `dotnet format` hook.
- `.github/hooks/scripts/log-interaction.mjs` — appends interaction events to the log.
- `.github/hooks/scripts/format-dotnet.mjs` — runs .NET formatting.
- `.github/hooks/scripts/edited-files.mjs` — supplies edited-file information to the hooks.

## Specification (Spec Kit)
The feature artifacts are in `specs/001-role-based-approval-limits/` (specification, plan, tasks, research, quickstart, data model, approval contract, and requirements checklist). The project principles are in `.specify/memory/constitution.md`. Spec Kit produced the feature artifacts through its specify, plan, and tasks workflows.

## Implementation steps
- `3da9bf5 chore: add Copilot setup and Spec Kit initialisation` — added the repository setup needed to guide AI work and create feature artifacts.
- `c50868c docs(spec): add Spec Kit spec, plan and tasks for approval limits` — defined the feature and implementation work before coding.
- `6353816 feat: enforce single-approver limits by role` — added role-based approval limits and refusals.
- `558fa29 feat: require two distinct approvers above 500,000` — added the two-person approval flow for high-value invoices.
- `c66d577 test(e2e): cover refused over-limit approval in the UI` — checked that a Manager's over-limit attempt is refused in the UI.
- `6f3b7a2 test(e2e): verify unchanged invoice from fresh API reads` — verified the refused approval leaves persisted invoice data unchanged.
- `15089cc test: kill surviving mutants in submit and reject` — strengthened tests for Submit and Reject behavior.
- `5f700f4 fix: address PR review findings` — resolved review findings and corrected related implementation and tests.

## Test strategy
Unit tests group by rule:

- **Role limits and boundaries:** `LimitFor_Manager_Returns50000`, `LimitFor_Director_Returns500000`, `TwoApproverThreshold_ReturnsDirectorLimit`, `LimitFor_Admin_ReturnsNull`, `Manager_CanApprove_AtExactly50000`, `Manager_IsRefused_At50000_01_AndInvoiceUnchanged`, `Director_CanApprove_72500`, `Director_CanApprove_AtExactly500000`, and `Above500000_At500000_01_DirectorApproval_IsFirstOnly_StaysPending`.
- **Admin cannot approve:** `Admin_IsRefused_WithNoApprovalAuthority_AndInvoiceUnchanged`, `Above500000_AdminAsSecond_IsRefused_AndInvoiceUnchanged`.
- **Two approvals above 500,000:** `Above500000_FirstApprovalByManager_StaysPendingWithOneApproval`, `Above500000_FirstApprovalByDirector_StaysPendingWithOneApproval`, `Above500000_SecondApprovalByDifferentDirector_Approves`, `AtOrBelow500000_Approval_RecordsExactlyOneApprovalRecord`.
- **Second-approver restrictions:** `Above500000_SecondApprovalByManager_IsRefused_AndInvoiceUnchanged`, `Above500000_SameDirectorTwice_IsRefused_AndInvoiceUnchanged`, `Above500000_SameUserDifferentCasing_IsRefused_AndInvoiceUnchanged`, `Above500000_ManagerFirstThenSameManagerAgain_IsRefused_AndInvoiceUnchanged`. These cover the same-user-twice and case-insensitive negative rules.
- **Refusal and rejection:** `RefusalMessage_ContainsRoleLimitAndAmount`, `Reject_HasNoAmountLimit`. Refusal tests assert that the invoice remains unchanged.
- **Role comes from the signed-in session:** `Approve_UsesRoleFromSession`, `Approve_AllowsDirectorSessionWithinDirectorLimit`, `Approve_RefusesAdminSession`, `Approve_AcceptsNoClientSuppliedRole`.

Playwright test: `a manager's over-limit approval is refused with a reason and the invoice stays pending` in `e2e/tests/over-limit-approval.spec.ts`; it checks the refusal message, pending status, and invoice data from a fresh read.

## Mutation testing
| | Score |
|---|---|
| Before | 30.77% |
| After  | 68.18% |

With Stryker.NET 4.13.0 on `InvoiceApp.Domain`, the before run killed 12 mutants, with 10 surviving and 17 not covered. The after run killed 45, with 2 surviving and 19 not covered. `ApprovalService` killed all 45 of its mutants. The remaining mutants are in the untouched `DiscountCalculator`, two `Invoice` initialisers, and two unreachable defensive messages in `ApprovalPolicy`. An intermediate 66.15% run had two survivors in Submit and Reject; commit `15089cc` killed them.

## Review findings
[PR #1](https://github.com/ssingh465/ai-sdlc-demo-app/pull/1): 3 fixed, 1 dismissed, 0 deferred. The pr-reviewer raised two findings; one (that Reject should be Admin-only) was dismissed as a false positive. I found two more that it missed. The full table with verdicts is in the PR description.

## AI interaction log
`.github/hooks/ai-logging.json` configures hooks that append interaction events to `logs/ai-interactions.jsonl`. Chat exports are saved in `logs/chat-exports/`; this repository contains `01-setup.json`, `04-before.json`, and `10-review.json`.

Output of `Get-Content logs/ai-interactions.jsonl | ConvertFrom-Json | Group-Object event | Select-Object Name,Count`:

```text
Name                Count
----                -----
userPromptSubmitted    41
sessionStart           26
preToolUse            587
postToolUse           583
agentStop              38
sessionEnd             32
subagentStart           5
subagentStop            8
```

## What I would do differently
- Give the pr-reviewer a checklist item for duplicated constants and page-object-only e2e tests; it missed both.
- Have the logging hook also capture the agent's final reply text; the review table had to be exported by hand.
- Ask Spec Kit for the single e2e test from the start; its tasks planned more tests and had to be narrowed.
- Avoid unreachable defensive branches in `ApprovalPolicy`; they add mutants no test can kill.
- State "one commit only" in commit prompts to keep the history tighter; the implementer added an extra e2e commit.
