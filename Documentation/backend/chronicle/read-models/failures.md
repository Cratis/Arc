---
title: When read model resolution fails
description: Every failure mode for a command-scoped Chronicle read model — what the client sees, what it means, and how to fix it.
---

Resolving a read model for a command can fail in a small number of well-defined ways. Each one is a distinct type, and each one is deliberately surfaced as a **validation failure (HTTP 400)** rather than a server error, because every one of them is caused by the request rather than by the server.

| Failure | Cause | Client sees |
|---|---|---|
| [`UnableToResolveReadModelFromCommandContext`](#unabletoresolvereadmodelfromcommandcontext) | The command carries no usable key | HTTP 400 — "The command is missing the identifier required to load its current state." |
| [`ReadModelDoesNotExistForCommand`](#readmodeldoesnotexistforcommand) | Valid key, but a required (non-nullable) read model does not exist | HTTP 400 — "The command targets an entity that does not exist." |
| [`ReadModelValidatorRequiresCommandPipeline`](#readmodelvalidatorrequirescommandpipeline) | The validator ran through MVC model binding, before a command context existed | Request fails |
| [`CannotResolveCommandDependency` / `CannotResolveValidatorDependency`](#cannotresolvecommanddependency-and-cannotresolvevalidatordependency) | A required non-nullable dependency could not be resolved | Depends on the dependency |

In every case the detailed message — which names the read model type — goes to the server log only. The client sees a generic message, so the type never leaks over the wire.

## UnableToResolveReadModelFromCommandContext

The command context carried no usable event source id, so there is no key to resolve a read model by. This happens when the command has no identity at all, or when the resolved identity is `EventSourceId.Unspecified`.

```csharp
// No key: no [Key] property, no EventSourceId-convertible property,
// no ICanProvideEventSourceId — nothing to resolve a read model by.
[Command]
public record InvalidCommand(string SomeProperty)
{
    public SomethingHappened Handle(Customer customer) => new();
}
```

This is not "the entity does not exist" — it is a command that could never resolve one, for a nullable and a non-nullable parameter alike. Making the parameter nullable does **not** suppress it.

**Fix:** give the command a key. Mark a property with `[Key]`, use a property whose type converts to `EventSourceId` (typically a `ConceptAs<Guid>` with an `implicit operator EventSourceId`), or implement `ICanProvideEventSourceId`. See [Resolving EventSourceId](../resolving-event-source-id.md).

## ReadModelDoesNotExistForCommand

The command carried a valid key, but no read model exists for it — and the dependency was declared **non-nullable**, so Arc cannot inject anything.

This is the runtime counterpart of the choice [ARC0006](../../code-analysis/ARC0006.md) asks you to make. Arc treats it as a rejected command rather than a server fault, because "you asked me to act on an entity that isn't there" is invalid input.

**Fix — pick the one that matches your intent:**

- **Absence is a business condition.** Make the parameter nullable and write the rule around `null`:

  ```csharp
  public class RemoveContactValidator : CommandValidator<RemoveContact>
  {
      public RemoveContactValidator(Customer? customer) =>
          RuleFor(_ => customer)
              .NotNull()
              .WithMessage("Customer is not registered");
  }
  ```

  You get a specific message instead of the generic one, which is almost always the better experience.

- **The projection really is required.** Leave it non-nullable — the HTTP 400 is the intended behavior, and nothing needs to change.

If neither fits, the read model may not have caught up yet: it is eventually consistent, so a command issued immediately after the event that creates the projection can arrive first. For an invariant that must hold regardless, use a Chronicle [constraint](/chronicle/constraints/) instead of projected state.

## ReadModelValidatorRequiresCommandPipeline

A `CommandValidator<TCommand>` that depends on a read model was constructed through the **MVC controller** model-validation path. MVC runs validation during model binding — before the command context, and therefore before the event source id, exists. The validator cannot be constructed, so the request fails.

This affects MVC controllers only. Minimal-API command endpoints (the Arc default) and direct `ICommandPipeline` execution both establish the command context first and work correctly.

**Fix:** expose the command through a minimal-API command endpoint, or move the read-model based check out of the validator and into the command's `Handle()` method.

## CannotResolveCommandDependency and CannotResolveValidatorDependency

The general case: Arc needed to invoke `Provide()`, `Handle()`, or construct a discoverable validator, and a required non-nullable parameter could not be resolved or resolved to `null`.

For a registered read model with a valid event source id, Arc classifies the failure as `ReadModelDoesNotExistForCommand` instead — so if you are seeing these types for a read model parameter, the cause is usually one of:

- **The read model has no Chronicle backing artifact.** `[ReadModel]` alone does not register a type for command-scope injection. Add an `IProjectionFor<T>`, a model-bound projection, or an `IReducerFor<T>`. See [what makes a read model injectable](./index.md#what-makes-a-read-model-injectable).
- **The parameter is not a read model at all** — an ordinary service that is not registered.

Arc deliberately leaves these as server errors rather than masking them as validation failures, so genuine misconfiguration is not hidden behind an HTTP 400.

## See also

- [Read models in commands](./injecting-into-commands.md) — declaring the dependency and choosing nullability.
- [ARC0006](../../code-analysis/ARC0006.md) — the analyzer that surfaces the nullability choice at build time.
- [Resolving EventSourceId](../resolving-event-source-id.md) — how the key is found in the first place.
