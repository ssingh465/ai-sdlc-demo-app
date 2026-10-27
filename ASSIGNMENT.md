# AI in SDLC — Approval limits by role

## Summary
In three lines, summarize what was built and its purpose.
Describe the role-based approval limits and approval workflow.
Mention the key outcome or user benefit.

## Demo video
{{VIDEO_LINK}}

## How the agent was set up
### Context (AGENTS.md)
Describe what was added to `AGENTS.md` and compare the agent's before-and-after answers.

### Skills
List the `.github/skills` entries and briefly explain what each is for.

### Agents and sub-agents
Describe spec, implementer (calls test-reviewer as a sub-agent), test-reviewer, and pr-reviewer.

### Hooks
Describe the logging hook and the `dotnet format` hook.

## Specification (Spec Kit)
Identify where the files are (`specs/...`) and explain how they were produced.

## Implementation steps
Add one line per commit explaining what changed and why.

## Test strategy
Describe the unit tests per rule, the two negative rules, the boundary cases, and the Playwright test.

## Mutation testing
| | Score |
|---|---|
| Before | {{MUTATION_BEFORE}} |
| After  | {{MUTATION_AFTER}} |

## Review findings
Link to the PR and report the counts of fixed, dismissed, and deferred findings.

## AI interaction log
Summarize the event count in `logs/ai-interactions.jsonl` and the chat exports in `logs/chat-exports/`.

## What I would do differently
Reflect briefly on what you would change in your approach next time.
