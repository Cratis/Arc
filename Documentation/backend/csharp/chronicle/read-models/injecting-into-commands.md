---
title: Read models in commands
description: Take a Chronicle read model as a dependency in a CommandValidator, a Provide method, or a Handle method — and declare what a missing instance means.
---

A command can take the read model Arc resolved for its key in three places: the constructor of a `CommandValidator<TCommand>`, a `Provide()` method, and a `Handle()` method. All three resolve from the same command scope, so all three see the same instance.

:::caution[A plain read guards nothing under Off]
Under read protection `Off`, the default, an injected read model is a **plain read**: it returns a snapshot, captures no boundary and guards nothing, in a validator, in `Provide()` and in `Handle()` alike. Under `On`, the same parameter is read fresh and guarded. The next major version of Arc makes `On` the default; see [The default changes in the next major version](../consistency/index.md#the-default-changes-in-the-next-major-version). Use a constraint for uniqueness. See [Choosing how a command's rule holds](../consistency/index.md) and [Getting started with read protection](../consistency/getting-started.md).
:::

Which one to use is a question about *what the state is for*. These are focused dependency fragments using application read models, events, and commands; declarations without `Handle()` show only the validator's input shape, not complete runnable commands.

| Position | Use it when | The state is… |
| --- | --- | --- |
| `CommandValidator<TCommand>` | The command should be rejected with a message | a gate |
| `Handle()` | The event you produce is computed from the state | an input |
| `Provide()` | The state has to be combined with fetched data before the decision | an input to acquisition |

## Choose how a command reads state

Use the first shape that expresses the key:

1. Take the read model as a plain `Handle` parameter. Use `[ReadProtection(ReadProtectionMode.On)]` where the rule must hold under concurrency.
2. Use `[LookUpBy(nameof(Command.Property))]` when a command property supplies another model's key.
3. Declare a per-stream model and take it as a plain parameter when state belongs to the command's stream.
4. Only when no parameter can express the key—an identity-derived or ambient key, or each key in a collection—read with `IDecisionReads.Get<T>` inside `Provide()`, returning plain values to `Handle()`.

New application code does not take `IDecisionReads` or `DecisionRead<T>` in `Handle()`. Those advanced shapes remain supported, but the plain parameter keeps the decision independent of acquisition and makes it directly specifiable.

## In a validator

Validators run before the handler, so a validator can reject early with a message. The read model is an ordinary constructor dependency. Under `Off` the rejection is advisory: two concurrent commands can both pass it. Under `On` the parameter is guarded, so a command that passed on stale state is not committed. See [validators and read protection](../consistency/validators.md):

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

The injection signature is the same for classic reducers, [self-reducing models](./index.md#self-reducing-read-models) (Chronicle 19.58.0+), fluent projections, and model-bound projections, including passive models. A self model needs no separate reducer registration in Arc: Chronicle's discovered reducer set supplies it. Existing instances are read through `IReadModels` and released before injection; absent self-model state resolves to null. Under `Off`, none of these snapshots protects a later append against concurrent changes. Under `On`, a model that read protection can guard is read fresh and guarded, and a reducer that it cannot guard is refused. Freshness and release paths differ; see [materialized and passive paths](./index.md#materialized-and-passive-paths).

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

Read models never emit events. Under `Off`, materialized models can lag and passive models are snapshots rather than locks. For concurrent invariants use a Chronicle [constraint](/chronicle/constraints/) for uniqueness, or read protection `On` for state. An aggregate guards only its own event source and the event types it handles. Under `On`, an aggregate and a plain read of the **same** event source in one command are refused; a read of a different source is allowed. See [choosing how a command's rule holds](../consistency/index.md).

## Decision reads for event-dependent commands

Under read protection `On`, a plain `T` parameter in `Handle()`, `Provide()` or a validator constructor is a guarded read. Nothing else is needed on the command. [Getting started](../consistency/getting-started.md) shows the whole path, and [Reads](../consistency/reads.md) lists what each read becomes.

| You write | Under `Off` or `Measure` | Under `On` |
| --- | --- | --- |
| `Handle(BookStock? stock)`, or a validator constructor parameter | Plain read | Guarded read |
| `[Unprotected("why")]` on the parameter or the model | Plain read | Advisory read: stored, unguarded |
| `[LookUpBy(nameof(Property))]` on a parameter | Selects the key | Selects the key |
| `IDecisionReads.Get<T>(key)` in `Provide()`, returning plain values for computed keys | Guarded for `[ProtectedDecision]` commands | Guarded read |
| Existing advanced `DecisionRead<T>` with `[ProtectedDecision]` | Guarded read for that command | Guarded read. Prefer a plain parameter |
| `IReadModels`, `IEventStore` or `IEventLog` reads | Allowed | Refused unless the method is `[Unprotected("why")]` |

A conflict returns `concurrencyViolation` with the detail `ReadChanged`, and the command is not retried. See [Refusals](../consistency/refusals.md).

`Validate` is advisory: it reads fresh state and does not append or guard, so a successful validation response does **not** promise that `Execute` will commit.

### Existing advanced commands marked [ProtectedDecision]

The following is existing, advanced usage, not the recommended shape for new application code. Prefer a plain read-model parameter under `On`. An existing command that is not under `On` can opt in on its own with `[ProtectedDecision]` (`Cratis.Arc.Chronicle.ReadModels`) before injecting `DecisionRead<T>` into `Provide()` or `Handle()`; use `.Instance` and `.Exists`. Inject the command-aware `IDecisionReads` and call `Get<T>(otherKey)` to guard a different event source. The profile is fixed when the Arc pipeline enters each command, including nested commands. An unmarked command under `Off` keeps its plain reads, and `IDecisionReads.GetDetached<T>` returns a detached snapshot as Chronicle's own reader does. Inside a `[ProtectedDecision]` command `GetDetached<T>` is refused, because a detached snapshot is not enrolled in the decision. A `DecisionRead<T>` returned from `Provide()` must have been issued in that invocation.

By default, a `[ProtectedDecision]` command outside `On` runs a discoverable validator only if it has one public, parameterless constructor. [Certified validator construction](../consistency/certified-validators.md) opts dependency-taking validators into fresh construction in a native Microsoft DI scope: use `DecisionRead<T>` or the command-aware `IDecisionReads`, not a plain read-model parameter. This capability requires an untrimmed JIT runtime. Under `On`, validators with plain read-model parameters remain allowed without that opt-in. See [Validators and read protection](../consistency/validators.md#commands-marked-protecteddecision).

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

### What is guarded

A guarded read requires a model that Chronicle admits for direct, event-source-keyed event-log reads with a supported key conversion and a known, nonempty event-type set. Flat projections, unfiltered reducers and self-reducing models without constructor dependencies other than `ILogger<T>` are admitted. Reducers with a filter or other dependencies, joins, nested or child sources, `FromEvery`, by-property keys, stream-keyed projections read as a plain parameter, open-ended event types and Pinned generation delivery are refused. The model needs an `Id` property of a string-like or `Guid` type, which is matched to the command's key or to a `[LookUpBy]` property.

Revisions, redactions, projection definition changes, other event sequences, external state and writes that bypass the kernel's append path are not guarded. A guard detects every matching event appended through the kernel, whoever wrote it. An unguarded writer's own decision is not protected.

Arc claims ownership of the command's Chronicle unit of work and commits or rolls it back as owner. A direct `Commit()` or `Rollback()` of that unit from command code is refused with `ProtectedUnitOfWorkRequiresOwner`. Immediate `IEventLog.Append*` calls inside a handler are not part of the guarded commit: under `On` they are refused; return events instead.

### Read models scoped to one event stream

Declare `[EventStream<TSource>(stream)]` from `Cratis.Chronicle.ReadModels` on a source-keyed model when it always represents one stream. Take it as a plain parameter in `Handle()`, `Provide()` or a `CommandValidator<T>` constructor. Arc derives the source id, source type, stream type and stream id from the command's route, including `[EventStreamId("{Month}")]` templates. The [monthly-limit example](../consistency/scenarios/per-stream.md#a-monthly-limit) includes the declaration, validator and a `ForMonth` query.

| Mode | Per-stream parameter |
| --- | --- |
| `On` | Fresh stream fold, enrolled before the value is returned; validator and handler share one fold |
| `Off` or `Measure` | Plain stream fold, no guard |
| `On` with `[Unprotected("why")]` on the parameter or model | Advisory stream fold, no guard |

A different source or stream, `All`, or a missing/default stream id is refused in every mode with `ReadModelStreamNotOnCommandRoute`. A missing template part returns `400` naming the command property before reading. `[LookUpBy]` is refused on per-stream parameters: the source belongs to the command's route, not the lookup convention. A `DecisionRead<T>` of a declared per-stream model uses that same route; other models remain key-based.

The model is implicitly passive. Queries use `IReadModels.GetInstanceById<T>(EventSourceId, EventStreamId)`, which supplies no guard; key-only reads and watchers are refused. Combining a stream model with a whole-source read on the same source chooses a whole-source guard. All writers must use the definition route: events without its source type are neither folded nor guarded.

### Read a projection within one stream

Prefer a declared per-stream model bound to the command route. When a key or stream can only be computed during acquisition, inject the command-aware `IDecisionReads` into `Provide()` and pass the source id, stream type and stream id explicitly, returning plain state. This command fragment assumes an admitted event-source-keyed `PeriodState` projection with an integer `Value`, and a registered `PeriodValueDecided(int Value)` event:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

[Command, ReadProtection(ReadProtectionMode.On)]
[EventSourceType("reports")]
[EventStreamType("approval")]
[EventStreamId("{Period}")]
public record DecideWithinPeriod(EventSourceId Id, string Period)
{
    public async Task<PeriodState?> Provide(IDecisionReads reads) =>
        (await reads.Get<PeriodState>(Id, "approval", Period, "reports")).Instance;

    public PeriodValueDecided Handle(PeriodState? state) => new(state?.Value ?? 0);
}
```

The command declares `On`. The fold and its append guard use those same dimensions. Arc enrolls the read before `Provide()` returns its plain state and caches identical reads only within that invocation; `Handle()` does not need the token. A whole-source read or another stream's read does not share that cache entry. The optional event source type narrows both the fold and guard. Keep it aligned with the returned event route.

Use `AdmitForStream<T>()` to check the projection shape without reading. Chronicle also admits consistently stream-id-keyed projections through this overload; reading them through a key-only `Get<T>` refuses with `StreamKeyRequiresStreamScope`. An injected `DecisionRead<T>` is narrowed automatically only when `T` declares `[EventStream<TSource>(stream)]`; otherwise it stays key-based. Use the explicit stream overload for another stream or source.

The stream type must be specific, not `EventStreamType.All`. See Chronicle's [stream-scoped decision-read contract](/chronicle/read-models/decision-reads/#scoped-to-one-event-stream) for default-id behavior, key requirements and merged guard widening. The kernel must acknowledge stream scoping; unsupported scoping fails rather than silently folding the whole source.

For the decision matrix, costs, pitfalls and scenarios that put these pieces together, see [Choosing how a command's rule holds](../consistency/index.md), its [scenarios](../consistency/scenarios/index.md) and its [pitfalls](../consistency/pitfalls.md).

## Read models from other providers

Injection is not Chronicle-only. A read model backed by Entity Framework Core or MongoDB is injected into a command exactly the same way, and everything on this page — the three positions, and what nullability means — applies unchanged.

What differs is where the read model is loaded from and what key loads it, including how a command declares its key when there is no Chronicle to resolve one. See [Read models from other providers](./other-providers.md).

## Testing

Specify a decision by calling `Handle(readModel)` directly. To exercise the pipeline, use `CommandScenario<T>` and give state with `Given.ForEventSource(id).ReadModel(instance)`, including for protected plain parameters. The real protected read still runs before the pin supplies its instance; admission and conflict detection are unchanged. Specs never configure decision reads or simulate concurrency. Per-stream models and computed-key reads in `Provide()` use given events. See [Chronicle testing](../../testing/chronicle.md#give-a-protected-read-model-its-state).

## See also

- [Use current state in a command](../../../../scenarios/use-current-state-in-a-command.mdx) — the short recipe.
- [When resolution fails](./failures.md) — every error and what it means.
- [Command validation](../../commands/validation.md) — the rest of Arc's validation model.
