---
title: "ARCCHR0033: Aggregate and read-model parameter share a source"
description: ARCCHR0033 warns when a Cratis Arc command combines an aggregate and a protected plain read on the same source under On; runtime refuses this combination.
---

## Rule

**Warning**, under On. An automatically injected aggregate and protected plain
read-model parameter resolve to the same command key. Runtime refuses this
combination; a model on a different key is allowed.

## Resolve the diagnostic

Use the aggregate's own state, or read the other entity explicitly.
If the plain read is only context, mark it `[Unprotected("why")]`.

## Boundaries

Only statically matching automatic keys are reported. Advisory reads, custom
source providers, and Handle dependencies potentially supplied by Provide are
excluded. Runtime checks remain authoritative for actual keys. No code fix is offered.
