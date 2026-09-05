---
title: Events
description: What a command Handle() can return — a single event, several, a tuple, a Result, or nothing — and what Chronicle does with each.
---

When a [model-bound](../../commands/model-bound/index.md) command handler returns an event (or a collection of events), Chronicle appends those events to the event log automatically. This lets you keep command handlers focused on decisions and domain rules instead of event log plumbing.

The appends are part of the command's transaction: all events commit atomically when the command succeeds, and none are appended when it fails — see [Transactional Commands](../../commands/transactional-commands.md).

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record RegisterCustomer(EventSourceId CustomerId, string Email)
{
    public CustomerRegistered Handle()
    {
        return new CustomerRegistered(CustomerId, Email);
    }
}

[EventType]
public record CustomerRegistered(EventSourceId CustomerId, string Email);
```

You can also return multiple events as a collection:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record UpdateCustomerProfile(EventSourceId CustomerId, string DisplayName, string Email)
{
    public IEnumerable<object> Handle()
    {
        return new object[]
        {
            new CustomerDisplayNameChanged(CustomerId, DisplayName),
            new CustomerEmailChanged(CustomerId, Email)
        };
    }
}

[EventType]
public record CustomerDisplayNameChanged(EventSourceId CustomerId, string DisplayName);

[EventType]
public record CustomerEmailChanged(EventSourceId CustomerId, string Email);
```

Chronicle uses the command context to resolve the event source identity and event stream metadata before appending events.

## Event Source Id Resolution

Chronicle resolves the event source id for commands using a small set of conventions. This value is stored in the command context and is required for event appending.

Chronicle resolves the event source id in this order:

1. Implement `ICanProvideEventSourceId` on the command and return the id from `GetEventSourceId()`.
2. Add a property of type `EventSourceId` to the command.
3. Mark a property with `[Key]` and let Chronicle use its value as the event source id.

If none of these are present, Chronicle creates a new `EventSourceId` so the command still has a valid identity for event appends.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

[Command]
public record OpenAccount(Guid AccountId, string OwnerName) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => AccountId.ToString();
}

[Command]
public record RenameAccount(EventSourceId AccountId, string NewName);

[Command]
public record CloseAccount([Key] Guid AccountId);
```

For the full reference, including how Chronicle uses the same identity conventions for query arguments and how you can override the command value by returning `EventSourceId` from `Handle()`, see [Resolving EventSourceId](../resolving-event-source-id.md) and [Returning EventSourceId](./returning-event-source-id.md).

## Event Stream Metadata

Chronicle supports additional metadata that can be attached to commands and used when appending events. This metadata tags the appended events with the specified stream identity, making them easier to query and react to.

### EventStreamId

Use `[EventStreamId]` to assign a specific event stream id to a command, or implement `ICanProvideEventStreamId` to supply it dynamically.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
[EventStreamId("customer-profile")]
public record UpdateCustomerProfile(EventSourceId CustomerId, string DisplayName, string Email);

[Command]
public record UpdateCustomerPreferences(EventSourceId CustomerId, string PreferenceKey, string PreferenceValue)
    : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => "customer-preferences";
}
```

If both a non-empty `[EventStreamId]` value and `ICanProvideEventStreamId` are used, Chronicle treats this as ambiguous and throws an `AmbiguousEventStreamId` exception. Choose one approach, or set the attribute value to `null` to defer to the interface.

### EventStreamType

Use `[EventStreamType]` to categorize events under a named stream type. This is useful for grouping related streams, such as separating onboarding events from transaction events for the same event source.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
[EventStreamType("Onboarding")]
public record RegisterCustomer(EventSourceId CustomerId, string Email);
```

### EventSourceType

Use `[EventSourceType]` to tag events with a specific event source type when they are appended.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
[EventSourceType("Customer")]
public record RegisterCustomer(EventSourceId CustomerId, string Email);
```

These metadata attributes categorize and identify the appended events. Because the append carries them, the concurrency strategy configured on the event sequence resolves its expected tail with the same narrowing — so a routing-only tag already bounds the concurrency check, without any attribute opting in. Setting `concurrency: true` chooses which dimensions bound it explicitly; see [concurrency scoping](./concurrency.md).

## Events for Specific Event Sources

Sometimes a single command needs to append events to multiple different event sources. The standard approach appends all events to the same event source resolved from the command context, which is fine for the common case. When you need finer control — for example, a fund transfer that debits one account and credits another — use `EventForEventSourceId`.

`EventForEventSourceId` is a record that pairs an event with an explicit `EventSourceId`. Chronicle appends each event to its specified event source, independently of the event source id in the command context. Because the command is a [transactional scope](../../commands/transactional-commands.md), the appends across all the event sources are atomic — if any of them is rejected, none of them land.

Return a single `EventForEventSourceId` when only one cross-source event is needed:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
public record MigrateCustomerToNewId(EventSourceId OldCustomerId, EventSourceId NewCustomerId)
{
    public EventForEventSourceId Handle() =>
        new(NewCustomerId, new CustomerMigrated(OldCustomerId, NewCustomerId));
}

[EventType]
public record CustomerMigrated(EventSourceId OldCustomerId, EventSourceId NewCustomerId);
```

Return an `IEnumerable<EventForEventSourceId>` to append events to several different event sources in one command:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
public record TransferFunds(EventSourceId FromAccountId, EventSourceId ToAccountId, decimal Amount)
{
    public IEnumerable<EventForEventSourceId> Handle() =>
    [
        new EventForEventSourceId(FromAccountId, new FundsDebited(Amount)),
        new EventForEventSourceId(ToAccountId, new FundsCredited(Amount))
    ];
}

[EventType]
public record FundsDebited(decimal Amount);

[EventType]
public record FundsCredited(decimal Amount);
```

Chronicle enrolls the events in order in the command transaction. They commit through one atomic append when the command succeeds. A constraint violation, concurrency conflict, or append error rejects the whole batch and becomes an ordinary failed `CommandResult`; no event from the returned batch lands.

You can mix `EventForEventSourceId` values with regular events in a tuple return, letting some events use the command's own event source while others target specific event sources:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
public record AcceptOrder(EventSourceId OrderId, EventSourceId CustomerId)
{
    public (OrderAccepted, EventForEventSourceId) Handle() =>
        (
            new OrderAccepted(OrderId),
            new EventForEventSourceId(CustomerId, new CustomerOrderAccepted(OrderId))
        );
}

[EventType]
public record OrderAccepted(EventSourceId OrderId);

[EventType]
public record CustomerOrderAccepted(EventSourceId OrderId);
```

> `EventForEventSourceId` does not share one concurrency scope across targets — a scope carries a single stream's expected tail, so it cannot be reused for another stream. The command's concurrency declaration still applies: one scope is built per target event source, with that target's own expected tail. Each append also uses the stream metadata from the command (stream id, stream type, event source type) while targeting the event source id you supply explicitly.

## Events with exact concurrency scopes

The automatic strategy resolves a target's expected tail after `Handle()` returns. That is right for ordinary optimistic concurrency. When a command makes its decision from an exact revision it already read, return `EventsWithConcurrencyScopes` to carry that revision with the events instead of resolving a newer tail later.

The response contains two values:

- the `EventForEventSourceId` values, in append order; and
- the exact concurrency scopes the decision depended on, keyed by labels you choose.

A label can name an event target, but it does not have to. An independent label lets a command protect a broader fact — for example, the tail of all active-administrator events — while writing to member and invitation streams.

Exact revisions that govern authorization or another invariant must never come from request input. Resolve the authoritative revision on the server while handling the command, and construct any independent scope label from a deterministic server-owned value. Otherwise, a caller could choose which version of the protected fact the command validates.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[Command]
public record InviteFirstAdministrator(
    EventSourceId MemberId,
    EventSourceId InvitationId)
{
    static readonly EventSourceId AdministratorScope = "active-administrators";

    public async Task<EventsWithConcurrencyScopes> Handle(IEventLog eventLog)
    {
        var activeAdministratorEvent = typeof(AdministratorActivated).GetEventType();
        var expectedAdministratorRevision = await eventLog.GetTailSequenceNumber(
            filterEventTypes: [activeAdministratorEvent]);

        return new EventsWithConcurrencyScopes(
            [
                new(MemberId, new MemberInvited()),
                new(InvitationId, new InvitationIssued(MemberId))
            ],
            [
                new(
                    AdministratorScope,
                    new ConcurrencyScope(
                        expectedAdministratorRevision,
                        EventTypes: [activeAdministratorEvent]))
            ]);
    }
}

[EventType]
public record AdministratorActivated;

[EventType]
public record MemberInvited;

[EventType]
public record InvitationIssued(EventSourceId MemberId);
```

Arc enrolls the response in the command's existing unit of work. The event order, exact scope labels, and exact scope values are passed to Chronicle together; the command does not append immediately. If the protected fact changes between the decision and commit, the command returns a concurrency validation failure and none of its returned events land.

Use an exact scope only for a revision the command actually read. `ConcurrencyScope.NotSet` retains the event sequence's configured strategy for an event-target label, while `ConcurrencyScope.None` deliberately disables checking for that label. An independent label must carry a concrete exact scope or `ConcurrencyScope.None` because there is no target from which Chronicle can infer a scope.
