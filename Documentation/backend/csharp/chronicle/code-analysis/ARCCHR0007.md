---
title: "ARCCHR0007: Command handler injects IEventLog"
description: Express command writes through returned events or transactional appends.
---

## Rule

**Warning**. A command injects `IEventLog` in Handle or Provide. Return events
instead of bypassing Arc's append pipeline. To close a stream, return `CompleteStream`.

Under On, immediate appends are refused, even inside `[Unprotected("why")]`
method bodies: that declaration permits advisory reads, not immediate writes.
Use `IEventLog.Transactional` when returning events is not possible.

## Boundaries

This signature rule does not prove whether an injected log is actually used or
whether its appends are transactional. Suppress with a justification for a deliberate
transactional-only dependency. No code fix is offered.
