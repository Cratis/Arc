# Event Source Values Provider

Chronicle contributes event source identity information to the command pipeline through the `EventSourceValuesProvider`. This provider participates in the command context value system and makes the event source ID available for downstream features.

For details on command context values and how providers work, see the [Command Context Values](../commands/command-context.md#command-context-values) section.

## What It Does

The provider inspects the command instance and resolves an `EventSourceId` by convention. When it finds a matching value, it adds it to the command context values so it can be used by Chronicle services.

This enables:

- Automatic aggregate root resolution for commands
- Read model resolution for validation and model-bound handlers
- Consistent identity propagation without custom plumbing

## Identity Resolution Convention

The event source ID is resolved when a command has a property of type `EventSourceId` or a type that inherits from it. That value becomes the identity used by Chronicle for aggregate and read model operations.

## Read Model Integration

When model-bound commands depend on read models, Chronicle uses the event source ID from the command context to load the correct read model instance. This leverages the Chronicle read models system for consistency and projection-backed state.

To learn more about read models, see [Read Models](xref:Chronicle.ReadModels).

