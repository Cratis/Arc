---
title: Request-scoped context values in Cratis Arc
description: Read the values your model provides, such as the vessel being viewed, from command handlers, query performers, policies and validation rules, resolved lazily and once per request.
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

`Values` reads synchronously. Where you are already asynchronous, use `ProvidedValues.GetAsync("vessel")`, or `ReadAsync("vessel")` to tell an absent value from a present one.

## Absent, required and failing values

- An **absent** value, with no key, no matching row or a value the host did not supply, reads as `null`. That is unknown, and a gate that reads it denies.
- A value declared **required** that is absent throws `RequiredContextValueAbsentException`, so the request is refused with a clear reason. Declare it with `Required: false` for code that accepts `null`.
- A value that **fails** to resolve, because a store is unavailable or a provider throws, throws `ContextValueUnavailableException`. The request fails as a retryable error, logged, and is never reported as a denial, so an outage is not shown as "not available". Command and query results report it with `IsRetryable`.

## Once per request

The first read runs the provider. The result, including a failure, is reused for the rest of the request. Nothing is cached across requests: a second request resolves again. A custom `IContextValueSource` registered as a scoped service replaces the Choreography one for hosts that resolve values another way.
