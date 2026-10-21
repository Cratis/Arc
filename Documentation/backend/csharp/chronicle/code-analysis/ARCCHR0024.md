---
title: "ARCCHR0024: Read-model shape cannot be protected"
description: Source-visible admission refusals under Measure and On.
---

## Rule

**Warning**, in Measure or On. A protected read-model parameter has a shape
Chronicle refuses: joins or children, an event-property key, an unsupported key,
or a reducer with constructor dependencies or an observer event filter.
Unfiltered dependency-free reducers are admitted.

## Resolve the diagnostic

Read joined parts separately, append facts to the entity a model is keyed by,
use a constraint for uniqueness, or remove reducer dependencies/filters.
Use a string or Guid identity (including supported concepts).
If the read is only context, mark the parameter or model `[Unprotected("why")]`.
Method-level `[Unprotected]` does not opt out injected parameters.

## Boundaries

The analyzer checks Handle/Provide dependencies and validator constructors.
Off and explicitly advisory parameters are excluded. Source-visible fluent
projection shapes are checked; runtime-only registrations, generation delivery
(including Pinned), and definitions in other assemblies remain runtime-validated.
A quiet analyzer is not proof of admission. No code fix is offered.
