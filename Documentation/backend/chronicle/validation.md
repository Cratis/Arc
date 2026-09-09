---
title: Validation with read models
description: Validate an event-sourced command against the state Chronicle already projected for its key, without writing a query.
---

Arc's [validation model](../commands/validation.md) — data annotations, `ConceptValidator<T>`, and `CommandValidator<TCommand>` — works unchanged when Chronicle is in the picture. What the integration adds is one thing: a validator can take the **read model Chronicle projected for the command's key** as a constructor dependency, and validate against current state without a query.

```csharp
public class SettleLedgerValidator : CommandValidator<SettleLedger>
{
    public SettleLedgerValidator(LedgerBalance balance) =>
        RuleFor(command => command.LedgerId)
            .Must(_ => balance.Balance > 0)
            .WithMessage("Ledger has no funds to settle.");
}
```

This validator fragment assumes the application's `SettleLedger` command and `LedgerBalance` backing. Arc resolves the model using the command's input-time event source id, then constructs the validator. A later returned identity cannot reload that dependency. The flow is:

1. The command is bound, and its event source id is resolved — from `[Key]`, from an `EventSourceId` or `EventSourceId<T>`-derived property, or from `ICanProvideEventSourceId`. See [Resolving EventSourceId](resolving-event-source-id.md), which contributes the value to the [Command Context Values](../commands/command-context.md#command-context-values).
2. The read model instance is loaded from Chronicle's read model store by that id.
3. Validators are constructed with it and their rules run — before `Handle()` is invoked.

Because the same command scope serves `Provide()` and `Handle()`, all three see the same instance.

## What to be aware of

- **A key does not prove existence.** Declare the parameter nullable when a missing projection is a business condition, non-nullable when it is required. This is the central decision — see [nullable versus required](read-models/injecting-into-commands.md#nullable-means-you-handle-absence).
- **Freshness depends on backing.** Materialized models can lag; passive models build state on demand. Neither snapshot alone guarantees an invariant under concurrent commands. Use a Chronicle [constraint](/chronicle/constraints/) for those.
- **Validators need the Arc command pipeline.** Read-model injection does not work through MVC controllers — see [`ReadModelValidatorRequiresCommandPipeline`](read-models/failures.md#readmodelvalidatorrequirescommandpipeline).

## See also

- [Use current state in a command](../../scenarios/use-current-state-in-a-command.md) — the recipe, covering validators, `Provide()`, and `Handle()`.
- [Read models in commands](read-models/injecting-into-commands.md) — the full reference for all three positions.
- [Command validation](../commands/validation.md) — Arc's validation model in general.
