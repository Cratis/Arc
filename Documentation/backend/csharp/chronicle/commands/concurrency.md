---
title: Cratis Arc command concurrency
description: Cratis Arc uses command metadata to scope optimistic append checks. Carry the revision read by a decision when its rule depends on existing state.
---

Cratis Arc uses command metadata to choose which events Chronicle checks for a concurrent change before appending. A `ConcurrencyScope` names that history and its expected last position, called the tail. The append fails if the checked expectation no longer matches.

Use the attributes on this page to declare the check's boundaries on a model-bound command. Arc builds the scope for events returned by `Handle()`. If your command decides whether a withdrawal or approval is allowed by reading existing state, you also need a guard tied to that read; an atomic append alone does not provide it. See [Chronicle's concurrency control](/chronicle/events/concurrency/) for the underlying check.

:::caution[`concurrency: true` does not guard a decision]
The automatic check reads the tail after `Handle()` returns. An event arriving between your read and that capture can become part of the expected tail instead of causing a conflict. For a stateful rule, choose a constraint, a read model parameter under [read protection](../consistency/getting-started.md) `On`, a [stream decision](./stream-decisions.md), an aggregate, or an explicit scope. [Choosing how a command's rule holds](../consistency/index.md) helps you select one.
:::

A [Dynamic Consistency Boundary](/chronicle/dynamic-consistency-boundary/) rejects an append if an event matching the decision's query arrived after the position it read. Metadata chooses which events match; the captured position determines when a change counts as interference. An [event source definition](./event-source-definitions.md) is the typed alternative to repeating these metadata strings on commands. [Choosing a mechanism](/chronicle/dynamic-consistency-boundary/choosing/) compares the available guards.

A [guarded generated append target](fresh-identifiers.md) is an exception to automatic tail capture:
Arc uses an immutable whole-source `BeforeFirst` scope. Existing events produce `ConcurrencyViolation`
with `ReasonDetail = "GeneratedIdentifierCollision"`, regardless of routing metadata.

If you already have the revision used by the decision, return it with the events in [`EventsWithConcurrencyScopes`](./events.md#events-with-exact-concurrency-scopes) instead of relying on automatic tail capture.

## Concurrency Metadata Attributes

Choose the dimensions that should make two writes compete. Each attribute sets routing metadata on the events; `concurrency: true` also includes that dimension in the scope Arc declares. The target event source id is included in these attribute-built scopes.

### `[EventStreamId]`

Scopes concurrency to a specific event stream id within a stream type. Use this when independent streams within the same stream type should not interfere with each other.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
[EventStreamId("customer-profile", concurrency: true)]
public record UpdateCustomerProfile(EventSourceId CustomerId, string DisplayName)
{
    public CustomerDisplayNameChanged Handle() => new(DisplayName);
}

/// <summary>
/// Records the customer's updated display name.
/// </summary>
[EventType]
public record CustomerDisplayNameChanged(string DisplayName);
```

### `[EventStreamType]`

Scopes concurrency to a named stream type. Stream types group related streams — for example, separating `Onboarding` events from `Transactions` for the same customer.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
[EventStreamType("Transactions", concurrency: true)]
public record ProcessPayment(EventSourceId AccountId, decimal Amount)
{
    public PaymentProcessed Handle() => new(Amount);
}

/// <summary>
/// Records the amount of a processed account payment.
/// </summary>
[EventType]
public record PaymentProcessed(decimal Amount);
```

### `[EventSourceType]`

Scopes concurrency to a named event source type. This is the overarching concept the event source belongs to — for example `Customer` or `BankAccount`.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
[EventSourceType("Customer", concurrency: true)]
public record RegisterCustomer(EventSourceId CustomerId, string Email)
{
    public CustomerRegistered Handle() => new(Email);
}

/// <summary>
/// Records the email address supplied when a customer registered.
/// </summary>
[EventType]
public record CustomerRegistered(string Email);
```

## Combining Attributes

Combine attributes when writes should compete only within the same source type, stream type and stream id. In this example, two profile updates for the same customer share the check; an event on a different stream type does not. Only attributes with `concurrency: true` select dimensions for the declared scope. Attributes without the flag still affect the fallback check, as [the routing-only examples](#what-a-routing-only-tag-already-does) explain.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
[EventStreamId("customer-profile", concurrency: true)]
[EventStreamType("Profile", concurrency: true)]
[EventSourceType("Customer", concurrency: true)]
public record UpdateCustomerProfile(EventSourceId CustomerId, string DisplayName, string Email)
{
    public IEnumerable<object> Handle() =>
    [
        new CustomerDisplayNameChanged(DisplayName),
        new CustomerEmailChanged(Email)
    ];
}

/// <summary>
/// Records the customer's updated display name.
/// </summary>
[EventType]
public record CustomerDisplayNameChanged(string DisplayName);

/// <summary>
/// Records the customer's updated email address.
/// </summary>
[EventType]
public record CustomerEmailChanged(string Email);
```

With no `concurrency: true` flag, the command leaves scope construction to the event sequence's configured strategy. The default optimistic strategy still narrows its check by the routing metadata; the next section shows how.

## What a routing-only tag already does

Without a declared scope, the default optimistic strategy reads the expected tail using the append's routing metadata. For example, `[EventStreamType("Attachments")]` limits that fallback check to `Attachments` events for the target source. A write under another stream type is outside it even though no attribute says `concurrency: true`.

Adding the flag to every metadata attribute keeps the same narrowing dimensions, but changes the capture time. Arc captures a declared scope while processing the handler's response; Chronicle captures the fallback during commit-time `AppendMany()`. A write between those moments can conflict with the earlier expectation but become part of the later one.

Adding the flag to only some attributes can broaden the check. Suppose a command carries `[EventSourceType("X")]` and `[EventStreamType("Attachments", concurrency: true)]`. Arc includes the stream type but passes `null` for the source type because that attribute did not opt in. The declared check therefore covers more history than the fallback, which would have filtered by both values. Choose the flags together rather than treating each as an independent on/off switch.

:::note[Concurrency tags narrow the default check]
Use `concurrency: true` to choose the dimensions explicitly, not to turn concurrency checking on. For the default whole-event-source check, leave routing metadata unset. Use an exact scope if routing must stay narrow while the guard covers broader history.
:::

## Dynamic Event Stream Id

When the event stream id is determined at runtime rather than as a constant, implement `ICanProvideEventStreamId` and return the id from `GetEventStreamId()`.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
[EventStreamType("Reporting", concurrency: true)]
public record GenerateMonthlyReport(EventSourceId AccountId, string MonthKey)
    : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => MonthKey;

    public MonthlyReportGenerated Handle() => new(MonthKey);
}

/// <summary>
/// Records the month covered by a generated account report.
/// </summary>
[EventType]
public record MonthlyReportGenerated(string MonthKey);
```

In this example, only `EventStreamType` opts into the declared scope, so the check covers the account's `Reporting` events across months. To check each month independently, include the stream-id dimension as well.

If both a non-empty `[EventStreamId]` value and `ICanProvideEventStreamId` are present, Arc throws `AmbiguousEventStreamId`. Choose one approach, or set the attribute value to `null` to defer to the interface.

## Template Event Stream Ids

When a stream id combines command properties, use a template instead of implementing `ICanProvideEventStreamId`. Templates are resolved for **Arc commands only**. On a Chronicle reactor, the same `[EventStreamId]` value is used literally; it does not interpolate reactor properties.

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
[EventStreamType("Reporting")]
[EventStreamId("{ReportingScopeId}:{Period}")]
public record GenerateReport(EventSourceId AccountId, string ReportingScopeId, DateOnly Period)
{
    public ReportGenerated Handle() => new(Period);
}

[EventType]
public record ReportGenerated(DateOnly Period);
```

`{Name}` selects a readable public instance property. Each part is formatted as follows:

| Part type | Format |
| --- | --- |
| Concept | Its underlying value, using the rules below |
| `string` | Unchanged |
| Enum | Its name |
| `DateOnly` | `yyyy-MM-dd` |
| `DateTime`, `DateTimeOffset`, `TimeOnly` | Round-trip `O`, preserving seconds and fractional ticks |
| `TimeSpan` | Invariant `c` |
| Other `IFormattable` values, including numbers and `Guid` | Default format with invariant culture |
| Other values | `ToString()` |

`{{` and `}}` escape literal braces within a template. Arc refuses unknown properties, null or blank parts, malformed braces, and a resolved id equal to `Default` or empty.

Template resolution waits until the route is needed, normally after authorization and validation. A per-stream model injected into a validator needs the route earlier; Arc resolves it before constructing that validator and refuses a blank part before reading the model. Constant values and `ICanProvideEventStreamId` keep their existing behavior.

`EventStreamIdTemplate.Resolve("{ReportingScopeId}:{Period}", command)` resolves a template outside the pipeline. `EventStreamIdTemplate.ResolveFor(command)` resolves the command's attribute or provider interface exactly as the pipeline does, and returns null when neither declares an id. Use the same function for a read or seed route instead of repeating the join. An attribute with a value and `ICanProvideEventStreamId` still raises `AmbiguousEventStreamId`.

Owner-qualified templates are not needed to scope a [stream completion](./completing-streams.md): when the command has an event source id, Arc closes only that source's stream. Keep a composite id when those properties genuinely identify the stream.

## Event Source Id

The event source id used when appending is resolved from the command by convention — not from the concurrency scope. See [Event Source Id Resolution](./events.md#event-source-id-resolution) for the full resolution order, including `ICanProvideEventSourceId`.

## How the Scope Is Built

When `Handle()` returns events, Arc reads the three concurrency attributes and the resolved command metadata. It builds the declared scope from the dimensions marked `concurrency: true`, with an expected revision from the event sequence's strategy. Under read protection `On`, the transaction handles the guard instead; see [how read protection works](../consistency/how-it-works.md).

The expected revision must be checkable. `BeforeFirst` means no matching event may exist; `Unavailable` means no usable expectation was supplied and does not protect empty history. Automatic first-append checking is opt-in through the client's concurrency options.

Arc builds these scopes per target event source. A command writing to several targets cannot reuse one target's expected tail as the expectation for another.

## Carrying the revision used by the decision

With an opted-in concurrency attribute, automatic optimistic concurrency resolves the expected tail while Arc handles the returned value. With no opted-in attribute, Arc leaves the scope unset and Chronicle resolves the fallback expectation during commit-time `AppendMany()`. Neither capture is necessarily the revision `Handle()` read. A concurrent append that lands after that read but before the applicable capture can become part of the new expected tail rather than cause a conflict.

When that newer tail would invalidate the decision, capture the revision during the read and return it in `EventsWithConcurrencyScopes`. Arc passes it unchanged into the same command transaction as the ordered events. Interference after the read then produces a concurrency validation failure at commit, with no partial append.

For a decision over stream facts, [`IStreamReads`](./stream-decisions.md) captures the tail before reading the events and returns a `StreamDecision`. Its `Append(...)` routes the facts and builds the exact scope, including the empty-stream first-append check.

This is opt-in. Returning ordinary events or `EventForEventSourceId` values keeps the automatic strategy and its existing behavior. For an empty read, explicitly convert `Unavailable` to `BeforeFirst` as shown in the [exact-scope example](./events.md#events-with-exact-concurrency-scopes), and deploy a compatible server. A scope watching one event type cannot enforce a rule over changes outside that narrowing.

## Exact scopes replace automatic capture

When `Handle()` returns `EventsWithConcurrencyScopes`, its supplied scopes replace automatic capture.
For a guarded generated target, an equal whole-source `BeforeFirst` intent merges with the freshness guard;
a differing explicit intent fails with `ConflictingConcurrencyScopes`. Remove `concurrency: true` from `[EventSourceType]`, `[EventStreamType]` and `[EventStreamId]` on that command, but keep their routing values. A later automatically captured tail would defeat the purpose of guarding the revision used by the decision.

For `[EventSource<TSource>(stream)]`, keep the concurrency dimensions on the shared definition. The exact returned scope takes precedence for this command without changing the policy for other commands. Arc's exact-scope response handler reads `ConcurrencyScopes` as a dictionary with one scope per source id, so this return path cannot carry two independent stream-decision guards for the same source.
