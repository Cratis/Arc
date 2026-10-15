---
title: "ARCCHR0023: Read-model lookup key is not proven"
description: Signature-based hints about ambiguous, missing, or repeated read-model lookup keys.
---

## Rule

A read-model parameter's key is not necessarily the entity you intended to read.
This analyzer compares a Chronicle-backed model's `[Key]` or `Id`/`id` type with
public command properties and the command's event source key.

For a distinguishing concept type different from the command key type:

- Several matching properties produce a **Warning** naming the candidates.
- No matching property produces **Info**.
- Under On, exactly one matching property is selected and is not reported.
- Under Off/Measure, a unique matching property produces **Info**: the read still
  uses the command key unless `[LookUpBy]` explicitly redirects it.

Under On, ambiguity is reported even for primitive key types. Explicit `[LookUpBy]`
parameters are excluded from this heuristic and validated by ARCCHR0031.

A second parameter resolving to the same inferred key produces **Info**. Reading
several views of one entity can be intentional; this advisory does not prove a bug.
Raw primitives, `Guid`, `string`, `EventSourceId`, and the generic `EventSourceId<T>`
base are not distinguishing types. Derived domain identity types are distinguishing.

## Resolve the diagnostic

Use `[LookUpBy(nameof(Command.Property))]` on the read-model parameter to select
the intended entity in every mode, or use `Provide()` for a computed key. See [read-model injection](../read-models/injecting-into-commands.md).
Suppress a repeated-key advisory with a justification when both parameters intentionally
represent the same entity. There is no automatic code fix.

This analyzer does not change runtime resolution. On selects a unique matching
property; Off and Measure retain the command key without an explicit `[LookUpBy]`.

## Boundaries

The analyzer examines public instance `Handle()` and `Provide()` methods on recognizable
event-producing model-bound commands. It recognizes model-bound projection metadata,
projection/reducer targets declared in the compilation, and recognizable self-reductions.
Custom event source providers, keyed service parameters, and `Handle()` dependencies
on commands with `Provide()` are excluded because their lookup is not proven statically.
Indirect reads, external fluent artifacts, runtime-only event registration, and erased
event return types can be missed. A quiet analyzer does not prove lookup correctness.
