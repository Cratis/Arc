---
uid: Arc.Testing.ChronicleExtension
title: Cratis Arc command testing with Chronicle
description: Use the optional Cratis Chronicle testing extension to seed command state and assert event appends in Cratis Arc without an external server.
---

Use Cratis Chronicle's optional testing extension when a Cratis Arc command needs seeded event history or read-model state, or when its test must check an event append. It adds an in-process Chronicle event log to `CommandScenario<TCommand>` without an external server.

For a decision alone, call `Handle()` directly and use [returned-decision assertions](./command-assertions.md). For a standalone Arc pipeline, use [Cratis.Arc.Testing](./command-scenario.md) without this extension. Add Chronicle only when the test needs its behavior.

Referencing `Cratis.Arc.Chronicle.Testing` or the `Cratis.Testing` meta-package enables automatic discovery of `ChronicleCommandScenarioExtender`, an `ICommandScenarioExtender`. Keep the same `CommandScenario<TCommand>` class. In a Cratis Specification, seed state in `Establish()`, execute once in `Because()`, and assert the `CommandResult` and appends in `[Fact]` methods. The seeding API depends on the command's read-protection declarations, as described below.

## Package

```xml
<PackageReference Include="Cratis.Specifications.XUnit" />
<PackageReference Include="Cratis.Arc.Chronicle.Testing" />
```

Or via the meta-package:

```xml
<PackageReference Include="Cratis.Specifications.XUnit" />
<PackageReference Include="Cratis.Testing" />
```

## Basic usage

The examples are spec fragments for an existing application, not standalone projects. They assume the commands, event types, read-model projections or reducers, and any named constraints are already defined and discovered. For a complete lesson, start with [an event-sourced command](./event-sourced-commands.md); use the [standalone testing checkpoint](./index.md) if the scenario lifecycle is new to you.

Import `Cratis.Arc.Testing.Commands` for Arc assertions, `Cratis.Arc.Chronicle.Testing.Commands` for the C# 14 extension properties and capture assertions, `Cratis.Chronicle.Testing.EventSequences` for event-log assertions, and your normal `Cratis.Specifications` / `Xunit` namespaces. Event IDs and sequence numbers live in `Cratis.Chronicle.Events` and `Cratis.Chronicle.EventSequences`. Dispose each scenario in `Destroy()` as shown in [command scenarios](./command-scenario.md#disposal).

For an ordinary scenario, seed log history with `EventScenario.Given` and injected read-model state with `Given`. Commands that declare read protection or protected dependencies automatically use decision mode: one shared log supplies seeded history, reads, and commits. In that mode, seed with `Given`; `EventScenario` throws. See [protected decision scenarios](#protected-decision-scenarios) before arranging such a command.

| Property | Type | Purpose |
| -------- | ---- | ------- |
| `Given` | `CommandScenarioChronicleGivenBuilder<TCommand>` | Seed injected state; in decision mode, `Events(...)` seeds the shared log |
| `EventScenario` | `EventScenario` | Seed the ordinary scenario's event log; unavailable in decision mode |
| `EventLog` | `IEventLog` | Append events or assert stored history in either mode |
| `EventSequence` | `IEventSequence` | Use Chronicle's event-sequence assertion helpers in either mode |
| `AppendedEvents` | `IReadOnlyList<AppendedEventWithResult>` | Captured appends; ordinary mode includes log setup, while decision mode captures successful command appends |

The first example checks a successful registration with no prior history:

```csharp
public class when_registering_author : Specification
{
    readonly EventSourceId _authorId = EventSourceId.New();
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterAuthor(_authorId, "Jane Austen"));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_appended_registered_event() =>
        _scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(_authorId);
}
```

The spec executes the command once and then asserts both the Arc result and the Chronicle fact that was recorded.

## Seeding pre-existing events with `Given`

For an ordinary scenario, use `_scenario.EventScenario.Given` to append prior events before the command runs. `ForEventSource` selects the identity whose history receives them. In decision mode, use `_scenario.Given` instead; it seeds the shared log.

This example assumes the application's validator rejects a repeated registration with the message `"Author is already registered"`:

```csharp
public class when_registering_author_with_same_name : Specification
{
    readonly EventSourceId _authorId = EventSourceId.New();
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    CommandResult _result = default!;

    Task Establish() =>
        _scenario.EventScenario.Given
            .ForEventSource(_authorId)
            .Events(new AuthorRegistered("Jane Austen"));

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterAuthor(_authorId, "Jane Austen"));

    [Fact] void should_not_succeed() =>
        _result.ShouldNotBeSuccessful();

    [Fact] void should_report_the_existing_author() =>
        _result.ShouldHaveValidationErrorFor("Author is already registered");

    [Fact] Task should_not_have_appended_a_second_event() =>
        _scenario.EventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```

The message assertion identifies the intended rejection; a missing dependency must not make the spec pass. In ordinary mode, event-log seeding does not populate injected read models. If the validator reads one, also arrange its state with the [read-model `Given` builder](#testing-commands-that-take-read-model-dependencies).

## EventLog assertion helpers

Chronicle's `EventSequenceShouldExtensions` extend `IEventSequence`. Call them on `_scenario.EventLog` or `_scenario.EventSequence`. They read the log, including pre-existing events, rather than only the latest command's output.

| Call shape (all return `Task`) | Meaning |
| --- | --- |
| `ShouldHaveAppendedEvent<TEvent>()` | An event of that type exists anywhere in the sequence |
| `ShouldHaveAppendedEvent<TEvent>(eventSourceId)` | An event of that type exists for that source |
| `ShouldHaveAppendedEvent<TEvent>(eventSourceId, predicate)` | A matching event for that source satisfies `Func<TEvent, bool>` |
| `ShouldHaveAppendedEvent<TEvent>(sequenceNumber)` | That exact global sequence position contains the type |
| `ShouldHaveAppendedEvent<TEvent>(sequenceNumber, eventSourceId, predicate)` | That exact position also belongs to the source and satisfies the predicate |
| `ShouldHaveTailSequenceNumber(expected)` | The log's tail equals the expected `EventSequenceNumber` |

Content overloads also accept `Action<TEvent>` assertions. An action validates the first matching event; a predicate searches for a satisfying event. For all overloads, see the [Chronicle event assertions reference](/chronicle/testing/events/assertions/).

## Protected decision scenarios

Create a plain `new CommandScenario<T>()` for a command whose decision depends on protected event history. Read protection checks that relevant history used for a decision has not changed before commit; the scenario selects the appropriate log before you seed it.

Commands declaring read protection `On` or `Measure`, `[ProtectedDecision]`, decision or stream dependencies, per-stream read models, or guarded generated parameters automatically use decision mode. Selection follows the command's runtime-resolved assembly/type declarations, including `<CratisReadProtection>`. The pipeline still applies the host's runtime cap on each execution: selecting the shared log does not add a guard to an `Off` or `Measure` command.

Under `On`, validators with read-model parameters run as in production, including when only the validator reads state. Outside `On`, an existing `[ProtectedDecision]` command runs only [parameterless validators](../chronicle/consistency/validators.md#commands-marked-protecteddecision). Application specs give state and assert outcomes: they never configure decision reads or simulate concurrent writers. Arc and Chronicle prove concurrency handling in their own framework specs.

The selected in-process log is shared by seeded events, protected reads and command commits. `scenario.AppendedEvents` and scenario-level `ShouldHaveAppendedEvent` count successful **command** appends in this setup, not seeded events. `scenario.EventLog` and `scenario.EventSequence` point at the shared log, so log assertions see all stored events.

Use `Given.ForEventSource(...).Events(...)` to seed the shared history. `scenario.EventScenario` throws in decision mode because there is no separate event scenario over that store. Selection happens during construction, before any seeds exist. For an untyped `CommandScenario<object>` that will execute protected commands, call `EnableDecisionReads()` before seeding or the first `Execute` or `Validate`; the shared log then supports every command executed by that scenario. Ordinary scenarios retain their separate `EventScenario` and cumulative append capture.

Do not invent sequence numbers or decision tokens in the spec. Chronicle obtains those tokens from the real read to check the decision at commit.

Keep server, storage-backend, and authorization-middleware checks in integration tests. The guard also has consistency exclusions: revise/redact, generation migration, projection-definition changes, external state, and writes that bypass the kernel's serialized append path are outside it.

A guard detects every matching event appended through the kernel, whoever wrote it. It does not protect an unguarded writer's own decision, whether that writer uses a plain read, `[Unprotected]`, or a direct append. Let Arc complete the protected command's unit of work: direct SDK `Rollback()` or `Commit()` calls, including from an aggregate, fail with `ProtectedUnitOfWorkRequiresOwner`.

For production admission, opt-out, and advisory validation behavior, see [decision reads in commands](../chronicle/read-models/injecting-into-commands.md#decision-reads-for-event-dependent-commands).

### Reads that do not guard the decision

A command may read advisory state without guarding it. This includes an `[Unprotected]` read-model parameter, an allowed `IReadModels.GetInstanceById` call, or an existing `DecisionRead<T>` in an `[Unprotected]` command.

In decision mode, seed events once with `Given.ForEventSource(...).Events(...)` or append to `scenario.EventLog` before `Execute`. Both protected and advisory reads use that log. The model's own projection or reducer derives advisory state from it, as from a caught-up production store. A nullable model stays `null` if the source has only events that its projection or reducer does not handle.

The scenario resolves each advisory read from one event source's events. If an event property supplies the model's key, the resulting value is returned for that source even when the key differs from the source id. Another source's events do not contribute. If the selected source produces several model instances with different keys, the command fails. Parts that depend on other sources, such as children, remain empty.

Pin the complete value with `.ReadModel(...)` when the case needs that state. A pin takes precedence over events for the same model and event source, regardless of whether you seed the events before or after it.

### Give a protected read model its state

Use `.ReadModel(...)` to give a plain read-model parameter known state under `[ReadProtection(On)]`, including when a validator takes the model in its constructor. Chronicle still performs the protected read first. It checks that the model supports protection (admission), enrolls the read in the transaction, and obtains the decision token used for conflict detection.

The pin supplies the value, not a fabricated guard. It takes precedence over seeded events for that model type and resolved key.

For example, given an admitted `LedgerBalance` model and a `SettleLedger` command taking it as a plain `Handle` parameter:

```csharp
public class when_settling_a_funded_ledger : Specification
{
    readonly EventSourceId _ledgerId = EventSourceId.New();
    readonly CommandScenario<SettleLedger> _scenario = new();
    CommandResult _result = default!;

    void Establish() => _scenario.Given.ForEventSource(_ledgerId)
        .ReadModel(new LedgerBalance(_ledgerId, 100m));

    async Task Because() => _result = await _scenario.Execute(new SettleLedger(_ledgerId));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact] async Task should_settle_the_given_balance() =>
        await _scenario.ShouldHaveAppendedEvent<SettleLedger, LedgerSettled>(_ledgerId, fact => fact.Balance == 100m);

    async Task Destroy() => await _scenario.DisposeAsync();
}
```

This fragment assumes the application's `LedgerBalance(EventSourceId Id, decimal Balance)` and `LedgerSettled(decimal Balance)` types. No testing switch is needed.

Use routed events for per-stream models rather than source-only pins. Advanced `DecisionRead<T>` parameters also need events: pins throw `PinnedReadModelCannotProvideDecisionToken`. An explicit `IDecisionReads.Get<T>(key)` derives state from events, not pins. Use that call inside `Handle()` only for keys parameter binding cannot express, suppressing ARCCHR0040 with a reason and a link to the tracking feature.

### Check that Chronicle admits the decision reads

Add an admission spec to catch models that cannot support protected reads before deployment. `DecisionReadAdmissions` applies Chronicle's admission rules to the artifacts discovered for the test run without executing commands.

Chronicle admits flat, event-source-keyed projections and unfiltered reducers with no constructor dependencies except `ILogger<T>`; self-reducing models are exempt from that constructor restriction. Models with children or nested objects, joins, unsupported reducer dependencies or filters, more than one projection, or routing by an event property compile but are refused for protected reads.

Arc also checks discoverable parameter shapes at startup. Under effective `On`, refusals stop the host unless `ReadProtection:StartupAdmissionCheck` is `Log` or `Off`. That check applies the runtime cap; the test helper checks the declared policy regardless of the cap. Calls to `Get<T>` inside method bodies still need explicit admission checks or scenarios.

Use one assembly-wide spec to check the discoverable protected commands:

```csharp
public class when_checking_decision_read_admission
{
    [Fact]
    public void should_admit_every_protected_decision_read() =>
        DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(typeof(RegisterAuthor).Assembly);
}
```

A failure throws `DecisionReadsAreRefused`, listing each command, read model, and `DecisionReadRefusalReason`. Use `ShouldAdmitDecisionReadsOf(params Type[])` to check selected commands, or `FindRefused` to inspect the list without throwing.

The helper checks `DecisionRead<T>` and `IEnumerable<DecisionRead<T>>` parameters of `Handle` and `Provide` on `[ProtectedDecision]` commands and commands declaring read protection `On`. Under `On`, it also checks plain Chronicle-backed model parameters and discoverable validator constructor parameters, except reads marked advisory on the parameter or model. It honors command and assembly declarations, ignores the runtime cap, includes inherited methods, and skips abstract commands in assembly scans.

The helper cannot inspect `IDecisionReads.Get<T>(key)` calls inside method bodies. Give those models their own `Admit<T>()` check or run the command in a scenario. Projections in the test assembly also take part in discovery; adding a spec-local projection for a production model can make that model ambiguous.

### Assert stream completion

For a command returning `CompleteStream`, `scenario.ShouldHaveCompletedStream(type, id)` accepts a successful completion with or without an event source scope. To assert the exact owner, pass a `ClosedStreamScope` instead; see [completion assertions](../chronicle/commands/completing-streams.md#assert-completion-in-a-command-scenario). Other sources using the same stream type and id remain open after a scoped close. A command without an event source id still closes that pair for every source.

For per-stream parameters, seed events with `OnRoute(...)`. The static admission helper uses stream admission for these parameters under `On`. Advanced explicit stream folds inside `Handle()` can use `AdmitForStream<T>()`; the helper cannot inspect method-body calls.

:::note[Advanced: framework concurrency verification]
`AppendConcurrently(source, events)` exists to verify the framework's concurrency handling and is hidden from normal editor completion. Use it only for a specific, explicit need, not in ordinary application specs. Its behavior is unchanged: it works with or without decision mode, queues facts before the first execution, and appends them after response processing but before the owner's commit. The route overload preserves routing. Nested commands do not drain the outer queue. The hook does not add a guard to an unguarded command, and it refuses custom execution scopes whose ordering it cannot guarantee.
:::

## Transactional commands in tests

The harness runs commands with the same [transactional scope](../commands/transactional-commands.md) as production: the events a command returns — and appends through `eventLog.Transactional` — commit atomically when it succeeds and roll back when it fails, including when a unique constraint rejects the commit. Immediate appends through `IEventLog`/`IEventStore.EventLog` land right away and are final, but a failed one fails the command. That gives specs two natural assertions: the intended rejection and the absence of committed events.

The following fragment assumes the application's registered unique-name constraint covers `AuthorRegistered.Name` across authors. `AuthorConstraintNames.UniqueName` is the application's string constant for that constraint's registered name, not an Arc-provided name. Use your actual constraint-name constant; seeding an event alone does not register a constraint.

```csharp
public class when_registering_author_with_taken_name : Specification
{
    readonly EventSourceId _existing = EventSourceId.New();
    readonly EventSourceId _author = EventSourceId.New();
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    CommandResult _result = default!;

    Task Establish() =>
        _scenario.EventScenario.Given
            .ForEventSource(_existing)
            .Events(new AuthorRegistered("Jane Austen"));

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterAuthor(_author, "Jane Austen"));

    [Fact] void should_not_succeed() =>
        _result.ShouldNotBeSuccessful();

    [Fact] void should_be_rejected_by_the_uniqueness_constraint() =>
        _result.ShouldHaveConstraintViolationFor(AuthorConstraintNames.UniqueName);

    [Fact] async Task should_append_nothing_for_the_rejected_author() =>
        (await _scenario.EventLog.HasEventsFor(_author)).ShouldBeFalse();
}
```

The named assertion checks both `ConstraintViolation` and the exact constraint name, so a dependency-resolution failure or a different constraint cannot satisfy it. Keep the no-events assertion as well: it verifies the separate storage contract.

When inspecting `AppendedEvents`, account for the append style. Immediate appends appear as they happen. Returned events and `Transactional` appends appear as one batch when the command's transaction commits.

A successful plain `IEventLog.Append` or `IEventStore.EventLog` append remains in the log even if the command later fails. Test that behavior explicitly when a handler uses immediate appends; do not expect the transaction to reverse them.

## Commands for occurrences

`IOccurrenceCommandExecutor` needs a kernel that advertises `effect-receipts`. The in-process kernel from `Cratis.Chronicle.Testing` has a capability switch (`KernelCapabilitiesForTesting.Current()`, `Old()`, `Unknown()`) and stores receipts, so a spec can assert that a redelivered effect key returns `AlreadyRecorded`, that a business refusal is recorded as `Refused`, that a handler exception leaves no receipt, and that the executor throws `EffectReceiptsNotSupported` against an old or unknown kernel. See [Commands for occurrences](../chronicle/commands/occurrence-commands.md) for the classification and for the commit styles the executor refuses.

## Testing commands that use EventForEventSourceId

To inspect a direct `Handle()` return value, use [returned-decision assertions](./command-assertions.md#assert-a-returned-events-target): `ShouldTarget(id)`, `ShouldBeEvent<TEvent>()`, and `ShouldContainEvent<TEvent>(id)` check the declared target and payload. They do **not** prove an append; keep the scenario assertions below when the append contract matters.

When a command handler returns `EventForEventSourceId` or `IEnumerable<EventForEventSourceId>`, it can target sources other than the command's own source. **Both** event-log assertions and the scenario-level helpers below support filtering by event source ID. Choose log assertions to verify readable stored events; choose scenario helpers to inspect appends captured through the client-side `AppendOperations` observable.

In ordinary mode, capture starts at scenario construction, not at each `Execute`. `AppendedEvents` accumulates event-log seeds and later executions. Use a fresh scenario, a source unique to the command, or an explicit baseline so a matching seeded event cannot satisfy the command's append assertion. Decision mode captures successful command appends without the seeds, but log assertions still see the full stored history.

| Method | Asserts that... |
| ------ | --------------- |
| `ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId)` | At least one event of type `TEvent` was appended for the given `EventSourceId` |
| `ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId, predicate)` | Same, and the event also satisfies the predicate |
| `ShouldHaveTailSequenceNumber<TCommand>(expected)` | The highest sequence number among all captured events equals `expected` |

### Example: Single cross-source event

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

public class when_migrating_customer_to_new_id : Specification
{
    readonly CommandScenario<MigrateCustomerToNewId> _scenario = new();
    readonly EventSourceId _oldId = EventSourceId.New();
    readonly EventSourceId _newId = EventSourceId.New();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new MigrateCustomerToNewId(_oldId, _newId));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_appended_migrated_event_for_new_id() =>
        _scenario.ShouldHaveAppendedEvent<MigrateCustomerToNewId, CustomerMigrated>(_newId);

    [Fact] Task should_reference_old_id_in_event() =>
        _scenario.ShouldHaveAppendedEvent<MigrateCustomerToNewId, CustomerMigrated>(
            _newId,
            e => e.OldCustomerId == _oldId);
}
```

### Example: Multiple cross-source events (fund transfer)

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

public class when_transferring_funds : Specification
{
    readonly CommandScenario<TransferFunds> _scenario = new();
    readonly EventSourceId _fromAccount = EventSourceId.New();
    readonly EventSourceId _toAccount = EventSourceId.New();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new TransferFunds(_fromAccount, _toAccount, 250m));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_debited_from_account() =>
        _scenario.ShouldHaveAppendedEvent<TransferFunds, FundsDebited>(_fromAccount);

    [Fact] Task should_have_credited_to_account() =>
        _scenario.ShouldHaveAppendedEvent<TransferFunds, FundsCredited>(_toAccount);

    [Fact] Task should_have_debited_correct_amount() =>
        _scenario.ShouldHaveAppendedEvent<TransferFunds, FundsDebited>(_fromAccount, e => e.Amount == 250m);

    [Fact] Task should_have_appended_two_events() =>
        _scenario.ShouldHaveTailSequenceNumber<TransferFunds>(1ul);
}
```

> **Sequence numbering applies here too**: `ShouldHaveTailSequenceNumber` checks the highest sequence number across captured appends and fails if none were captured. A tail of `1` means two total events only for a fresh, contiguous sequence starting at `0`. It is not a general count of events from the last command.

The `AppendedEvents` extension property gives you the raw list if you need to write custom assertions:

```csharp
[Fact]
void should_have_exactly_two_events() =>
    _scenario.AppendedEvents.Count.ShouldEqual(2);
```

## Testing commands that take read model dependencies

When a handler, `Provide` method, or `CommandValidator<T>` takes a read model, arrange the state that parameter should receive. A read model is state derived from events through a projection or reducer. Arc resolves it for the command's event source id using `IProjectionFor<T>`, `IReducerFor<T>`, or a model-bound projection; [use current state in a command](/arc/scenarios/use-current-state-in-a-command/) explains the application pattern.

Use `_scenario.Given.ForEventSource(id)` rather than mocking `IReadModels`. Supply events when their history matters to the case, or pin an instance when the case only needs a known value. The examples below use ordinary mode; [protected decision scenarios](#protected-decision-scenarios) use a shared log instead of separate read-model seeds.

### Seeding read model state from events

State the events that happened for the event source. Any read model a command injects for that source is materialized from those events through its own reducer or projection — you never name the read model type here, just as you never do in production:

```csharp
public class when_withdrawing_with_sufficient_funds : Specification
{
    readonly EventSourceId _accountId = EventSourceId.New();
    readonly CommandScenario<Withdraw> _scenario = new();
    CommandResult _result = default!;

    void Establish() =>
        _scenario.Given
            .ForEventSource(_accountId)
            .Events(new MoneyDeposited(100m), new MoneyDeposited(50m));

    async Task Because() =>
        _result = await _scenario.Execute(new Withdraw(_accountId, 120m));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();
}
```

One `Events(...)` call feeds each read model built from those events. If the command injects both `AccountBalance` and `AccountStatement`, their own projections or reducers derive both values from the same deposits. You do not name either read-model type in the setup.

This is distinct from `_scenario.EventScenario.Given` above: that seeds the event **log** (prior facts the handler may read or append against); this seeds the **read model state** the command observes through its injected parameters. [Observable read-model queries](./query-scenario.md#observe-chronicle-read-models) use a private log that combines read-model seeds and successful appends from the command log, without changing the command log.

### Pinning a read model instance

When you would rather assert against a known value than express the events behind it, pin the instance directly. The read model type is inferred from the value:

```csharp
void Establish() =>
    _scenario.Given
        .ForEventSource(_accountId)
        .ReadModel(new AccountBalance(150m));
```

Pinned instances take precedence over seeded events for the same read model type and source. A model seeded for one source is not visible to another: resolving an unseeded source returns `null`. A registered nullable dependency can receive that null; a required dependency instead rejects the command as unavailable. See [the dependency-unavailable assertion distinction](./command-scenario.md#dependency-unavailable-is-not-a-business-rule-rejection).

In ordinary mode, these seeds control injected read-model resolution. Appending through `EventLog` or executing a command does not add events to that separate collection. Arrange both log history and injected state if the case needs both. Observable queries combine the histories in a private log, so seeding the same facts in both places processes them twice for observation.

Pins require a readable key. [Observable queries support children, joins, and reducers in default mode](./query-scenario.md#children-joins-and-shared-command-scenarios). For projection behavior beyond those scenario boundaries, use Chronicle read-model scenarios or hosted integration specs.

## Multiple events

For exact-position assertions, account for seeded events. This example assumes `CompleteOrder` appends one `OrderCompleted` after the seeded `OrderPlaced`, giving two total events:

```csharp
public class when_completing_order : Specification
{
    readonly CommandScenario<CompleteOrder> _scenario = new();
    readonly EventSourceId _orderId = EventSourceId.New();
    CommandResult _result = default!;

    Task Establish() =>
        _scenario.EventScenario.Given
            .ForEventSource(_orderId)
            .Events(new OrderPlaced("item-1", 3));

    async Task Because() =>
        _result = await _scenario.Execute(new CompleteOrder(_orderId));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_two_events_including_the_seed() =>
        _scenario.EventLog.ShouldHaveTailSequenceNumber(new EventSequenceNumber(1));

    [Fact] Task should_have_appended_completed_event() =>
        _scenario.EventLog.ShouldHaveAppendedEvent<OrderCompleted>(new EventSequenceNumber(1));
}
```

> **Sequence numbering**: Sequence numbers are zero-based. `EventSequenceNumber.First` is `0`. The second event is `new EventSequenceNumber(1)`, the third `new EventSequenceNumber(2)`, and so on. `ShouldHaveTailSequenceNumber` reports the number of the *last* appended event — so two total events means a tail of `1`.

## What the extension provides

`ChronicleCommandScenarioExtender` registers event types discovered from loaded test-process assemblies and an event log backed by the in-process Chronicle kernel. It also registers `IEventSequence` and `IReadModels`, allowing handlers, validators, and `Provide` methods to receive read models from seeded events or pinned instances. Decision mode additionally uses the shared store for protected reads.

The extension properties expose that setup without a different scenario base class:

| Property | Type | Purpose |
| -------- | ---- | ------- |
| `Given` | `CommandScenarioChronicleGivenBuilder<TCommand>` | `ForEventSource(id).Events(...)` or `.ReadModel(...)` seeds command state |
| `EventScenario` | `EventScenario` | Seeds the ordinary event log; throws in decision mode |
| `EventLog` | `IEventLog` | Accesses the selected log for appending and stored-history assertions |
| `EventSequence` | `IEventSequence` | Accesses the selected sequence for Chronicle assertions |
| `AppendedEvents` | `IReadOnlyList<AppendedEventWithResult>` | Supplies capture assertions; includes log setup in ordinary mode, successful command appends only in decision mode |

For a complete test that combines returned events and external work, continue to [operation recovery after a Chronicle rejection](./command-operations-with-chronicle.md). For read-model query behavior after a command, use [observable query scenarios](./query-scenario.md#observe-chronicle-read-models).
