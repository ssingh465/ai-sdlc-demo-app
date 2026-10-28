---
name: interaction-log
description: "Understand the repository's AI interaction hooks, export chats, and summarize interaction events for ASSIGNMENT.md."
---

# AI Interaction Log

Use when auditing AI-assisted work or preparing the interaction summary for `ASSIGNMENT.md`.

- Hooks configured in `.github/hooks/ai-logging.json` call scripts in `.github/hooks/scripts/` for session, prompt, tool, and agent lifecycle events. The logger appends JSON Lines records to `logs/ai-interactions.jsonl`.
- Each line is a JSON object with the keys `ts`, `sessionId`, `event`, `agent`, `prompt`, `tool`, `input`, `output`, and `files`.
- Export a chat from the Command Palette: choose **Chat: Export Chat...** and save it as `logs/chat-exports/<step>.json`.
- Never put secrets in prompts, logs, or chat exports.
- To summarize event counts in PowerShell for `ASSIGNMENT.md`, run:

  ```powershell
  Get-Content logs/ai-interactions.jsonl | ConvertFrom-Json | Group-Object event | Select-Object Name,Count
  ```
