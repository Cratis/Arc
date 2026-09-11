---
title: Events
description: What a command Handle() can return — a single event, several, a tuple, a Result, or nothing — and what Chronicle does with each.
---

When a [model-bound](../../commands/model-bound/index.md) command handler returns an event (or a collection of events), Chronicle appends those events to the event log automatically. This lets you keep command handlers focused on decisions and domain rules instead of event log plumbing.

Return-driven appends enroll in the command's pending transaction: the batch commits atomically on success and rolls back on failure, unless code explicitly completed that shared transaction earlier — see [Transactional commands](./transactional-commands.md).

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record RegisterCustomer(EventSourceId CustomerId, string Email)
{
    public CustomerRegistered Handle()
    {
        return new CustomerRegistered(Email);
    }
}

[EventType]
public record CustomerRegistered(string Email);
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
            new CustomerDisplayNameChanged(DisplayName),
            new CustomerEmailChanged(Email)
        };
    }
}

[EventType]
public record CustomerDisplayNameChanged(string DisplayName);

[EventType]
public record CustomerEmailChanged(string Email);
```

Chronicle uses the command context to resolve the event source identity and event stream metadata before appending events.

## Event Source Id Resolution

Chronicle resolves the event source id for commands using a small set of conventions. This value is stored in the command context and is required for event appending.

`ICanProvideEventSourceId` takes precedence. Otherwise Arc selects the first property matching any of: `EventSourceId`, `EventSourceId<T>` ancestry, or Chronicle `[Key]`. There is no typed-before-key precedence; use one unambiguous candidate.

If none of these are present, Chronicle creates a new `EventSourceId` so the command still has a valid identity for event appends.

The following declarations are identity fragments; add a public instance `Handle()` for a complete command.

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

For command input identity, ordinary query binding (which does not use these conventions), and the later append override from a returned `EventSourceId`, see [Resolving EventSourceId](../resolving-event-source-id.md) and [Returning EventSourceId](./returning-event-source-id.md).

## Event Stream Metadata

Chronicle supports additional metadata that can be attached to commands and used when appending events. This metadata tags the appended events with the specified stream identity, making them easier to query and react to.

### EventStreamId

Use `[EventStreamId]` to assign a specific event stream id to a command, or implement `ICanProvideEventStreamId` to supply it dynamically. The declarations in this metadata section are attribute-focused fragments, not complete commands; supply `Handle()` in your application.

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

`EventForEventSourceId` is a record that pairs an event with an explicit `EventSourceId`. Chronicle appends each event to its specified event source, independently of the event source id in the command context. Because the command is a [transactional scope](./transactional-commands.md), the appends across all the event sources are atomic — if any of them is rejected, none of them land.

Return a single `EventForEventSourceId` when only one cross-source event is needed:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[Command]
public record MigrateCustomerToNewId(EventSourceId OldCustomerId, EventSourceId NewCustomerId)
{
    public EventForEventSourceId Handle() =>
        new(NewCustomerId, new CustomerMigrated(OldCustomerId));
}

[EventType]
public record CustomerMigrated(EventSourceId OldCustomerId);
```

Return an `IEnumerable<EventForEventSourceId>` to append events to several different event sources in one command:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

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

Arc enumerates and enrolls these wrappers in order, but that is not a guarantee of cross-source batch order. Chronicle's ordinary unit-of-work staging groups events by source: `A1, B1, A2` can commit as `A1, A2, B1`. Use [`EventsWithConcurrencyScopes`](#events-with-exact-concurrency-scopes) when global batch order matters. The events still commit through one atomic append when the command succeeds. A constraint violation, concurrency conflict, or append error rejects the whole batch and becomes an ordinary failed `CommandResult`; no event from the returned batch lands.

You can mix `EventForEventSourceId` values with regular events in a tuple return, letting some events use the command's own event source while others target specific event sources:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[Command]
public record AcceptOrder(EventSourceId OrderId, EventSourceId CustomerId) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => OrderId;

    public (OrderAccepted, EventForEventSourceId) Handle() =>
        (
            new OrderAccepted(),
            new EventForEventSourceId(CustomerId, new CustomerOrderAccepted(OrderId))
        );
}

[EventType]
public record OrderAccepted;

[EventType]
public record CustomerOrderAccepted(EventSourceId OrderId);
```

> `EventForEventSourceId` does not share one concurrency scope across targets — a scope carries a single stream's expected tail, so it cannot be reused for another stream. The command's concurrency declaration still applies: one scope is built per target event source, with that target's own expected tail. Each append also uses the stream metadata from the command (stream id, stream type, event source type) while targeting the event source id you supply explicitly.

## Events with exact concurrency scopes

The automatic strategy resolves a target's expected tail after `Handle()` returns: during response processing for attribute-selected scopes, or during commit for the fallback scope. That is right for ordinary optimistic concurrency. When a command makes its decision from an exact revision it already read, return `EventsWithConcurrencyScopes` to carry that revision with the events instead of resolving a newer tail later.

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
    EventSourceId InvitationId) : ICanProvideEventSourceId
{
    static readonly EventSourceId AdministratorScope = "active-administrators";

    public EventSourceId GetEventSourceId() => MemberId;

    // Intentional server-side read; no imperative append bypasses the return path.
#pragma warning disable ARCCHR0007
    public async Task<EventsWithConcurrencyScopes> Handle(IEventLog eventLog)
#pragma warning restore ARCCHR0007
    {
        var activeAdministratorEvent = typeof(AdministratorActivated).GetEventType();
        var tail = await eventLog.GetTailSequenceNumber(
            filterEventTypes: [activeAdministratorEvent]);
        var expectedAdministratorRevision = tail == EventSequenceNumber.Unavailable
            ? EventSequenceNumber.BeforeFirst
            : tail;

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

This is a concurrency-routing example, not a complete first-administrator business rule. It watches changes to `AdministratorActivated`, not invitation uniqueness, and does not reject an already-active administrator by itself. Authorize and validate that decision separately.

When the matching history is empty, `BeforeFirst` expresses “no matching event may exist”; `Unavailable` is not a protected empty revision. Arc enrolls the ordered events and exact scopes together. If a matching activation event appears after the read, commit rejects the returned batch. A concurrent invitation without an activation is outside this scope.

`ICanProvideEventSourceId` deliberately selects `MemberId` as the input-time command identity, including for any identity-bound dependencies. The returned wrappers still target their explicit member and invitation ids. This removes the two-property ambiguity reported by [ARCCHR0002](../code-analysis/index.md#arcchr0002-ambiguous-command-identity); `EventsWithConcurrencyScopes` is not a recognized exemption in the current analyzer.

The narrow ARCCHR0007 suppression documents an intentional read-only `IEventLog` dependency; the analyzer matches the parameter type, not whether a method writes.

Use an exact scope only for a revision the command actually read. `ConcurrencyScope.NotSet` retains the event sequence's configured strategy for an event-target label, while `ConcurrencyScope.None` deliberately disables checking for that label. An independent label must carry a concrete exact scope or `ConcurrencyScope.None` because there is no target from which Chronicle can infer a scope.
