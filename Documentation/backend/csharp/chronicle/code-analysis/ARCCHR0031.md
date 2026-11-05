---
title: "ARCCHR0031: Invalid read-model lookup"
description: ARCCHR0031 rejects invalid read-model lookups in Cratis Arc; use one lookup form and a public instance command property for string lookups.
---

## Rule

**Error**, in every mode. `[LookUpBy]` names a missing public instance command
property or annotates a provably non-model parameter. The attribute is placed on
the injected parameter, not the property. On a `CommandValidator<T>` constructor,
the property belongs to `T`, not the validator.

The generic `[LookUpBy<TKeyProvider>]` form does not name a command property. It is valid on a read-model parameter, but not on a provably non-model parameter. Combining it with string `[LookUpBy]`, or declaring multiple provider forms, is invalid. Declare exactly one lookup form; the C# compiler also rejects duplicate generic attributes with `CS0579`.

## Resolve the diagnostic

Use `[LookUpBy(nameof(Command.Property))]` with an existing command property.
Arc converts its value to the wire key independently of the model's CLR `Id`
type. Concepts and their primitive values, `EventSourceId`-derived records,
`Guid` and string keys do not have to share a CLR type. Null or empty keys are
still refused at runtime.

For caller-owned state, use `[LookUpBy<CallerSubject>]` or a custom provider instead of inventing a client-supplied property. See [caller-derived keys](../read-models/injecting-into-commands.md#key-a-read-by-the-caller). Providers have no property-name argument, so the `nameof` fix is not offered for them. Per-stream models cannot be redirected by either form.

For a missing name, the code fix offers `nameof` only for a nearby spelling
(edit distance at most two). No candidate means no fix, rather than changing
lookup to an unrelated property.

Unseen models can be backed by another project, so absence of local projection
metadata alone is not proof that a parameter is not a model.
