---
title: "ARCCHR0025: Keyless command reads Chronicle state"
description: Detect automatically injected read models looked up by a newly generated command identity.
---

## Rule

A command without an event source key gets a newly generated id on every call.
An automatically injected Chronicle read model is looked up by that id, not an
existing entity's identity. The analyzer reports a **Warning** on the read-model
parameter, whether nullable or non-nullable.

When the model does not exist, a nullable parameter receives null. A non-nullable
parameter produces `ReadModelDoesNotExistForCommand`, a validation failure surfaced
as HTTP 400. This is different from a declared key that cannot be composed, which
produces an unspecified id and `UnableToResolveReadModelFromCommandContext`.

An `IReadModels` or `IEventStore` parameter in the same keyless event-producing
command produces **Info** instead: explicit reads might already use the right key,
and their lookup is not proven by this analyzer.

## Resolve the diagnostic

Declare the command's event source key with a domain identity derived from
`EventSourceId<T>`, or mark the intended property with Chronicle's `[Key]`.
For a computed identity, implement `ICanProvideEventSourceId`.
See [event source identity timing](../resolving-event-source-id.md).
There is no automatic code fix.

## Boundaries

The analyzer examines public instance `Handle()` and `Provide()` methods on recognizable
event-producing model-bound commands. Inherited keys, positional `[Key]`, and custom
identity providers count as key declarations; a null or uncomposable value cannot be
proven from a signature. Keyed services and `Handle()` dependencies on commands with
`Provide()` are excluded because they may be supplied explicitly.

Model recognition includes model-bound projection metadata, source-declared projections
and reducers, and recognizable self-reductions. External fluent artifacts, runtime-only
event registration, erased event return types, and indirect reads can be missed. An
explicit identity returned beside an event does not change earlier dependency lookup.
