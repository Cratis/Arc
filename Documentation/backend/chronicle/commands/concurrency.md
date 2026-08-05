---
title: Concurrency
description: Build a concurrency scope from command metadata so appends participate in optimistic concurrency checks.
---

Chronicle's [concurrency control](/chronicle/events/concurrency/) prevents conflicting operations from appending events to the same event source simultaneously. A `ConcurrencyScope` defines the boundaries for that check — which stream type, stream id, and event source type form the concurrency boundary.

On model-bound commands, you declare concurrency intent directly on the command record using attributes and interfaces. Chronicle then builds the `ConcurrencyScope` automatically when appending the events returned by `Handle()`. No manual scope construction is required.

## Concurrency Metadata Attributes

Three attributes control concurrency scope declaration on a command. Each attribute serves a dual purpose: it tags the appended events with metadata *and*, when `concurrency: true` is set, contributes that metadata to the concurrency scope.

### `[EventStreamId]`

Scopes concurrency to a specific event stream id within a stream type. Use this when independent streams within the same stream type should not interfere with each other.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
[EventStreamId("customer-profile", concurrency: true)]
public record UpdateCustomerProfile(EventSourceId CustomerId, string DisplayName)
{
    public CustomerDisplayNameChanged Handle() => new(CustomerId, DisplayName);
}

[EventType]
public record CustomerDisplayNameChanged(EventSourceId CustomerId, string DisplayName);
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
    public PaymentProcessed Handle() => new(AccountId, Amount);
}

[EventType]
public record PaymentProcessed(EventSourceId AccountId, decimal Amount);
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
    public CustomerRegistered Handle() => new(CustomerId, Email);
}

[EventType]
public record CustomerRegistered(EventSourceId CustomerId, string Email);
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
        new CustomerDisplayNameChanged(CustomerId, DisplayName),
        new CustomerEmailChanged(CustomerId, Email)
    ];
}

[EventType]
public record CustomerDisplayNameChanged(EventSourceId CustomerId, string DisplayName);

[EventType]
public record CustomerEmailChanged(EventSourceId CustomerId, string Email);
```

If no attribute has `concurrency: true`, the command contributes no scope of its own and the append is left to the concurrency strategy configured on the event sequence — by default the optimistic one, which resolves the expected tail for the event source being appended to, **narrowed by whatever routing metadata the command carries**.

## What a routing-only tag already does

A metadata attribute declared *without* `concurrency: true` still narrows the concurrency check. Its value reaches the append regardless of the flag, and the fallback strategy resolves the expected tail with the same narrowing — so the flag governs whether the command **declares** a scope, while the tag governs what the check is **narrowed by**. Both are true at once.

Three consequences follow, and the third is the surprising one:

- **A routing-only tag silently narrows every concurrency check on that command.** `[EventStreamType("Attachments")]` with no flag restricts the expected tail to `Attachments` events, so a concurrent append to the same event source under a different stream type is invisible to the check.
- **Declaring `concurrency: true` on *every* metadata attribute a command carries is behaviorally identical to declaring it on none.** The declared scope passes the context values; the fallback passes the same values, with a sentinel standing in for anything absent — and a sentinel adds no filter. Same filter set, same expected tail.
- **Declaring it on a *subset* produces a strictly broader scope than declaring it on none.** The declared scope passes `null` for every dimension that did not opt in, while the fallback would have passed its real value. Declaring it on `[EventStreamType]` alone, on a command that also carries `[EventSourceType("X")]`, **drops** the `EventSourceType == "X"` filter and widens the check.

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

    public MonthlyReportGenerated Handle() => new(AccountId, MonthKey);
}

[EventType]
public record MonthlyReportGenerated(EventSourceId AccountId, string MonthKey);
```

> **Note**: If both a non-empty `[EventStreamId]` value and `ICanProvideEventStreamId` are present on the same command, Chronicle throws an `AmbiguousEventStreamId` exception. Choose one approach, or set the attribute value to `null` to defer to the interface.

## Event Source Id

The event source id used when appending is resolved from the command by convention — not from the concurrency scope. See [Event Source Id Resolution](./events.md#event-source-id-resolution) for the full resolution order, including `ICanProvideEventSourceId`.

## How the Scope Is Built

When `Handle()` returns events, Chronicle inspects the command type for the three concurrency attributes. It reads the resolved metadata values from the command context and builds a `ConcurrencyScope` covering only the metadata where `concurrency: true` was set.

Two properties of that scope decide whether the check actually happens, and both are resolved per append rather than once per command:

- **It carries an expected sequence number**, resolved by the same concurrency strategy an unscoped append would use. A scope without one is skipped by the kernel — there is nothing to compare against — so the append would proceed unchecked.
- **It is bound to the event source being appended to.** A command that appends across streams gets a scope per target, because an expected tail belongs to exactly one stream; applying one stream's tail to another would be wrong for both.
