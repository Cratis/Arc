---
applyTo: "**/*"
---

## AI-assisted development

This repository uses the managed Cratis AI corpus:

- `.cratis/ai.json` selects `cratis/engineering/csharp` and `cratis/documentation` for every supported harness.
- This file is project-owned guidance shared by Claude, Codex, Copilot, Cursor, OpenCode, and Pi.
- Cratis-managed rules, agents, prompts, hooks, and the selected skills live beside it under `.cratis/ai`; `.cratis/ai.manifest.json` records only the files owned by Cratis.
- Add Arc-specific guidance to this file or another user-owned rule under `.cratis/ai/rules`. Managed update and uninstall preserve those files.

General reusable improvements belong in [`Cratis/AI`](https://github.com/Cratis/AI). AI session work records stay in the ignored `.ai-work/` directory; durable follow-up work belongs in GitHub issues.
