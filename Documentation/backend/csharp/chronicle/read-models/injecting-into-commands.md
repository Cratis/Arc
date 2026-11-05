---
title: Inject read models into Cratis Arc commands
description: Inject Chronicle state into a Cratis Arc validator, Provide, or Handle method. Choose lookup keys, handle missing models, and apply read protection.
---

Inject a Chronicle read model when a Cratis Arc command needs state to reject a request, prepare its inputs, or decide which event to produce. Declare the model in a validator constructor, `Provide()`, or `Handle()`; Arc loads the instance for the selected key before your code uses it.

The ordinary command-key dependency is shared across those three positions in the same command scope. Explicit lookups can select other instances. Start with a plain model parameter rather than `IDecisionReads` or `DecisionRead<T>`; [Command state inputs](command-state-inputs.md) explains the bindings and the acquisition gaps that justify an advanced reader.

:::caution[A plain read guards nothing under Off]
Under read protection `Off`, the default, a plain model parameter supplies a snapshot but captures no boundary to guard a later append. This applies in a validator, `Provide()`, and `Handle()`. Under `On`, Arc reads supported models fresh and guards the append; unsupported models are refused. The next major version of Arc makes `On` the default; see [The default changes in the next major version](../consistency/index.md#the-default-changes-in-the-next-major-version). For uniqueness, use an append-time constraint. [Choosing how a command's rule holds](../consistency/index.md) explains the choice; [Getting started with read protection](../consistency/getting-started.md) shows the setup.
:::

Choose the position by what you need the state to do. The examples below are dependency fragments using application read models, events, and commands. Declarations without `Handle()` show the validator's input shape, not a complete runnable command.

| Position | Use it when | The state is… |
| --- | --- | --- |
| `CommandValidator<TCommand>` | The command should be rejected with a message | a gate |
| `Handle()` | The event you produce is computed from the state | an input |
| `Provide()` | The state has to be combined with fetched data before the decision | an input to acquisition |

## Choose how a command reads state

First establish [which provider owns the model](./other-providers.md#which-provider-resolves-a-read-model). For a Chronicle-owned model, choose the binding that expresses its identity:

| State you need | Declaration |
| --- | --- |
| The instance for the command's event source | A plain read-model parameter |
| An instance identified by another public command property | `[LookUpBy(nameof(Command.Property))]` on the parameter |
| The instance for the selected caller | `[LookUpBy<CallerSubject>]` for the exact `sub` claim, or an application claim provider |
| State belonging to the command's declared stream | A per-stream model taken as a plain parameter |
| A key or stream id none of those bindings can express | `IDecisionReads` or `DecisionRead<T>` directly in `Handle()`, with a reasoned ARCCHR0040 suppression and tracking-feature link |

Use `[ReadProtection(ReadProtectionMode.On)]` when a supported state read must guard the later append. The key binding does not enable that policy. Keep advanced readers for acquisition gaps, such as the [stream example](#read-a-projection-within-one-stream); a plain parameter keeps the decision independent of loading state.

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

Use the same parameter shape for classic reducers, [self-reducing models](./index.md#self-reducing-read-models) with Chronicle 19.58.0+, fluent projections, and model-bound projections, including passive models. A self model needs no separate reducer registration in Arc: Chronicle's discovered reducer set supplies it.

Under `Off`, Arc loads existing instances through `IReadModels` and releases them before injection; absent self-model state resolves to null. These snapshots do not protect a later append against concurrent changes. Under `On`, Arc reads supported models fresh and guards the append, refusing reducers it cannot protect. See [materialized and passive paths](./index.md#materialized-and-passive-paths) for freshness and release differences.

## Key a read by the caller

Use a caller-derived key when the model belongs to the authenticated caller rather than an identity the client can choose. The generic lookup attribute reads the selected caller snapshot, so the command needs no client-supplied key property. This fragment uses the same `LedgerBalance` and `LedgerSettled` application types as above:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

[Command]
public record SettleMyLedger
{
    public LedgerSettled Handle([LookUpBy<CallerSubject>] LedgerBalance balance) =>
        new(balance.Balance);
}
```

`CallerSubject` reads exactly `sub`; it does not map `NameIdentifier`, fall back to a name, or inspect actor-chain claims. It requires an authenticated, non-system caller and one distinct non-empty value. Repeated identical values are one value; different values are ambiguous. Claim types and values use ordinal comparison.

For an application claim, derive from the abstract `CallerClaim`:

```csharp
using Cratis.Arc.Commands;

public sealed class CallerLogin() : CallerClaim("login");
```

Then use `[LookUpBy<CallerLogin>]` on the read-model parameter. For a computed key, implement `IReadModelKeyProvider.Resolve(ReadModelKeyContext)` and return `ReadModelKeyLookup.Found(key)`, `.Missing`, or `.Ambiguous`. `Found(null)` is invalid; a default result is missing. `Caller.Single(claimType)` applies the same distinct-value rule as `CallerSubject`.

A provider receives only the immutable `Caller` and target `ReadModelType`. It must be a stateless, thread-safe class with a public parameterless constructor because Arc shares one instance. It cannot use services, dependency injection, IO, or an ambient principal. If acquiring the key needs any of those, this provider shape does not fit.

Arc captures the selected caller before command context providers run. A scheme-selected principal or an on-behalf-of represented identity is the caller, not the actor. Later observable principal replacement or in-place claim mutation refuses resolution. This also applies to validation preflight and validation nested in execution.

The key selects the instance in `Off`, `Measure` and `On`; it does not enable protection. `Off` and `Measure` remain unguarded. Under `On`, the existing admission, freshness and concurrency checks still apply. `[Unprotected]` exempts guarding, not caller/key checks. A provider key must have safe, non-empty exact wire text in every mode: wildcard, unspecified, surrounding whitespace and ambiguous `#` text are refused.

A missing or ambiguous key, changed caller, or throwing provider produces an authorization refusal. Arc does not fall back to a convention or client key, even for a nullable parameter. Only after a valid key is found do the ordinary nullable-versus-required absence rules apply.

Declare one lookup form: do not combine the generic and string attributes or declare two providers. An override replaces the same lookup declaration on its base method; different provider types or mixed forms remain conflicts. A `Provide()` value of the parameter's type causes `InvalidLookUpBy` rather than replacing the caller-keyed read. Neither lookup form can redirect a per-stream model away from the command's own source. See [lookup failures](./failures.md#readmodelkeyresolutiondenied).

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

A valid key identifies which instance to load; it does not prove the instance exists. Use nullability to declare whether your code handles missing state. A nullable parameter does not allow a missing or invalid key, and passive projections can return a default-valued instance rather than null when no events match.

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

Under read protection `On`, a supported plain `T` parameter in `Handle()`, `Provide()`, or a validator constructor becomes a guarded read unless explicitly marked unprotected. You do not need to replace it with a token parameter. [Getting started](../consistency/getting-started.md) shows the setup, and [Reads](../consistency/reads.md) lists what each read becomes.

| You write | Under `Off` or `Measure` | Under `On` |
| --- | --- | --- |
| `Handle(BookStock? stock)`, or a validator constructor parameter | Plain read | Guarded read |
| `[Unprotected("why")]` on the parameter or the model | Plain read | Advisory read: stored, unguarded |
| `[LookUpBy(nameof(Property))]` on a parameter | Selects the key | Selects the key |
| `IDecisionReads.Get<T>(key)` in `Handle()` for computed keys, with a reasoned ARCCHR0040 suppression linked to the tracking feature | Guarded for `[ProtectedDecision]` commands | Guarded read |
| Existing advanced `DecisionRead<T>` with `[ProtectedDecision]` | Guarded read for that command | Guarded read. Prefer a plain parameter |
| `IReadModels`, `IEventStore` or `IEventLog` reads | Allowed | Refused unless the method is `[Unprotected("why")]` |

A conflict returns `concurrencyViolation` with the detail `ReadChanged`, and the command is not retried. See [Refusals](../consistency/refusals.md).

`Validate` is advisory: it reads fresh state and does not append or guard, so a successful validation response does **not** promise that `Execute` will commit.

### Existing advanced commands marked [ProtectedDecision]

For new code, prefer a plain read-model parameter under `On`. Existing commands outside `On` can opt in individually with `[ProtectedDecision]` from `Cratis.Arc.Chronicle.ReadModels`. Those commands take `DecisionRead<T>` in `Handle()` with a reasoned ARCCHR0040 suppression and tracking-feature link, then use `.Instance` and `.Exists`. To guard a different event source, use the command-aware `IDecisionReads.Get<T>(otherKey)`.

Arc fixes the protection profile when the pipeline enters each command, including nested commands. An unmarked command under `Off` keeps its plain reads, and `IDecisionReads.GetDetached<T>` returns a detached snapshot as Chronicle's own reader does. Inside a `[ProtectedDecision]` command, `GetDetached<T>` is refused because that snapshot is not enrolled in the decision. A `DecisionRead<T>` returned from `Provide()` must have been issued in that invocation.

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

Before guarding a read, Chronicle checks whether it can fold the model from the event log and identify the events that could change it. The model needs a supported event-source key conversion, a known nonempty event-type set, and an `Id` property of a string-like or `Guid` type matching the selected lookup key.

Flat projections, unfiltered reducers, and self-reducing models without constructor dependencies other than `ILogger<T>` are admitted. Reducers with filters or other dependencies are refused, as are joins, nested or child sources, `FromEvery`, by-property keys, stream-keyed projections read as a plain parameter, open-ended event types, and Pinned generation delivery. Making a model injectable does not remove these protection limits.

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

Prefer a declared per-stream model bound to the command route. When supported parameters cannot express a computed key or stream id, take the command-aware `IDecisionReads` directly in `Handle()` and suppress ARCCHR0040 with a reason and a link to the tracking feature. This compiled command fragment assumes the admitted event-source-keyed `AccountWithdrawals` model with `Total` and the `MoneyWithdrawn(decimal Amount)` event. It computes a month from the supplied date:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

[Command, ReadProtection(ReadProtectionMode.On)]
[EventSourceType("BankAccount")]
[EventStreamType("Transactions")]
[EventStreamId("{Month}")]
public record InspectMonth(EventSourceId Id, string Date)
{
    public string Month => Date[..7];

#pragma warning disable ARCCHR0040 // Computed month; https://github.com/Cratis/Arc/issues/3620 tracks acquisition gaps.
    public async Task<MoneyWithdrawn> Handle(IDecisionReads reads)
    {
        var state = await reads.Get<AccountWithdrawals>(Id, "Transactions", Month, "BankAccount");
        return new(state.Instance?.Total ?? 0);
    }
#pragma warning restore ARCCHR0040
}
```

The command declares `On`. The fold and its append guard use those same dimensions. Arc enrolls the read before `Get` returns to the asynchronous `Handle()` and caches identical reads only within that invocation. The pipeline awaits the returned event before committing the guarded unit of work. A whole-source read or another stream's read does not share that cache entry. The optional event source type narrows both the fold and guard. Keep it aligned with the returned event route.

Use `AdmitForStream<T>()` to check the projection shape without reading. Chronicle also admits consistently stream-id-keyed projections through this overload; reading them through a key-only `Get<T>` refuses with `StreamKeyRequiresStreamScope`. An injected `DecisionRead<T>` is narrowed automatically only when `T` declares `[EventStream<TSource>(stream)]`; otherwise it stays key-based. Use the explicit stream overload for another stream or source.

The stream type must be specific, not `EventStreamType.All`. See Chronicle's [stream-scoped decision-read contract](/chronicle/read-models/decision-reads/#scoped-to-one-event-stream) for default-id behavior, key requirements and merged guard widening. The kernel must acknowledge stream scoping; unsupported scoping fails rather than silently folding the whole source.

For the decision matrix, costs, pitfalls and scenarios that put these pieces together, see [Choosing how a command's rule holds](../consistency/index.md), its [scenarios](../consistency/scenarios/index.md) and its [pitfalls](../consistency/pitfalls.md).

## Reads are awaited

Arc binds a read model, a `DecisionRead<T>` token or a per-stream model into `Handle()` and `Provide()` by awaiting the Chronicle read, so a command that runs on a single-threaded scheduler yields while the read is pending instead of blocking it. This holds for framework-owned parameter binding only, and only when the caller awaits the command pipeline itself.

Constructor injection, validators built by synchronous factories, other `IServiceProvider` lookups and custom synchronous resolvers are not changed by this; they keep their existing synchronous behavior. The supported ways to read state described above stay the same. See [the command pipeline](../../commands/command-pipeline.md#how-parameters-are-bound) for the resolver contract.

## Read models from other providers

Entity Framework Core and MongoDB can also supply read-model dependencies in a validator, `Provide()`, or `Handle()`. Their missing-instance behavior follows the same nullable-versus-required choice.

The provider still controls loading and key conversion. Chronicle's protected event reads do not make an EF Core row or MongoDB document part of its guarded commit. Use [Read models from other providers](./other-providers.md) to configure ownership and declare a command key without Chronicle.

## Testing

Specify a decision by calling `Handle(readModel)` directly. To exercise the pipeline, use `CommandScenario<T>` and give state with `Given.ForEventSource(id).ReadModel(instance)`, including for protected plain parameters. The real protected read still runs before the pin supplies its instance; admission and conflict detection are unchanged. Specs never configure decision reads or simulate concurrency. Per-stream models and computed-key reads in `Handle()` use given events. See [Chronicle testing](../../testing/chronicle.md#give-a-protected-read-model-its-state).

For caller-derived keys, give `CommandScenario<T>` a selected principal through its scenario services before executing the command; there is no `GivenCaller` helper. Arrange the matching read-model state under that caller's key. Direct `Handle(readModel)` calls still test the decision without exercising lookup. To test refusal, execute through the pipeline with a missing or ambiguous caller claim rather than passing a fabricated model.

## See also

- [Use current state in a command](../../../../scenarios/use-current-state-in-a-command.mdx) — the short recipe.
- [When resolution fails](./failures.md) — every error and what it means.
- [Command validation](../../commands/validation.md) — the rest of Arc's validation model.
