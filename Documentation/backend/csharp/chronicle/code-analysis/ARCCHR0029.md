---
title: "ARCCHR0029: Conflicting read protection"
description: Choose a consistent protected or advisory command declaration.
---

## Rule

**Error**, in every mode. A command combines `[ProtectedDecision]` with
`[Unprotected]`, or explicitly declares On/Measure together with `[Unprotected]`.
Inherited declarations participate in the same policy.

## Resolve the diagnostic

Remove one conflicting declaration. A command-level `[Unprotected("why")]` may
override an assembly rollout; an explicit command-level On/Measure may not coexist
with that opt-out. `[ProtectedDecision]` plus On is compatible, not a conflict.

There is no automatic code fix: the choice changes command policy.
