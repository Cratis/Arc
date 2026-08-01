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

You can combine multiple concurrency attributes to build a precise scope. Only the attributes with `concurrency: true` contribute to the scope; others still tag the events but do not affect concurrency.

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

If no attribute has `concurrency: true`, the command contributes no scope of its own and the append is left to the concurrency strategy configured on the event sequence — by default the optimistic one, which resolves the expected tail for the event source being appended to.

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
