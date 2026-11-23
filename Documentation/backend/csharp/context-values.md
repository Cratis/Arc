---
title: Request-scoped context values in Cratis Arc
description: Read model-provided values like the vessel being viewed in Arc command handlers, query performers, policies and validation rules, resolved once per request.
---

A model can add values to `$context` with `provide`: the vessel being viewed, the project the user works in. Arc resolves each value lazily, once, for the request that reads it, and every reader in the request sees the same value: gates, command handlers, query performers, policies and validation rules.

The resolution is the [Choreography engine's](https://github.com/Cratis/Choreography). Arc supplies the per-request scope and the places you read from. Hosts that do not call `AddArcAvailability` register no source, and every value reads as absent without any work being done.

## Provide values

Declare the values in the `ContextValueCatalog` you register for [availability](availability.md). `AddArcAvailability` also registers the `IContextValueSource` that resolves them for each request:

```csharp
var catalog = new ContextValueCatalog()
    .Declare(new ContextValueDeclaration(
        "vessel",
        new ReadModelContextValueProvider(query, "VesselByKey", "arguments.vesselId"),
        ContextScope.Of("Fleet")));

services.AddSingleton(catalog);
services.AddArcAvailability(options =>
    options.ScopeFor = part => part.Namespace?.StartsWith("Fleet") == true ? ContextScope.Of("Fleet") : ContextScope.Application);
```

`ScopeFor` tells which level a command or query is read in. The nearest declaration wins: a feature-level `vessel` overrides the module's for the parts beneath it.

During a bound command or query invocation, providers can resolve `arguments.<property>` and `receivedAt`.
The arguments are the command instance or bound query dictionary; the receipt is the server's pinned
`DateTimeOffset`, not caller input. In a type-only availability/listing call, both values are absent. No
invocation is guessed from an active request, so metadata cannot widen execution or acquire its arguments.
Caller keys stay nested under `arguments` and do not override trusted tenant, identity, audience or context data.

## Read values

| Where | How |
| --- | --- |
| Query performer | `QueryContext.Values.Vessel`, `QueryContext.ProvidedValues`, `QueryContext.ValuesAs<T>()` |
| Policy | `AuthorizationPolicyContext.Values.Vessel`, `ProvidedValues`, `ValuesAs<T>()` |
| Validation rule | `context.Values().Vessel`, `context.ProvidedValues()`, `context.ValuesAs<T>()` on the FluentValidation context |
| Command | `CommandContext.ProvidedValues`, `CommandContext.ValuesAs<T>()` |

`CommandContext.Values` already holds the values of the command pipeline, so the provided values have another name there. Names are matched with the first letter in either case, so `Values.Vessel` reads the value declared as `vessel`.

```csharp
public record VesselContext(Vessel Vessel, Project? Project);

var vessel = context.ValuesAs<VesselContext>().Vessel;
```

`Values` reads synchronously and uses dynamic dispatch, so it is not available in a Native AOT application; `ProvidedValues` and `ValuesAs<T>()` are. A type for `ValuesAs<T>()` declares a nullable member for any value that can be absent. Where you are already asynchronous, use `ProvidedValues.GetAsync("vessel")`, or `ReadAsync("vessel")` to tell an absent value from a present one.

## Absent, required and failing values

- An **absent** value, with no key, no matching row or a value the host did not supply, reads as `null`. That is unknown, and a gate that reads it denies.
- A value declared **required** that is absent throws `RequiredContextValueAbsentException`, so the request is refused with a clear reason. Declare it with `Required: false` for code that accepts `null`.
- A value that **fails** to resolve, because a store is unavailable or a provider throws, throws `ContextValueUnavailableException`. The request fails as a retryable error, logged, and is never reported as a denial, so an outage is not shown as "not available". Command and query results report it with `IsRetryable`.

## Once per request

The first read runs the provider. Successful and absent values are reused by the gate and readers of that
invocation, including after dependency binding. A provider failure or cancellation clears the affected resolver
so a later resolution can retry. Metadata-only reads have a separate cache and never seed execution values.
Nested, parallel and later invocations resolve independently, even if their arguments and receipt times are equal.

A custom `IContextValueSource` registered as a scoped service replaces the Choreography one for hosts that
resolve values another way. Its existing `Read` method remains supported. It can optionally implement
`IExecutionContextValueSource.ReadForExecution` to receive the same `AvailabilityInvocation` identity as the gate.
