---
title: Request-scoped context values
description: Declare values at application, module, feature and slice level, resolve them lazily once per request, and handle required values, cycles and failures.
---

Gates read values: the vessel being viewed, the project the user works in. A `ContextValueDeclaration` adds such a value
under `$context` at one level of the model. `ContextValueCatalog` holds the declarations and
`ContextValueResolver` resolves them for one request.

```csharp
var catalog = new ContextValueCatalog()
    .Declare(new ContextValueDeclaration(
        "vessel",
        new ReadModelContextValueProvider(query, "VesselByKey", "arguments.vesselId"),
        ContextScope.Of("Fleet")));

var request = catalog.CreateResolver(
    ContextScope.Of("Fleet", "DataQuality", "QualityScores"),
    ambient: new Dictionary<string, object?> { ["arguments"] = arguments },
    audiences: async ct => await audienceResolver.Resolve(subject, ct));

var package = await request.Resolve("$context.vessel.commercialPackage");
```

## Levels and overrides

A `ContextScope` is the path from the module down: `ContextScope.Application` for the application level, then module,
feature or features, and slice. A declaration applies to its scope and everything below it. The nearest declaration wins,
and `catalog.Find(name, scope)` tells which one that is. Declaring the same name twice at one level is an error.

## Resolve once per request

A provider runs when its value is first read. The result, including a failure, is reused for the rest of that request, so
every gate in the request sees one consistent value. Nothing is cached across requests.

`ambient` holds the values the runtime supplies, such as `tenant`, `identity` and `arguments`. A path walks into
dictionaries and public properties: `vessel.commercialPackage`.

## Built-in names

`tenant`, `identity`, `causedBy`, `causation`, `occurred`, `audiences`, `command` and `arguments` are built in.
Declaring one throws `ReservedContextValueNameException`, so no level can change who a request is.
`$context.audiences` reads the request's `EffectiveAudiences`, resolved once through the function you pass.

## Providers

| Provider | Source |
| --- | --- |
| `ReadModelContextValueProvider` | A read model query through `IReadModelQuery`, keyed by another context path. |
| `LiteralContextValueProvider` | A literal. |
| `ExpressionContextValueProvider` | Another context value by path. |
| `HostContextValueProvider` | A value the host supplies through `IHostContextValues`. |
| `CodeContextValueProvider` | A delegate you write. List the values it reads so cycles are found early. |

`InMemoryReadModelQuery` and `InMemoryHostContextValues` are in-memory implementations for specifications.
Implement `IContextValueProvider` for another source.

## Required, optional and absent

A declaration is required by default. Absence (no key, no matching row, a value the host did not supply) is a normal
outcome:

- an **optional** value (`Required: false`) that is absent reads as absent, and a gate that reads it is `Unknown`;
- a **required** value that is absent throws `RequiredContextValueAbsentException`, so the host can refuse the request with
  a clear reason.

## Failure

A provider that throws is wrapped in `ContextValueProviderFailedException`, with the cause as the inner exception. It is
an error to log and retry, never a denial.

## Cycles

Values that read each other throw `ContextValueCycleException`. Call `catalog.Validate()` at startup to find cycles
before any request runs; the resolver also detects them while resolving.
