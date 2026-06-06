---
title: Getting started
description: Set up an Arc backend and build your first command, read model, and live query while learning the CQRS boundary.
---

Get an Arc backend up and building features. These first pages use a plain database — MongoDB or EF Core — so you can see Arc's CQRS model in isolation: commands with `Handle()`, read models with query methods, validation, authorization, and generated TypeScript proxies. In a full Cratis information system, the same boundary usually sits on Chronicle's event-sourced write side.

Once you have a project:

- **[Your first command and query](./your-first-command.md)** — build a backend slice end to end: a command with `Handle()`, the read model it writes, and the live query that serves it.
- **[MongoDB integration](../mongodb/index.md)** — configure Arc over MongoDB collections and observable change streams.
- **[Entity Framework integration](../entity-framework/getting-started.md)** — configure Arc over DbContexts and observed DbSets.

When the backend compiles, it generates the TypeScript proxies your frontend consumes — continue with the [frontend getting started](/arc/frontend/getting-started/) or build the full database-backed path in the [Arc tutorial](/arc/tutorial/).

When you are ready to put the event-sourced backbone underneath that boundary, continue with the [Chronicle integration](../chronicle/index.md).
