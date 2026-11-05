---
title: Chronicle reactors for follow-up work
description: Use Chronicle reactors in Arc to act on committed events, return follow-up commands, and choose how side effects behave during replay and retry.
---

A Cratis Chronicle reactor runs follow-up work when an event has been committed. Use it in Cratis Arc to send a welcome email after registration or update a search index after a book enters the catalog. The original command decides whether the change is allowed; the reactor acts on the recorded fact. A projection, by contrast, builds state for queries.

For immediate work chosen inside a model-bound command, use returned [command operations](../../commands/operations/index.md). Return those operations from the command's `Handle()`, not from a reactor. They do not provide durable delivery; keep event-driven follow-up in a reactor or a durable workflow/outbox.

## Set up the reaction

Reactors need Arc's Chronicle integration and registered event types. Follow [Arc and Chronicle setup](../cratis-package.md#basic-setup), including the separate Chronicle server, before adding a reactor. If you already supply authentication, use that page's [split-registration setup](../cratis-package.md#running-arc-or-chronicle-on-their-own).

Implement `IReactor` and give each handler the event type it observes as its first parameter. [React to an event](../react-to-an-event.md) walks through calling a service or executing a command. To let Arc execute a follow-up command through its validation and authorization pipeline, [return the command as a side effect](./command-side-effects.md).

## Choose replay behavior

A replay processes recorded events again, for example after an observer rewind. Put `[OnceOnly]` on a handler when replay must not repeat its effect, such as sending a welcome email. At class level, `[OnceOnly]` excludes the whole reactor from replay.

Replay exclusion does not prevent retries. Recovering a failed event-source partition can deliver the same event again, including to a `[OnceOnly]` handler. Make effects idempotent, meaning that repeating them does not repeat the business change. Chronicle's [delivery identity](/chronicle/reactors/delivery-identity/) gives you a stable key to recognize a delivery, but your application or the receiving service must use it to prevent duplicate external effects.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Returning commands as side effects](./command-side-effects.md) | Return one or several commands, choose their system actor, and handle refusal or partial failure. |
