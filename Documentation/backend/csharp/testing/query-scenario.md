---
uid: Arc.Testing.QueryScenario
title: Query scenarios
description: Execute snapshot queries and observe buffered query emissions through Arc's pipeline without an HTTP server.
---

Use `QueryScenario<TReadModel>` when a static read-model query needs Arc's argument conversion, validation, authorization, dependency resolution, or result rendering in a test. A direct method call does not exercise those boundaries. This is a reference for the in-process scenario; use a hosted test for routing, middleware, scheme authentication, or transport behavior.

## Package and API

Install `Cratis.Arc.Testing` and import `Cratis.Arc.Testing.Queries`. The scenario is independent of Chronicle.

| Member | Behavior |
| --- | --- |
| `Services` | Configure application services, policies, and `ICurrentPrincipalAccessor` before the first `Perform` or `Observe` call. |
| `Context` | Values populated by discovered `IQueryScenarioExtender` implementations at construction. |
| `Perform(methodName, arguments?, paging?, sorting?, cancellationToken?)` | Executes the named static method on `TReadModel` through the hosted query pipeline; returns `Task<QueryResult>`. Use `nameof(TReadModel.Method)` for the method name, not a fully qualified name. Defaults are empty arguments, no paging, no sorting, and no cancellation. |
| `Observe(methodName, arguments?, paging?, sorting?, timeout?, cancellationToken?)` | Returns `Task<QueryObservation>` for a subject or async enumerable. The first-emission timeout defaults to five seconds. Pipeline rejections appear in `Initial`. |
| `QueryObservation.Initial` | The first `QueryResult`, including authorization or validation rejection. |
| `QueryObservation.Next(timeout?, cancellationToken?)` | Returns the next buffered `QueryResult`; defaults to a five-second deadline and throws `TimeoutException` when no emission arrives. Cancellation affects this read, not the subscription. |
| `QueryObservation.Dispose()` / `DisposeAsync()` | `Dispose` starts disposal without waiting; `DisposeAsync` waits for cleanup of the subscription and any identity-bound scope, not the subject. |
| `Dispose()` / `DisposeAsync()` | Ends observations before releasing the scenario provider and disposable extender context values; repeated calls are safe. |

The scenario discovers query methods on **only** `TReadModel` and builds their fully qualified names. Like [command scenarios](./command-scenario.md), it builds its provider lazily. Register dependencies before the first call; later changes to `Services` do not affect that provider. Extenders need a public parameterless constructor. The scenario disposes any identity-bound scope returned by hosted authorization before `Perform` returns; a snapshot's data remains in the result. Lazy snapshot sequences are materialized before the identity-bound scope is released.

This fragment assumes `OrderSummary` is a `[ReadModel]` with `public static OrderSummary ById(string id)` and that its dependencies, if any, have been registered:

```csharp
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;

using var scenario = new QueryScenario<OrderSummary>();
var result = await scenario.Perform(nameof(OrderSummary.ById), new QueryArguments { ["id"] = "order-123" });
Assert.True(result.IsSuccess);
var order = (OrderSummary)result.Data;
```

The in-repository query scenario specs in `Source/DotNET/Testing.Specs/Queries/for_QueryScenario/` exercise a parameterless query, concept argument conversion, rejection, and disposal with real read-model methods.

## Authorization and limits

Register an `ICurrentPrincipalAccessor` in `Services` for the principal under test, as you do for [command authorization](./command-scenario.md#example-authorization-spec). There is **no principal parameter** on `Perform`. Use an `ICurrentPrincipalAccessor` or a principal-selecting policy runtime, not both; combining them can cause the runtime-selected identity and the accessor's identity to disagree. Register host-neutral named policies with `Services.AddArcAuthorizationPolicy<TPolicy>(name)`; the hosted query path evaluates those policies before constructing protected dependencies. The result reports a denial through `IsAuthorized == false`. Missing required query arguments appear in `ValidationResults` and make `IsValid` false. Unknown policy names fail at run time. Tenant selection is not configurable in a query scenario; it uses default tenant resolution.

Authentication-scheme-restricted queries cannot be exercised in-process: selecting a scheme requires a live ASP.NET Core HTTP context. They fail closed with an unauthorized result. Use an HTTP-hosted integration spec to check schemes, request authentication, and ASP.NET Core-host policy behavior. See [ADR 0004](https://github.com/Cratis/Arc/blob/main/decisions/0004-evaluate-authorization-policies-asynchronously.md).

## Observable queries

Use `Perform` to assert the first emission of an `ISubject<TReadModel>` or `ISubject<IEnumerable<TReadModel>>` query. It waits up to 30 seconds, like HTTP with `waitForFirstResult=true`, then releases the subscription. A timeout or completion before the first value becomes an error result. `Perform` still rejects `IAsyncEnumerable<TReadModel>` before invocation with `StreamingQueryNotSupported`; use `Observe` for that shape.

Use `Observe` when the spec changes state and needs to assert a later emission. This fragment assumes a `[ReadModel]` named `OrderSummary` with an `[AllowAnonymous]` static query `Current(BehaviorSubject<OrderSummary> subject)` returning `ISubject<OrderSummary>`, and an `OrderSummary(string Status)` constructor:

```csharp
using System.Reactive.Subjects;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

using var subject = new BehaviorSubject<OrderSummary>(new("Pending"));
await using var scenario = new QueryScenario<OrderSummary>();
scenario.Services.AddSingleton(subject);
await using var observation = await scenario.Observe(nameof(OrderSummary.Current));
Assert.Equal("Pending", ((OrderSummary)observation.Initial.Data).Status);

subject.OnNext(new("Shipped"));
var changed = await observation.Next();
Assert.Equal("Shipped", ((OrderSummary)changed.Data).Status);
```

The subscription buffers values immediately, so the synchronous change above is retained even though `Next` is called afterward. `Next` returns the **next emission, not the next distinct value**: replayed or duplicate values still occupy a position in the buffer. Collection emissions are materialized as `List<TReadModel>`; paging totals are captured when each value is emitted.

Each emission runs read-model interceptors and emission guards in order when you read it. Suppression skips a value; denial returns an unauthorized result and ends the subscription. Stream errors and completion return error results rather than successful empty data. `Observe` and `Next` throw `TimeoutException` on their deadlines; a `Next` timeout leaves the subscription live. An observation's `Dispose` starts disposal without waiting for cleanup. Use `DisposeAsync` or dispose the scenario to wait for cleanup; the scenario ends observations before releasing its provider.

The scenario does not restore ambient tenant or identity on a producer thread. Guards receive the admitted principal explicitly. MongoDB `.Observe()` still needs a real MongoDB provider and change-stream support; these scenarios do not emulate it. Chronicle-backed state currently supports seeding snapshots, not pushing new emissions after an append.

## Chronicle-backed state

With `Cratis.Arc.Chronicle.Testing` referenced, the discovered Chronicle extender adds in-memory read-model services. Seed by event source before `Perform`:

```csharp
using Cratis.Arc.Chronicle.Testing.Queries;

using var scenario = new QueryScenario<QueryAccountBalance>();
var sourceId = EventSourceId.New();
scenario.Given.ForEventSource(sourceId).ReadModel(new QueryAccountBalance(42m));
var result = await scenario.Perform(nameof(QueryAccountBalance.ById), new QueryArguments { ["id"] = sourceId.Value });
Assert.True(result.IsSuccess);
```

`Given.ForEventSource(id).ReadModel(otherReadModel)` can pin another read model type used by the query. `Given.ForEventSource(id).Events(events)` also seeds history for projection on demand. The read-model query must resolve `IReadModels` from its method dependencies and read the seeded event source id; seeding does not replace arbitrary application stores or make HTTP endpoints available. See the Chronicle query spec `Source/DotNET/Chronicle.Specs/Queries/for_QueryScenario/when_a_chronicle_read_model_is_seeded.cs` for a complete tested example.
