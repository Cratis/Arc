---
title: Validate Chronicle commands against read models
description: Use a Chronicle read model in an Arc command validator to reject a settlement with no funds and return field feedback before handling.
---

Cratis Arc can validate a command against a read model from Cratis Chronicle before invoking `Handle()`. Use a read model, the state derived from events, when a rule depends on an existing balance or other stored state. The validator receives that model as a constructor dependency, so you do not write a query to load it.

## Reject a settlement with no funds

This validator fragment assumes your application has a `SettleLedger` command and a Chronicle-backed `LedgerBalance` read model. The command's `LedgerId` identifies the ledger to settle and resolves as its event source id. The rule allows settlement only when that ledger's balance is positive:

```csharp
public class SettleLedgerValidator : CommandValidator<SettleLedger>
{
    public SettleLedgerValidator(LedgerBalance balance) =>
        RuleFor(command => command.LedgerId)
            .Must(_ => balance.Balance > 0)
            .WithMessage("Ledger has no funds to settle.");
}
```

Suppose the ledger's read model exists and its `Balance` is `0`. Execute `SettleLedger` for that ledger through Arc's command pipeline. With the default validation severity, the rule rejects the command and returns:

| Result | Value |
| --- | --- |
| HTTP status, when called over HTTP | `400 Bad Request` |
| `CommandResult.IsSuccess` | `false` |
| `CommandResult.IsValid` | `false` |
| Validation result severity | `Error` |
| Validation result message | `Ledger has no funds to settle.` |
| Validation result members | `ledgerId` |

The failure belongs to `ledgerId` because the rule is attached to `command.LedgerId`, even though it checks the injected balance. A client can associate that feedback with the ledger field. Arc does not invoke `Handle()`, so the command appends no event.

## How Arc supplies the balance

Arc resolves the model using the command's event source id before constructing the validator:

1. Arc binds the command and resolves its event source id from `[Key]`, an `EventSourceId` or `EventSourceId<T>`-derived property, or `ICanProvideEventSourceId`. See [Resolving EventSourceId](resolving-event-source-id.md), which contributes the value to the [Command Context Values](../commands/command-context.md#command-context-values).
2. Arc resolves the Chronicle-backed read model by that id.
3. Arc constructs the validators with the model and runs their rules before invoking `Handle()`.

A later returned identity cannot reload the validator's dependency. The same command scope serves the validator, `Provide()`, and `Handle()`, so they share the resolved instance when execution reaches those phases.

The rest of Arc's [validation model](../commands/validation.md) works unchanged: data annotations validate input, `ConceptValidator<T>` validates a typed value, and `CommandValidator<TCommand>` validates the command.

## What to be aware of

The example requires an existing `LedgerBalance`. A key alone does not prove the model exists. Declare the parameter nullable when absence is a business condition your rule handles; keep it non-nullable when the projection is required. See [nullable versus required](read-models/injecting-into-commands.md#nullable-means-you-handle-absence) before deciding how a missing ledger should fail.

A rejection stops this execution, but approving a balance does not by itself protect that decision from concurrent changes. Under read protection `Off`, materialized models store projected state that can lag, while passive models build state on demand. Neither read is guarded, so two concurrent commands can pass the same rule. Under `On`, the validator's read-model parameter is read fresh, and a relevant competing event can cause rejection at commit rather than allow a write based on stale state. Follow [validators and read protection](consistency/validators.md) when the rule must hold through commit. For uniqueness, use a Chronicle [constraint](/chronicle/constraints/).

The result above assumes the default severity policy. If the rule must always block, configure [validation severity filtering](../commands/validation-severity-filtering.md) rather than relying on the caller's threshold.

Read-model injection requires Arc's command pipeline. It does not work through MVC controller validation; see [`ReadModelValidatorRequiresCommandPipeline`](read-models/failures.md#readmodelvalidatorrequirescommandpipeline).

## See also

- [Choosing how a command's rule holds](consistency/index.md) explains which mechanism enforces a rule.
- [Use current state in a command](../../../scenarios/use-current-state-in-a-command.mdx) walks through validators, `Provide()`, and `Handle()`.
- [Read models in commands](read-models/injecting-into-commands.md) covers dependency resolution in all three positions.
- [Command validation](../commands/validation.md) covers Arc's validation model without requiring Chronicle.

Once the settlement returns the expected feedback, add [pre-flight command validation](../commands/command-validation.md) so callers can check the rule without executing the handler.
