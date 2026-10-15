---
title: "ARCCHR0031: Invalid read-model lookup"
description: LookUpBy must select an existing public instance command property on a read-model parameter.
---

## Rule

**Error**, in every mode. `[LookUpBy]` names a missing public instance command
property or annotates a provably non-model parameter. The attribute is placed on
the injected parameter, not the property. On a `CommandValidator<T>` constructor,
the property belongs to `T`, not the validator.

## Resolve the diagnostic

Use `[LookUpBy(nameof(Command.Property))]` with an existing command property.
Arc converts its value to the wire key independently of the model's CLR `Id`
type. Concepts and their primitive values, `EventSourceId`-derived records,
`Guid` and string keys do not have to share a CLR type. Null or empty keys are
still refused at runtime.

For a missing name, the code fix offers `nameof` only for a nearby spelling
(edit distance at most two). No candidate means no fix, rather than changing
lookup to an unrelated property.

Unseen models can be backed by another project, so absence of local projection
metadata alone is not proof that a parameter is not a model.
