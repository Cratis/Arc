---
uid: Arc.Testing.QueryScenario
title: Query scenarios
description: Execute model-bound snapshot queries through Arc's query pipeline without an HTTP server.
---

Use `QueryScenario<TReadModel>` when a static read-model query needs Arc's argument conversion, validation, authorization, dependency resolution, or result rendering in a test. A direct method call does not exercise those boundaries. This is a reference for the in-process scenario; use a hosted test for routing, middleware, scheme authentication, or subscriptions.

## Package and API

Install `Cratis.Arc.Testing` and import `Cratis.Arc.Testing.Queries`. The scenario is independent of Chronicle.

| Member | Behavior |
| --- | --- |
| `Services` | Configure application services, policies, and `ICurrentPrincipalAccessor` before the first `Perform` call. |
| `Context` | Values populated by discovered `IQueryScenarioExtender` implementations at construction. |
| `Perform(methodName, arguments?, paging?, sorting?, cancellationToken?)` | Executes the named static method on `TReadModel` through the hosted query pipeline; returns `Task<QueryResult>`. Use `nameof(TReadModel.Method)` for the method name, not a fully qualified name. Defaults are empty arguments, no paging, no sorting, and no cancellation. |
| `Dispose()` / `DisposeAsync()` | Releases the scenario provider and disposable extender context values; repeated calls are safe. |

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

The in-repository [query scenario specs](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Testing.Specs/Queries/for_QueryScenario/) exercise a parameterless query, concept argument conversion, rejection, and disposal with real read-model methods.

## Authorization and limits

Register an `ICurrentPrincipalAccessor` in `Services` for the principal under test, as you do for [command authorization](./command-scenario.md#example-authorization-spec). There is **no principal parameter** on `Perform`. Use an `ICurrentPrincipalAccessor` or a principal-selecting policy runtime, not both; combining them can cause the runtime-selected identity and the accessor's identity to disagree. Register host-neutral named policies with `Services.AddArcAuthorizationPolicy<TPolicy>(name)`; the hosted query path evaluates those policies before constructing protected dependencies. The result reports a denial through `IsAuthorized == false`. Missing required query arguments appear in `ValidationResults` and make `IsValid` false. Unknown policy names fail at run time. Tenant selection is not configurable in a query scenario; it uses default tenant resolution.

Authentication-scheme-restricted queries cannot be exercised in-process: selecting a scheme requires a live ASP.NET Core HTTP context. They fail closed with an unauthorized result. Use an HTTP-hosted integration spec to check schemes, request authentication, and ASP.NET Core-host policy behavior. See [ADR 0004](https://github.com/Cratis/Arc/blob/main/decisions/0004-evaluate-authorization-policies-asynchronously.md).

Only snapshot results are supported. Methods returning `ISubject<T>` or `IAsyncEnumerable<T>` throw `StreamingQueryNotSupported` before invocation; the scenario does not subscribe, wait for a first value, or retain an emission context. If a method declared as a snapshot unexpectedly returns a stream, the scenario disposes it before rejecting it. Test observable queries through the hosted streaming transport instead.

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

`Given.ForEventSource(id).ReadModel(otherReadModel)` can pin another read model type used by the query. `Given.ForEventSource(id).Events(events)` also seeds history for projection on demand. The read-model query must resolve `IReadModels` from its method dependencies and read the seeded event source id; seeding does not replace arbitrary application stores or make HTTP endpoints available. See the [Chronicle query spec](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Specs/Queries/for_QueryScenario/when_a_chronicle_read_model_is_seeded.cs) for a complete tested example.
