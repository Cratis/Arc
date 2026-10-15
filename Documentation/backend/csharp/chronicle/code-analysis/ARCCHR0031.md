---
title: "ARCCHR0031: Invalid read-model lookup"
description: LookUpBy must select an existing compatible command property.
---

## Rule

**Error**, in every mode. `[LookUpBy]` names a missing public instance command
property, selects an incompatible key type, or annotates a provably non-model
parameter. The attribute is placed on the injected parameter, not the property.

## Resolve the diagnostic

Use `[LookUpBy(nameof(Command.Property))]` with a property of the model key type.
For a missing name, the code fix offers `nameof` only for a nearby spelling
(edit distance at most two) whose type matches the statically known model key.
No candidate means no fix, rather than changing lookup to an unrelated property.

Unseen models can be backed by another project, so absence of local projection
metadata alone is not proof that a parameter is not a model.
