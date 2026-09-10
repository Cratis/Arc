---
title: Transactional commands — optional Chronicle integration
description: Compatibility link to Chronicle's event-store-specific command transactions.
---

Command transactions described on this route belong to the optional **`Cratis.Arc.Chronicle`** integration, not standalone Arc.

Read [Chronicle transactional commands](../chronicle/commands/transactional-commands.md) for its commit, rollback, and immediate-append behavior. This compatibility page preserves the previous command-documentation route.

Standalone Arc handlers can call application services and return ordinary values without Chronicle. Arc does not automatically begin a database transaction or roll back arbitrary service writes. For your own lifetime coordination, see [command execution scopes](./command-execution-scopes.md).

:::note[Inline work does not require Chronicle]
Direct service calls remain supported. Prefer returned [command operations](./operations/index.md) for inline work chosen by a model-bound command. Arc executes them and manages eligible, explicitly supplied compensation—not automatic database rollback. Execution scopes remain the integration point for actual transaction coordination.
:::

## Choosing an append style

Continue to [Chronicle append styles](../chronicle/commands/transactional-commands.md#choosing-an-append-style).

## How it works

Continue to [Chronicle transaction completion](../chronicle/commands/transactional-commands.md#completion).

## Nested commands and aggregates

Continue to [nested commands and aggregate commit boundaries](../chronicle/commands/transactional-commands.md#nested-commands-and-aggregates).

## Things to know

Continue to [Chronicle transaction limitations](../chronicle/commands/transactional-commands.md#things-to-know).
