---
title: Slice testing API
description: Seed command state, execute once, and assert the outcome through a framework-neutral Slice facade.
---

Use `Slice<TCommand>` to express a command test as given state, one execution, and outcome assertions without a test-framework base class. It wraps [command scenarios](command-scenario.md), so the command runs through Arc's real pipeline rather than calling `Handle()` directly. The same scenario limits apply: this does not test HTTP routing, authentication middleware, or a production Chronicle server.

## Package and creation

Reference `Cratis.Arc.Chronicle.Testing` and import `Cratis.Arc.Chronicle.Testing.Slices`. The package targets .NET 10. “Framework-neutral” means the facade does not require a particular test framework; it does not mean Chronicle-free. For standalone Arc tests without Chronicle, use `Cratis.Arc.Testing` and `CommandScenario<TCommand>` directly.

Construct `new Slice<TCommand>(command)` with a command instance; `TCommand` must be a reference type. `Command` exposes that instance, and `Scenario` exposes the underlying `CommandScenario<TCommand>`.

## Seed, run, and assert

This test-body excerpt follows the ledger scenario in Arc's Slice specs. It assumes your test project already defines and discovers `SettleLedger`, `LedgerCredited`, `LedgerSettled`, `LedgerBalance`, and its reducer. The reducer sums credited amounts; the command takes the balance as a handler dependency and returns `LedgerSettled`. Its validator requires a positive balance.

```csharp
using Cratis.Arc.Chronicle.Testing.Slices;
using Cratis.Chronicle.Events;

var source = EventSourceId.New();
await using var slice = new Slice<SettleLedger>(new SettleLedger(source))
    .GivenEvents(new LedgerCredited(40m), new LedgerCredited(2m));

var outcome = await slice.Run();

outcome.ShouldBeSuccessful();
await slice.Then.ShouldHaveAppendedEventsInOrder(new LedgerSettled(42m));
await outcome.ShouldHaveAppendedEvent<LedgerSettled>(source,
    @event => @event.Balance == 42m);
```

The expected outcome is a successful command and one `LedgerSettled` event with balance `42m`. `Run()` returns `Task<SliceOutcome<TCommand>>`; after it completes successfully, `slice.Then` returns that same outcome. Assertion mismatches throw, so your test framework reports them as failures.

## Given methods

Configure state and services before calling `Run()`. Each method below returns the same `Slice<TCommand>` for fluent setup.

| Method | Behavior |
| --- | --- |
| `GivenEvents(params object[] events)` | Seeds prior events in order for the command's event source, resolved through `Command.GetEventSourceId()`. |
| `GivenEvents(EventSourceId eventSourceId, params object[] events)` | Seeds prior events for an explicit source. |
| `GivenReadModel<TReadModel>(TReadModel readModel)` | Pins a read model for the command's event source. `TReadModel` must be a reference type. |
| `GivenReadModel<TReadModel>(EventSourceId eventSourceId, TReadModel readModel)` | Pins a read model for an explicit source. |
| `GivenCaller(ClaimsPrincipal caller)` | Supplies the principal used by pipeline authorization checks through `ICurrentPrincipalAccessor`. This does not authenticate the caller. |

`GivenEvents` and `GivenReadModel` delegate to `Scenario.Given.ForEventSource(...)`. Event seeding uses the read model's projection or reducer; pinning supplies the instance directly. Pins remain subject to the scenario's decision-read rules: a pin cannot supply a protected decision token, and attempting that setup throws `PinnedReadModelCannotProvideDecisionToken`. Seed events instead for a protected decision read. See [Chronicle testing](chronicle.md) for seeding and decision-mode behavior.

For authorization tests, pass a `System.Security.Claims.ClaimsPrincipal` with the identity and role claims your command requires. The principal affects the real pipeline authorization checks, not only the handler.

## Outcome assertions

`SliceOutcome<TCommand>` forwards the following assertions under the same names as the underlying command-result and scenario assertions. These names, parameters, return types, and assertion behavior are a compatibility contract, not a separate assertion vocabulary.

| Method | Return type | Assertion |
| --- | --- | --- |
| `ShouldBeSuccessful()` | `void` | The command succeeded. |
| `ShouldBeRejected()` | `void` | The command was rejected; a dependency-only rejection does not satisfy this assertion. |
| `ShouldBeRejected(string reason)` | `void` | A validation message exactly matches `reason`. |
| `ShouldBeDenied()` | `void` | Authorization was denied. |
| `ShouldNotHaveAppendedEvents()` | `Task` | No command events were appended. |
| `ShouldNotHaveAppendedEvents(EventSourceId eventSourceId)` | `Task` | No command events were appended for this source. |
| `ShouldHaveAppendedEventsInOrder(params object[] events)` | `Task` | The complete appended event list matches the values and order. |
| `ShouldHaveAppendedEventsInAnyOrder(params object[] events)` | `Task` | The complete appended event list matches the values, including duplicates, regardless of order. |
| `ShouldHaveAppendedEvent<TEvent>(EventSourceId eventSourceId)` | `Task` | An event of this type was appended for this source. |
| `ShouldHaveAppendedEvent<TEvent>(EventSourceId eventSourceId, Func<TEvent, bool> predicate)` | `Task` | An event of this type for this source satisfies the predicate. |

Await the event assertions. A result mismatch throws `CommandResultAssertionException`; an event mismatch throws `EventSequenceAssertionException`. For capture boundaries, including seeded and competing events, follow the [Chronicle scenario assertion reference](chronicle.md).

## Escape hatches and lifecycle

Use `slice.Scenario` before execution for additional scenario configuration, such as application service registrations. After execution, `outcome.Result` exposes the `CommandResult` for assertions outside the forwarding contract, and `outcome.Scenario` exposes the underlying scenario for additional assertions. There are no read-model outcome assertion forwarders on `SliceOutcome<TCommand>`.

Keep execution through `slice.Run()` when using the facade. Calling `Scenario.Execute(...)` directly does not populate `slice.Then`.

- Accessing `Then` before `Run()` finishes successfully throws `SliceNotRun`.
- `Run()` permits only one execution request. A second call throws `SliceAlreadyRun`, including concurrent calls or a retry after the first execution throws. Create a new slice for another execution.
- Dispose the slice with `await using` or `await slice.DisposeAsync()` to release the underlying scenario and its resources. In a Specification, dispose it in `Destroy()`.

Screenplay recognizes a `Slice` test that expects its own command to be rejected without a named reason. Constructing the slice in a field initializer or `Establish()`, running it in `Because()`, asserting `ShouldBeRejected()` and disposing it in `Destroy()` is read as a rejection scenario. Any other setup, write or assertion on the slice makes Screenplay report the specification as unreadable instead of guessing.
