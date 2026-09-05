---
title: Reactors
description: Reactors turn a recorded fact into an effect — a notification, an external call, or a follow-up command in another slice.
---

A projection answers "what does this look like now?" A reactor answers "what should happen *because* of this?" When an author is registered, something has to send the welcome email, tell the search index, or kick off the next step — and none of that belongs in the command, which should only record that the fact happened.

[React to an event](../react-to-an-event.md) is the place to start: when to reach for a reactor, how method dispatch works by event type, and why idempotency matters. The topics here go deeper.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Returning commands as side effects](./command-side-effects.md) | Let a reactor trigger follow-up commands by returning them — Arc executes them through the command pipeline automatically. |
