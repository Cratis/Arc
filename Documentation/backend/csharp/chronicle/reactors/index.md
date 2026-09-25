---
title: Reactors
description: Reactors turn a recorded fact into an effect — a notification, an external call, or a follow-up command in another slice.
---

A projection answers "what does this look like now?" A reactor answers "what should happen *because* of this?" After an author registration commits, a reactor can send a welcome email, update the search index, or start the next step. Keep follow-up that depends on committed facts separate from the original command's decision.

:::note[Choose when the work must happen]
Keep reactors or a durable workflow/outbox for reliable follow-up to committed facts. For immediate work chosen within a model-bound command, prefer returned [command operations](../../commands/operations/index.md). They do not provide durable delivery and are returned from the command, not directly from a reactor.
:::

[React to an event](../react-to-an-event.md) is the place to start: when to reach for a reactor, how method dispatch works by event type, and why idempotency matters. The topics here go deeper.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Returning commands as side effects](./command-side-effects.md) | Let a reactor trigger follow-up commands by returning them — Arc executes them through the command pipeline automatically. |
