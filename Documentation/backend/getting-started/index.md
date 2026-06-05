---
title: Getting started
description: Set up an Arc backend and build your first command, read model, and live query over a plain database.
---

Get an Arc backend up and building features. Start with a plain database — MongoDB or EF Core — and use Arc for the application model: commands with `Handle()`, read models with query methods, validation, authorization, and generated TypeScript proxies.

Once you have a project:

- **[Your first command and query](./your-first-command.md)** — build a backend slice end to end: a command with `Handle()`, the read model it writes, and the live query that serves it.
- **[MongoDB integration](../mongodb/index.md)** — configure Arc over MongoDB collections and observable change streams.
- **[Entity Framework integration](../entity-framework/getting-started.md)** — configure Arc over DbContexts and observed DbSets.

When the backend compiles, it generates the TypeScript proxies your frontend consumes — continue with the [frontend getting started](/arc/frontend/getting-started/) or build the full database-backed path in the [Arc tutorial](/arc/tutorial/).

If your slice later needs an event log, the [Chronicle integration](../chronicle/index.md) is there as an optional next step, not a prerequisite.
