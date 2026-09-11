# Arc — project context

Cratis Arc: the open-source (MIT) opinionated CQRS application framework for
ASP.NET Core — commands, queries, validation, authorization, and TypeScript
proxy generation. Works without event sourcing, with optional Chronicle
integration. A framework repository, not an application: do not apply
event-sourced application patterns here.

## What Arc owns

Application behavior hosting, command and query pipelines, recognized
contracts over HTTP, generated TypeScript clients, observable queries,
validation, identity and tenancy, React integration, current-state
persistence, OpenAPI, analyzers, and testing packages.

## Commands

```bash
dotnet build
dotnet test
yarn install && yarn build   # JavaScript/React packages
```

CI runs the .NET and JavaScript builds, package-graph verification, and
markdown verification. Releasing a package happens only through a labeled
merge to `main` (`major`/`minor`/`patch`).

## AI-assisted development

This repository uses the Cratis AI contract:

- **`.cratis/ai.json`** records the subscription — `cratis/documentation` plus the `cratis/engineering/csharp` maintainer cell.
- **`.cratis/PROJECT.md`** (this file) is the canonical project context; the root `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md` are minimal bootstraps that point here and do nothing else.
- There is **no local AI corpus and no generated tool adapters** in this repository. Shared skills arrive through the Cratis AI marketplace plugins (Claude Code, Codex, GitHub Copilot, Cursor, and Pi are installable today — see the [harness guide](https://www.cratis.io/ai/harnesses/)).

For contributors:

1. Install the Cratis plugin for your harness once (per the harness guide); the subscribed profiles' skills then load automatically when tasks match.
2. General, reusable improvements are proposed in [`Cratis/AI`](https://github.com/Cratis/AI) — never copied into, or synchronized from, this repository.
3. Repository-specific facts and conventions belong in this file; repository-local skills live under `.agents/skills/`.
4. AI session work records (plans, handovers, session notes, scratch analyses) stay in the untracked `.ai-work/` folder and never enter git; a durable follow-up becomes a GitHub issue.
