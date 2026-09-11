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

This repository uses the managed Cratis AI corpus:

- `.cratis/ai.json` selects `cratis/engineering/csharp` and `cratis/documentation` for every supported harness.
- This file is project-owned guidance shared by Claude, Codex, Copilot, Cursor, OpenCode, and Pi.
- Cratis-managed rules, agents, prompts, hooks, and the selected skills live beside it under `.cratis/ai`; `.cratis/ai.manifest.json` records only the files owned by Cratis.
- Add Arc-specific guidance to this file or another user-owned rule under `.cratis/ai/rules`. Managed update and uninstall preserve those files.

General reusable improvements belong in [`Cratis/AI`](https://github.com/Cratis/AI). AI session work records stay in the ignored `.ai-work/` directory; durable follow-up work belongs in GitHub issues.
