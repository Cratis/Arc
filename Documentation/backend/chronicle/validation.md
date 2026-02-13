---
uid: Arc.Chronicle.Validation
---
# Validation

Chronicle integrates with Arc.Core validation so you can validate commands using read models. This is useful when validation depends on the current state derived from events.

For general validation concepts and patterns, see [Arc.Core validation](../commands/validation.md).

## Read Model Dependencies in Model-Bound Commands

Model-bound commands can take dependencies directly on read models. When the command is validated, Chronicle resolves the read model and makes it available to your validators and handlers.

Chronicle resolves the identity for the read model by convention. If a command has a property of type `EventSourceId` (or a type that inherits from it), that value is used as the identity for resolving the read model.

This enables a direct validation flow:

1. The command is bound and validated.
2. Chronicle detects the `EventSourceId` identity on the command.
3. The read model instance is loaded from the Chronicle read models system.
4. Validators can use the read model state to validate the command.

To learn more about the read model system in Chronicle, see [Read Models](xref:Chronicle.ReadModels).

## Notes

- The identity convention applies to model-bound commands and is based on `EventSourceId` or types deriving from it.
- Read model resolution uses the same persistence and projection infrastructure as the rest of Chronicle.

