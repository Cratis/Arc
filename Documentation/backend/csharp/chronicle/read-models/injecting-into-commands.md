---
title: Read models in commands
description: Take a Chronicle read model as a dependency in a CommandValidator, a Provide method, or a Handle method — and declare what a missing instance means.
---

A command can take the read model Arc resolved for its key in three places: the constructor of a `CommandValidator<TCommand>`, a `Provide()` method, and a `Handle()` method. All three resolve from the same command scope, so all three see the same instance.

Which one to use is a question about *what the state is for*. These are focused dependency fragments using application read models, events, and commands; declarations without `Handle()` show only the validator's input shape, not complete runnable commands.

| Position | Use it when | The state is… |
| --- | --- | --- |
| `CommandValidator<TCommand>` | The command should be rejected with a message | a gate |
| `Handle()` | The event you produce is computed from the state | an input |
| `Provide()` | The state has to be combined with fetched data before the decision | an input to acquisition |

## In a validator

Validators run before the handler, which makes them the natural place for state-based rejection. The read model is an ordinary constructor dependency:

```csharp
[Command]
public record SettleLedger(EventSourceId LedgerId)
{
    public LedgerSettled Handle(LedgerBalance balance) => new(balance.Balance);
}

public class SettleLedgerValidator : CommandValidator<SettleLedger>
{
    public SettleLedgerValidator(LedgerBalance balance) =>
        RuleFor(command => command.LedgerId)
            .Must(_ => balance.Balance > 0)
            .WithMessage("Ledger has no funds to settle.");
}
```

Validators are discovered by convention — there is nothing to register. Their messages reach the client through `CommandResult` like any other validation error.

:::note[Validators need the Arc command pipeline]
Read-model injection into a validator works for commands that run through the Arc command pipeline — minimal-API command endpoints (the default) and `ICommandPipeline` directly. It does **not** work through MVC controllers, because MVC model validation runs during model binding, before the command context exists and therefore before there is an event source id to resolve by. The request fails with [`ReadModelValidatorRequiresCommandPipeline`](./failures.md#readmodelvalidatorrequirescommandpipeline). Expose the command through a minimal-API endpoint, or move the check into `Handle()`.
:::

## In `Handle()`

When the state is an input to the event rather than a gate on it, take it in the handler:

```csharp
[Command]
public record UseReducerReadModelInHandle(EventSourceId AccountId)
{
    public BalanceRecorded Handle(ReducerAccountSummary summary) => new(summary.Balance);
}
```

The injection signature is the same for reducer, fluent-projection, and model-bound-projection backings, including passive models. Freshness and release paths differ; see [materialized and passive paths](./index.md#materialized-and-passive-paths).

## In `Provide()`

`Provide()` acquires the data `Handle()` needs, and its return value is passed to `Handle()` as an argument. A read model can be one of `Provide`'s own inputs:

```csharp
[Command]
public record ProvideReadModelDependencyCommand(EventSourceId AccountId)
{
    public ProvidedAccountBalance Provide(AccountBalanceReadModel readModel) =>
        new(readModel.Balance);

    public ReadModelDependencyProvided Handle(ProvidedAccountBalance balance) =>
        new(balance.Value);
}
```

Use this shape when the projected state has to be combined with something fetched — a rate, a policy, an external lookup — before `Handle()` can decide. For plain validation, prefer a validator; `Provide()` exists to keep IO out of the decision, not to host rules. See [Provide data to a command handler](../../../../scenarios/provide-data-to-a-command.mdx).

## Nullable means you handle absence

The key on a command identifies *which* read model instance to resolve. It does not prove that instance exists. Nullability is how you declare what absence means, and Arc behaves differently for each choice.

### Nullable — absence is a business condition

Declare the parameter nullable when "does not exist" is a state your rule is written around. Arc injects `null` and your code decides:

```csharp
[Command]
public record RegisterCustomer([Key] Guid CustomerId, string Name);

public class RegisterCustomerValidator : CommandValidator<RegisterCustomer>
{
    public RegisterCustomerValidator(Customer? customer) =>
        RuleFor(_ => customer)
            .Null()
            .WithMessage("Customer is already registered");
}
```

The mirror image — reject when the entity is *missing* — is the same shape with the rule inverted, and `When` guards the rules that dereference it:

```csharp
public class AssignPersonToRoleValidator : CommandValidator<AssignPersonToRole>
{
    public AssignPersonToRoleValidator(RoleReadModel? role)
    {
        RuleFor(_ => role)
            .NotNull()
            .WithMessage("Role does not exist");

        When(_ => role is not null, () =>
        {
            RuleFor(command => command.PersonId)
                .Must(personId => !role!.AssignedPersonIds.Contains(personId))
                .WithMessage("Person is already assigned to this role");

            RuleFor(command => command)
                .Must(_ => role!.Status == RoleStatus.Active)
                .WithMessage("Cannot assign people to inactive roles");
        });
    }
}
```

### Non-nullable — the projection is required

Keep the parameter non-nullable when the command genuinely requires the projection and cannot evaluate its rules without it. Arc then fails the command with [`ReadModelDoesNotExistForCommand`](./failures.md#readmodeldoesnotexistforcommand) before your code runs, and you write rules against the state directly:

```csharp
[Command]
public record SubmitOrder([Key] Guid OrderId);

public class SubmitOrderValidator : CommandValidator<SubmitOrder>
{
    public SubmitOrderValidator(OrderReadModel order)
    {
        RuleFor(_ => order.Status)
            .Equal(OrderStatus.ReadyForSubmission)
            .WithMessage("Only orders that are ready for submission can be submitted");

        RuleFor(_ => order.Lines)
            .NotEmpty()
            .WithMessage("Order must have at least one line");
    }
}
```

A missing `OrderReadModel` here is a dependency-unavailable validation failure, not a domain-rule rejection: the validator declared the projection required, so its rules could not be evaluated.

### The analyzer makes the choice explicit

[ARC0006](../../code-analysis/ARC0006.md) reports a warning on every non-nullable command-scoped read model parameter, in a validator, `Provide()`, or `Handle()`. It is not saying non-nullable is wrong — it is making sure the required-state choice was made deliberately rather than by default.

The same nullability rules apply in all three positions:

```csharp
[Command]
public record UseNullableReducerReadModelInHandle(EventSourceId AccountId)
{
    public ReadModelAbsenceRecorded Handle(ReducerAccountSummary? summary) => new(summary is null);
}
```

## Combining with an aggregate root

A command can take both — projected state as context, and the aggregate as the thing that changes:

```csharp
[Command]
public record AddItemToCart([Key] Guid CartId, Guid ProductId, int Quantity)
{
    public async Task<AggregateRootCommitResult> Handle(
        ShoppingCart cart,
        ShoppingCartSummary? summary,
        ILogger<AddItemToCart> logger)
    {
        logger.LogInformation("Cart had {Count} items", summary?.TotalItems ?? 0);
        await cart.AddItem(ProductId, Quantity);
        return await cart.Commit();
    }
}
```

This fragment assumes an asynchronous aggregate mutation and `Cratis.Arc.Chronicle.Aggregates.AggregateRootCommitResult`. Do not also return the event the aggregate applies. Explicit commit propagates aggregate failures but finalizes the shared unit of work; see [commit boundaries](../aggregates/defining-an-aggregate-root.md#reporting-failure-and-committing).

Read models never emit events. Materialized models can lag; passive models are still snapshots rather than locks. For concurrent invariants, verify the aggregate's revision enforcement or use a Chronicle [constraint](/chronicle/constraints/) at append time.

## Decision reads for event-dependent commands

A plain `T` read-model parameter remains advisory: it does not protect a command from a concurrent append. **Opt in at the command declaration with `[ProtectedDecision]`** (`Cratis.Arc.Chronicle.ReadModels`) before injecting `DecisionRead<T>` into `Provide()` or `Handle()`; use `.Instance` / `.Exists`. Inject the command-aware `IDecisionReads` and call `Get<T>(otherKey)` to guard a different event source. The profile is fixed when the Arc pipeline enters each command, including nested commands and supplied-provider calls. An unmarked command behaves as it did before protected decisions: it retains legacy validators and plain read models, and `IDecisionReads.GetDetached<T>` returns a detached snapshot exactly as Chronicle's own reader does. The same holds for an `[Unprotected]` command, in its handler and its validator, on both `Validate` and `Execute`: `GetDetached<T>` is an explicitly advisory, unguarded read there. `DecisionRead<T>` and `IDecisionReads.Get<T>` **refuse** inside it; neither can silently activate protection. Inside a `[ProtectedDecision]` command `GetDetached<T>` is refused, because a detached snapshot is not enrolled in the decision. Every protected read must be enrolled in the command's owner-capable Chronicle unit of work; a successful command with no returned events still validates its reads at completion. A conflict returns `concurrencyViolation` without exposing sequence numbers, and the command is not retried.

`Validate` is advisory: it reads a detached snapshot and does not validate or append, so a successful validation response does **not** promise that `Execute` will commit; the next execution folds a fresh decision. `[Unprotected]` on a **command class** opts out of protected reads: its `DecisionRead<T>` dependencies use the legacy, unguarded read path in both `Validate` and `Execute`. Do not combine it with `[ProtectedDecision]`. On a plain model parameter or `Handle`/`Provide` method `[Unprotected]` is only an acknowledgement for tooling; parameter-level runtime opt-out is unsupported. A `DecisionRead<T>` returned from `Provide()` must have been issued in that invocation.

**Protected-mode validator boundary:** A `[ProtectedDecision]` command runs a discoverable validator — a `CommandValidator<T>`, a `ConceptValidator<T>` or any other `IDiscoverableValidator<T>` in its command graph — only when Arc can certify that the validator's rules depend on nothing but the input they validate:

- The validator's only public constructor is parameterless, so nothing is injected into it. A validator whose constructor takes dependencies — a read model, `DecisionRead<T>`, `IDecisionReads` or any other service — is refused before its dependencies are resolved, its registration runs or its rules are evaluated.
- Arc runs the constructor. Arc binds each convention-discovered, parameterless validator to its own construction, keeping its lifetime, and a validator with no registration at all is constructed directly. When the service provider supplies an instance Arc did not construct — from a factory, an instance registration or an explicit registration added after Arc's conventions — the command is refused, so a rule a registration adds never silently disappears. Such a registration's factory still runs before the refusal, because only resolving it reveals what it supplies; the command still fails and nothing the factory produced is used.

What Arc certifies is exactly this: the parameterless constructor ran in Arc, and the instance still has, by reference, the same rules and the same components of each rule that the constructor left it with. A decorator or code that adds, removes or replaces a rule or a rule component on Arc's instance is therefore refused. Nothing else about the instance is certified. **Keeping validators free of anything that can carry decision state is your responsibility, for all of the following:**

- static members and service locators read by a rule;
- a decorator (for example Scrutor's `Decorate`) that returns Arc's inner instance after changing its state;
- property injection, or any code that resolves the validator and then sets a field or property that a rule closure reads;
- `ApplyCondition` on an existing rule, a changed cascade mode, or rules added to a nested child validator, none of which the rule and component references reveal.

```csharp
[Command]
[ProtectedDecision]
public record RenameLedger(EventSourceId LedgerId, string Name)
{
    public LedgerRenamed Handle(DecisionRead<LedgerBalance> balance) => new(Name);
}

public class RenameLedgerValidator : CommandValidator<RenameLedger>
{
    public RenameLedgerValidator() =>
        RuleFor(command => command.Name).NotEmpty().WithMessage("A ledger needs a name.");
}
```

A refusal is an exception outcome and cannot be allowed by `X-Allowed-Severity`; validation is never silently skipped. Queries the command performs still run their own validators, such as paging validation. Keep state-based rules in `Provide()` or `Handle()` with `DecisionRead<T>` or command-aware `IDecisionReads`; validators that read decision state are tracked in [Arc #2831](https://github.com/Cratis/Arc/issues/2831), and Screenplay [#209](https://github.com/Cratis/Screenplay/issues/209) `require` waits on that work. A parameterless validator can still reach state in the ways listed above, which Arc cannot see; do not make protected decisions that way. Legacy `[Unprotected]` and unmarked commands retain their existing validator resolution and rules (including parent validators that inject child validators). Arc cannot certify arbitrary services, filters, or handler-owned closures that hold decision state, nor turn a raw Chronicle SDK reader obtained from `IEventStore` into a command-aware read. Do not use those paths to make protected decisions. A custom provider must supply the command-aware Chronicle reader and protection support; a missing guard refuses protected commands before validation or handler work. Custom unit-of-work implementations continue to work for legacy commands but cannot own protected decisions.

Protection requires a projection admitted by Chronicle for direct event-source-keyed event-log reads with a supported key conversion and a known, nonempty event-type set. Reducers, joins, nested/child and open-ended sources, arbitrary keys, passive/incomplete definitions and other refused shapes cannot be made safe by injection. Chronicle's decision-read limitations also apply: revisions, redactions, generation migrations, definition changes, other event sequences, external or non-Chronicle state and foreign writers are not guarded. Explicit concurrency scopes on the same label cannot be merged into a decision scope. Immediate `IEventLog.Append*` calls made inside a handler are **not** protected; return events for transactional enrollment instead. A command that manually commits a protected aggregate before its owner finishes is refused.

**Direct completion of a protected unit of work is refused.** Arc claims ownership of the command's Chronicle unit of work and commits or rolls it back as owner. A direct `Rollback()` or `Commit()` of that unit from command code, including an aggregate root committing its mutation, is refused with `ProtectedUnitOfWorkRequiresOwner` once the unit has enrolled decision reads, so the guard cannot be bypassed. This requires Chronicle 19.23 or later. A failed command whose unit has no enrolled decision reads is rolled back through the public `Rollback()`, as before.

## Read models from other providers

Injection is not Chronicle-only. A read model backed by Entity Framework Core or MongoDB is injected into a command exactly the same way, and everything on this page — the three positions, and what nullability means — applies unchanged.

What differs is where the read model is loaded from and what key loads it, including how a command declares its key when there is no Chronicle to resolve one. See [Read models from other providers](./other-providers.md).

## Testing

For legacy plain read models, seed state with the `Given` builder — either events or a pinned instance. For protected decisions, explicitly select the [single-log decision scenario](../../testing/chronicle.md#protected-decision-scenarios) and seed actual events; pinned state cannot supply a valid guard.

## See also

- [Use current state in a command](../../../../scenarios/use-current-state-in-a-command.mdx) — the short recipe.
- [When resolution fails](./failures.md) — every error and what it means.
- [Command validation](../../commands/validation.md) — the rest of Arc's validation model.
