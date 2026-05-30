---
title: Getting started
description: Set up an Arc backend and build your first command, event, read model, and query.
---

Get an Arc backend up and building features. The fastest path is to scaffold from a template (`dotnet new cratis`), which wires Arc and Chronicle together for you — see the [Chronicle getting started](/chronicle/get-started/) for the scaffold step.

Once you have a project:

- **[Your first command and query](./your-first-command.md)** — build a backend slice end to end: a command with `Handle()`, the event it records, the read model a projection builds, and the query that serves it.
- **[Cratis Package](./cratis-package.md)** — the simplified setup that brings Arc and Chronicle event sourcing into an application.

When the backend compiles, it generates the TypeScript proxies your frontend consumes — continue with the [frontend getting started](/arc/frontend/getting-started/) or see the whole stack in [Build a full-stack feature](/build-a-full-app/).
