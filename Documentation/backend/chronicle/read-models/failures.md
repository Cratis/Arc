---
title: When read model resolution fails
description: Every failure mode for a command-scoped Chronicle read model — what the client sees, what it means, and how to fix it.
---

Read-model resolution distinguishes invalid input from missing infrastructure. An unusable key or an absent required read model is a **validation failure (HTTP 400)**. Missing service registrations and execution outside the command pipeline are configuration problems, not evidence that an entity does not exist.

| Failure                                                                                                                                       | Cause                                                                         | Client sees                                                                            |
| --------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| [`UnableToResolveReadModelFromCommandContext`](#unabletoresolvereadmodelfromcommandcontext)                                                   | The command carries no usable key                                             | HTTP 400 — "The command is missing the identifier required to load its current state." |
| [`ReadModelDoesNotExistForCommand`](#readmodeldoesnotexistforcommand)                                                                         | Valid key, but a required (non-nullable) read model does not exist            | HTTP 400 — "The command targets an entity that does not exist."                        |
| [`ReadModelValidatorRequiresCommandPipeline`](#readmodelvalidatorrequirescommandpipeline)                                                     | The validator ran through MVC model binding, before a command context existed | Request fails                                                                          |
| [`CannotResolveCommandDependency` / `CannotResolveValidatorDependency`](#cannotresolvecommanddependency-and-cannotresolvevalidatordependency) | A required non-nullable dependency could not be resolved                      | Depends on the dependency                                                              |

In every case the detailed message — which names the read model type — goes to the server log only. The client sees a generic message, so the type never leaks over the wire.

## UnableToResolveReadModelFromCommandContext

The resolved key is unusable, so there is nothing to resolve a read model by. With Chronicle, this means `EventSourceId.Unspecified` — for example, a declared key whose value is null, or an `ICanProvideEventSourceId` implementation that cannot compose an identity. Without Chronicle, it also includes commands on which no key is declared.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Keys;

[Command]
public record CheckCustomer([Key] string? CustomerId)
{
    // A null CustomerId leaves the declared key unspecified.
    // Making the read-model dependency nullable does not permit an unusable key.
    public bool Handle(Customer? customer) => customer is not null;
}
```

### Keyless creation commands are different

When a Chronicle command declares **no** event-source key and does not implement `ICanProvideEventSourceId`, Chronicle generates a new identity so the command can create an entity. That is a usable key, not `EventSourceId.Unspecified`. If no read model exists for the generated identity, a nullable dependency receives `null`; a required dependency produces `ReadModelDoesNotExistForCommand`. A command targeting an existing entity must declare that entity's identity instead of relying on this creation fallback.

For an unusable declared key, the failure is not "the entity does not exist" — the lookup cannot be performed, for a nullable and a non-nullable parameter alike. Making the parameter nullable does **not** suppress `UnableToResolveReadModelFromCommandContext`.

**Fix:** give the command a key. What counts as one depends on whether the application has Chronicle:

| Setup             | Declare the key by                                                                                                                                                                                                                                                          |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| With Chronicle    | marking a property or matching positional parameter with `Cratis.Chronicle.Keys.KeyAttribute`, using an `EventSourceId` or `EventSourceId<T>`-derived property, or implementing `ICanProvideEventSourceId` — see [Resolving EventSourceId](../resolving-event-source-id.md) |
| Without Chronicle | marking a property with `System.ComponentModel.DataAnnotations.KeyAttribute`, or implementing `ICanProvideKeyForCommand` — see [Declaring the key without Chronicle](./other-providers.md#declaring-the-key-without-chronicle)                                              |

The data annotations attribute does not declare a Chronicle key. Unless another identity source is present, using it triggers the generated-ID fallback, not the missing-key error. [ARCCHR0008](../code-analysis/ARCCHR0008.md) reports that attribute mismatch at build time.

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

- **No provider registered the read model for command-scope injection.** Chronicle requires a backing projection or reducer; `[ReadModel]` alone does not make Chronicle own it. MongoDB and Entity Framework Core supply their own registrations. See [what makes a read model injectable](./index.md#what-makes-a-read-model-injectable) and [other providers](./other-providers.md).
- **The parameter is not a read model at all** — an ordinary service that is not registered.

Arc deliberately leaves these as server errors rather than masking them as validation failures, so genuine misconfiguration is not hidden behind an HTTP 400. A nullable dependency can also receive `null` when its type is not registered at all: nullable DI alone does not prove that a read-model lookup took place. Verify provider registration before interpreting that null as an absent entity.

## See also

- [Read models in commands](./injecting-into-commands.md) — declaring the dependency and choosing nullability.
- [ARC0006](../../code-analysis/ARC0006.md) — the analyzer that surfaces the nullability choice at build time.
- [Resolving EventSourceId](../resolving-event-source-id.md) — how the key is found in the first place.
