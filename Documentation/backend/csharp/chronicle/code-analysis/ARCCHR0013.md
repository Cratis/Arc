---
title: "ARCCHR0013: Use the event source definition"
description: Replace matching legacy source and stream strings with a typed definition.
---

## Rule

**Info**. A legacy event source type string uniquely matches a definition in the
compilation. If a stream is named, that definition must declare it too.

## Resolve the diagnostic

Use `[EventSource<TDefinition>]` or `[EventSource<TDefinition>("Stream")]`.
The code fix combines the legacy source and stream attributes and keeps other
routing attributes. Review the definition's concurrency dimensions when migrating.

## Boundaries

Unmatched or ambiguous definitions and undeclared streams are not reported.
The fix is offered only when the source and any stream declaration can be changed
on the same type; inherited stream policies require a deliberate migration.
