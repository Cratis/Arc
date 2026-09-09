---
title: Concurrency
description: Build a concurrency scope from command metadata so appends participate in optimistic concurrency checks.
---

Chronicle's [concurrency control](/chronicle/events/concurrency/) rejects an append when its checked expectation no longer matches the protected history. Atomic append alone does not bind a decision to the state it read. A `ConcurrencyScope` defines the boundaries for that check — which stream type, stream id, and event source type form the concurrency boundary.

On model-bound commands, you declare concurrency intent directly on the command record using attributes and interfaces. Chronicle then builds the `ConcurrencyScope` automatically when appending the events returned by `Handle()`. No manual scope construction is required.

That automatic path is the default. If the decision must remain bound to a revision it already read, return [`EventsWithConcurrencyScopes`](./events.md#events-with-exact-concurrency-scopes) and supply that exact revision with the returned events.

## Concurrency Metadata Attributes

Three attributes control concurrency scope declaration on a command. Each attribute serves a dual purpose: it tags the appended events with metadata _and_, when `concurrency: true` is set, contributes that metadata to the concurrency scope.

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

You can combine multiple concurrency attributes to build a precise scope. Only the attributes with `concurrency: true` contribute to the scope the command declares — but the others still tag the appended events, and the fallback strategy narrows by whatever tags an append carries, so they are not concurrency-inert either. See [what a routing-only tag already does](#what-a-routing-only-tag-already-does) below.

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

If no attribute has `concurrency: true`, the command contributes no scope of its own and the append is left to the concurrency strategy configured on the event sequence — by default the optimistic one, which resolves the expected tail for the event source being appended to, **narrowed by whatever routing metadata the command carries**.

## What a routing-only tag already does

A metadata attribute declared _without_ `concurrency: true` still narrows the concurrency check. Its value reaches the append regardless of the flag, and the fallback strategy resolves the expected tail with the same narrowing — so the flag governs whether the command **declares** a scope, while the tag governs what the check is **narrowed by**. Both are true at once.

Three consequences follow, and the third is the surprising one:

- **A routing-only tag silently narrows every concurrency check on that command.** `[EventStreamType("Attachments")]` with no flag restricts the expected tail to `Attachments` events, so a concurrent append to the same event source under a different stream type is invisible to the check.
- **Declaring `concurrency: true` on _every_ metadata attribute uses the same narrowing dimensions as declaring it on none, under the default optimistic strategy.** The declared scope passes the context values; the fallback passes the same values, with a sentinel standing in for anything absent — and a sentinel adds no filter. The capture timing differs: Arc resolves an explicit scope while processing the response; Chronicle resolves a missing scope during the unit of work's commit-time `AppendMany()`. An intervening append can conflict with the earlier expectation but be included in the later fallback tail.
- **Declaring it on a _subset_ produces a strictly broader scope than declaring it on none.** The declared scope passes `null` for every dimension that did not opt in, while the fallback would have passed its real value. Declaring it on `[EventStreamType]` alone, on a command that also carries `[EventSourceType("X")]`, **drops** the `EventSourceType == "X"` filter and widens the check.

:::note
The practical reading: reach for `concurrency: true` to state intent and to pin which dimensions bound the check, not because its absence leaves the check unbounded. If you want a check bounded by the whole event source, do not tag the command at all.
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

> **Note**: If both a non-empty `[EventStreamId]` value and `ICanProvideEventStreamId` are present on the same command, Chronicle throws an `AmbiguousEventStreamId` exception. Choose one approach, or set the attribute value to `null` to defer to the interface.

## Event Source Id

The event source id used when appending is resolved from the command by convention — not from the concurrency scope. See [Event Source Id Resolution](./events.md#event-source-id-resolution) for the full resolution order, including `ICanProvideEventSourceId`.

## How the Scope Is Built

When `Handle()` returns events, Chronicle inspects the command type for the three concurrency attributes. It reads the resolved metadata values from the command context and builds a `ConcurrencyScope` covering only the metadata where `concurrency: true` was set.

Two properties of that scope decide whether the check actually happens, and both are resolved per append rather than once per command:

- **It carries a checked expectation**, resolved by the event sequence's strategy. An actual revision is checked and `BeforeFirst` protects empty matching history. `Unavailable` is not an empty-history expectation; incomplete expectations are skipped/reported rather than enforcing the invariant. Automatic first-append checking is opt-in through the client's concurrency options.
- **It is bound to the event source being appended to.** A command that appends across streams gets a scope per target, because an expected tail belongs to exactly one stream; applying one stream's tail to another would be wrong for both.

## Carrying the revision used by the decision

With an opted-in concurrency attribute, automatic optimistic concurrency resolves the expected tail while Arc handles the returned value. With no opted-in attribute, Arc leaves the scope unset and Chronicle resolves the fallback expectation during commit-time `AppendMany()`. Neither capture is necessarily the revision `Handle()` read. A concurrent append that lands after that read but before the applicable capture can become part of the new expected tail rather than cause a conflict.

When that newer tail would invalidate the decision, capture the revision during the read and return it in `EventsWithConcurrencyScopes`. Arc passes it unchanged into the same command transaction as the ordered events. Interference after the read then produces a concurrency validation failure at commit, with no partial append.

This is opt-in. Returning ordinary events or `EventForEventSourceId` values keeps the automatic strategy and its existing behavior. For an empty read, explicitly convert `Unavailable` to `BeforeFirst` as shown in the [exact-scope example](./events.md#events-with-exact-concurrency-scopes), and deploy a compatible server. A scope watching one event type cannot enforce a rule over changes outside that narrowing.
