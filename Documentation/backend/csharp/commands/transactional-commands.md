---
title: Cratis Arc command transaction boundaries
description: Cratis Arc uses optional Chronicle integration for event transactions. Coordinate other service writes through command execution scopes.
---

Cratis Arc command transactions use the optional `Cratis.Arc.Chronicle` integration to commit pending enrolled events together or roll them back when a command fails. That boundary covers Chronicle events, not arbitrary database writes or external service calls.

If your command returns or appends events, follow [Chronicle transactional commands](../chronicle/commands/transactional-commands.md) to choose which appends enroll and when they commit. This page keeps the previous command-documentation route available.

A standalone Arc handler can call application services and return ordinary values without Chronicle. Arc does not automatically begin a database transaction or undo those service writes when the command fails. Use [command execution scopes](./command-execution-scopes.md) to coordinate your own transaction lifetime.

:::note[Inline work does not require Chronicle]
Direct service calls remain supported. When a model-bound command chooses inline work, prefer returning [command operations](./operations/index.md). Arc executes the operations and can run compensation you explicitly supply when the commit boundary permits it. Compensation is not automatic database rollback; actual transaction coordination belongs in an execution scope.
:::

## Choosing an append style

Compare returned events, transactional appends, and immediate appends in [Chronicle append styles](../chronicle/commands/transactional-commands.md#choosing-an-append-style) before choosing how your handler writes events.

## How it works

Read [Chronicle transaction completion](../chronicle/commands/transactional-commands.md#completion) to understand how the command's outcome determines whether pending enrolled events commit or roll back.

## Nested commands and aggregates

If a handler invokes another command or commits an aggregate, check [nested commands and aggregate commit boundaries](../chronicle/commands/transactional-commands.md#nested-commands-and-aggregates) before relying on the outer command's outcome.

## Things to know

Check [Chronicle transaction limitations](../chronicle/commands/transactional-commands.md#things-to-know) for work outside the event transaction, including writes that have already committed.
