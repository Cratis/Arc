---
title: Observability
description: The tracing and metrics Arc emits, the activity source and meter to subscribe to, and the spans, attributes and metrics you will see.
---

Arc instruments its own pipelines. Every command and query that runs produces
activities on a named source and measurements on a named meter, so once you
subscribe to them your tracing backend shows where time went inside Arc, and why
an operation failed, without you adding anything to your handlers.

This is emitted telemetry rather than an extension point. Arc registers the
activity sources itself; what you do is subscribe to them.

## Subscribe to the source and the meter

Arc publishes its spans on an activity source named **`Cratis.Arc`** and its
metrics on a meter of the same name. Both names are available as constants on
`WellKnownDiagnostics`, so you do not have to repeat the string:

```csharp
using Cratis.Arc;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(WellKnownDiagnostics.ActivitySourceName)
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter(WellKnownDiagnostics.MeterName)
        .AddAspNetCoreInstrumentation());
```

Nothing else is required. The sources are registered when the host is built, so
a subscriber either sees the spans or does not — there is no Arc-side switch to
turn tracing on.

## The spans

All of these are `Internal` activities. Each span keeps one stable name, which
is what tracing backends show as the span name and what you filter and group on.
The command or query it covers is an attribute, `cratis.arc.command.type` or
`cratis.arc.query.name`, so a dashboard keyed on a span name keeps working as
your application adds commands and queries.

| Span | Raised when |
|---|---|
| `cratis.arc.command.execute` | a command runs through the pipeline |
| `cratis.arc.command.validate` | a command is validated without being run |
| `cratis.arc.command.filter` | the command filters run |
| `cratis.arc.command.authorize` | an authorization filter decides on the command |
| `cratis.arc.validator.invoke` | a validator runs, for a command, a query or a nested model |
| `cratis.arc.command.provide` | a model-bound command's `Provide()` method runs |
| `cratis.arc.command.handle` | the command handler runs |
| `cratis.arc.command.action` | a controller-based command action is invoked |
| `cratis.arc.query.perform` | a query runs through the pipeline |
| `cratis.arc.query.filter` | a query filter runs |
| `cratis.arc.query.action` | a controller-based query action is invoked |
| `cratis.arc.identity.resolve` | identity details are resolved for a request |

The child spans nest inside the command span, which itself nests inside the
request span your ASP.NET Core instrumentation already creates. For a
`RegisterAuthor` command, the `execute`, `authorize`, `provide` and `handle`
spans carry `cratis.arc.command.type = MyApp.Authors.RegisterAuthor`, and the
validator span carries the validator type:

```text
POST /api/authors/register
└── cratis.arc.command.execute
    ├── cratis.arc.command.filter
    │   ├── cratis.arc.command.authorize
    │   └── cratis.arc.validator.invoke
    ├── cratis.arc.command.provide
    └── cratis.arc.command.handle
```

A validator runs once for every instance of the type it validates, so a command
carrying a list of 500 items would otherwise add 500 spans. A validator gets a
span the first time it runs in a validation, not for each item after that, and
one validation adds at most 16 validator spans.

For a request over HTTP, the command or query span starts before Arc prepares
authorization, so a request turned away at that point still has a span and is
still counted in the metrics.

### Attributes

| Attribute | On | Value |
|---|---|---|
| `cratis.arc.command.type` | `execute`, `validate`, `authorize`, `provide`, `handle` | the full name of the command type |
| `cratis.arc.command.outcome` | `execute`, `validate` | `success`, `validation`, `authorization`, `append_rejected`, `cancelled` or `error` |
| `cratis.arc.command.key.type` | `execute`, `validate` | the type the command declares its key as, such as its event source id with the Chronicle integration |
| `cratis.arc.query.name` | `perform` | the fully qualified name of a known query; absent for a name that matches no query |
| `cratis.arc.query.transport` | `perform` | `snapshot`, `observable`, or `unknown` when the query failed before it ran |
| `cratis.arc.query.outcome` | `perform` | `success`, `validation`, `authorization`, `cancelled` or `error` |
| `cratis.arc.validator.type` | `cratis.arc.validator.invoke` | the full name of the validator type |
| `cratis.arc.validation.result_count` | `cratis.arc.validator.invoke` | how many results the validator produced |
| `cratis.correlation_id` | `execute`, `validate`, `perform` | the correlation id of the operation |
| `cratis.tenant` | `execute`, `validate`, `perform` | the tenant the operation runs for, when it is known |

The `execute`, `validate`, `filter` and `perform` spans also keep the
`command_type` and `query_name` attributes they carried in earlier versions.
`query_name` is `_other` for a name that matches no query. Prefer the
`cratis.arc.*` attributes for anything new.

Arc records the tenant once the pipeline has established who the operation runs
as, without resolving a tenant itself, since that would change what the
operation sees. The tenant is held per asynchronous flow, so one that a filter
or validator resolves inside its own call is not visible to the pipeline
afterwards. An operation turned away before that point, for example a command
with no handler, carries no tenant.

### Outcomes

The outcome attribute says how an operation ended. Following OpenTelemetry, only
an `error` outcome, an exception Arc did not expect, sets the span status to
`Error`. A command rejected by validation, by authorization or by the event store
is the application working as intended, so its span status stays unset; filter
on the outcome attribute to find those. `cancelled` means the operation's
cancellation token was cancelled, normally because the caller went away, and it
leaves the status unset too.

The child spans follow the same rule. An exception thrown from `Handle()`,
`Provide()` or a validator adds an `exception` event to that span, but sets its
status to `Error` only when it is an error: not when it is a validation failure
(an exception implementing `IValidationFailure`, which Arc reports as a
validation outcome) and not once the operation was cancelled.

Every outcome other than `success` adds events that say why:

| Event | Raised when | Attributes |
|---|---|---|
| `cratis.arc.validation.failed` | once for each validation result that blocked the operation, up to 16 | `cratis.arc.validation.severity`, `cratis.arc.validation.members`, `cratis.arc.validation.reason` and, when set, `cratis.arc.validation.reason_detail` |
| `cratis.arc.authorization.denied` | authorization denied the operation, on the operation span and the `authorize` span | — |
| `exception` | an exception was thrown, on the span it was thrown in | `exception.type` |

`append_rejected` is the outcome when the rejection came from the event store
rather than from a rule: a constraint violation or a concurrency conflict. The
reason says which, and for a constraint the reason detail is the constraint name.

### What the spans never carry

Spans carry types, names, identifiers, member names and outcomes. They never
carry a value from a command, query or event, so a property marked `[PII]` or
`[NotAudited]` cannot reach your tracing backend through them. That is why a
validation event names the member and the rule but not the message, which often
quotes the value, and why an exception event carries the exception type but not
its message or stack trace; those stay in your logs. The event source id is
recorded by its type, never by its value.

### Names as constants

Every span, attribute, event and metric name in this page is a constant on
`WellKnownTelemetryNames`, and every outcome is a constant on
`WellKnownOperationOutcomes`, so dashboards, alerts and tests built in C# can use
them instead of repeating the strings.

## What this is useful for

The split between `validate` and `execute` is the one worth watching. Validation
that reaches a database — a uniqueness rule, a state-dependent check — is easy to
write and easy to forget, and it shows up here as time spent before the handler
ever ran.

`cratis.arc.identity.resolve` is the other common surprise. The span covers each
identity lookup, such as the frontend's `/.cratis/me` request. When the identity
cookie already holds a result, the lookup returns it without calling your
identity-details provider; otherwise the provider runs, and one that queries a
store puts that query on the critical path of every uncached lookup.

## Metrics

Arc records these on the `Cratis.Arc` meter:

| Metric | Type | Unit | Attributes |
|---|---|---|---|
| `cratis.arc.command.duration` | histogram | `s` | `cratis.arc.command.type`, `cratis.arc.command.outcome` |
| `cratis.arc.command.outcomes` | counter | `{command}` | `cratis.arc.command.type`, `cratis.arc.command.outcome` |
| `cratis.arc.query.duration` | histogram | `s` | `cratis.arc.query.name`, `cratis.arc.query.transport`, `cratis.arc.query.outcome` |

Only commands that run are measured; validating a command without running it is
traced but not counted. For an observable query the duration covers setting up
the subscription, not how long it stays open.

Each metric records at most 1,000 distinct command types or query names. The
limit is fixed; there is no option to change it. Past that, further ones are
recorded as `_other`, so a metric backend never has to hold an unbounded number
of series. The limit holds exactly, however many requests arrive at once; only
right at the limit can a new name be recorded as `_other` once before it gets
the last free place. A query name that matches no known query is always
recorded as `_other`.

When nothing listens to the meter or the activity source, Arc skips the work of
recording: it builds no attributes and looks nothing up.

The MongoDB integration records its client metrics on the same meter.

## On the JVM

The Kotlin and Java backend observes the same pipeline stages but through
Micrometer rather than `ActivitySource`, with its own names and an explicit
correlation attribute. See
[Observability](/arc/backend/kotlin/guides/observability/) for that side. The two
are not wire-compatible with each other and are not meant to be — each uses what
its ecosystem already collects.
