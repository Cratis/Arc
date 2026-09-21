---
title: Observability
description: The distributed tracing Arc emits, the activity source to subscribe to, and the spans you will see.
---

Arc instruments its own pipelines. Every command and query that runs produces
activities on a named source, so once you subscribe to it your tracing backend
shows where time went inside Arc without you adding anything to your handlers.

This is emitted telemetry rather than an extension point. Arc registers the
activity sources itself; what you do is subscribe to them.

## Subscribe to the source

Arc publishes everything under a single activity source named **`Cratis.Arc`**.
Add it wherever you configure OpenTelemetry:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Cratis.Arc")
        .AddAspNetCoreInstrumentation());
```

Nothing else is required. The sources are registered when the host is built, so
a subscriber either sees the spans or does not — there is no Arc-side switch to
turn tracing on.

## The spans

All of these are `Internal` activities.

| Span | Raised when | Identifies |
|---|---|---|
| `cratis.arc.command.execute` | a command runs through the pipeline | the command type |
| `cratis.arc.command.validate` | that command's validation stage runs | the command type |
| `cratis.arc.command.filter` | a command filter runs | the command type |
| `cratis.arc.command.action` | a controller-based command action is invoked | the route template |
| `cratis.arc.query.perform` | a query runs through the pipeline | the query name |
| `cratis.arc.query.filter` | a query filter runs | the query name |
| `cratis.arc.query.action` | a controller-based query action is invoked | the route template |
| `cratis.arc.identity.resolve` | identity details are resolved for a request | — |

`execute` and `validate` nest inside the request span your ASP.NET Core
instrumentation already creates, so a slow command shows up as a slow child of
the HTTP span rather than as an unattributed gap.

## What this is useful for

The split between `validate` and `execute` is the one worth watching. Validation
that reaches a database — a uniqueness rule, a state-dependent check — is easy to
write and easy to forget, and it shows up here as time spent before the handler
ever ran.

`cratis.arc.identity.resolve` is the other common surprise: an identity-details
provider runs per request, and one that queries a store puts that query on the
critical path of everything.

## Metrics

The MongoDB integration records client metrics. Arc's command and query pipelines
emit tracing rather than counters, so if you want request rates or error
rates by command, derive them from the spans or record them in a
[command filter](commands/command-filters.md).

## On the JVM

The Kotlin and Java backend observes the same pipeline stages but through
Micrometer rather than `ActivitySource`, with its own names and an explicit
correlation attribute. See
[Observability](/arc/backend/kotlin/guides/observability/) for that side. The two
are not wire-compatible with each other and are not meant to be — each uses what
its ecosystem already collects.
