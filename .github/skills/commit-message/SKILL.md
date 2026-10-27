---
name: commit-message
description: "Prepare clear, standalone commits with the project's required type, change summary, rationale, and passing build and test checks."
---

# Commit Messages

Use when creating or reviewing a commit.

- Format the subject as `<type>: <what changed>`. Use one of `feat`, `fix`, `test`, `docs`, or `chore`.
- Add a blank line after the subject, then a body explaining why the change was made.
- Keep each commit standalone: it must build and pass `dotnet test backend/InvoiceApp.sln`.
- Run the build and test checks for the commit's changes before committing; do not rely on a later commit to make it pass.
