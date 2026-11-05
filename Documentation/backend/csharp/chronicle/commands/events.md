---
title: Return events from Chronicle commands
description: Return one or more events from an Arc command, route them to Chronicle event sources, and reject invalid decisions without appending pending events.
---

Return an event from a [model-bound command](../../commands/model-bound/index.md) to record a decision in Cratis Chronicle without calling the event log yourself. Cratis Arc resolves the destination from the command and appends the returned fact. For example, `RegisterCustomer` returns `CustomerRegistered` under the customer's event source id.

Returned events enroll in the command's pending transaction. The batch commits atomically on success and rolls back on failure, unless code explicitly completed the shared transaction earlier. Keep writes on this path when a later rejection must undo the pending work; [transactional commands](./transactional-commands.md) explains the boundary.

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

Both examples use the command's event source id and stream metadata. Use a tuple when the caller also needs a response, such as the [new entity's identity](./returning-event-source-id.md). The event is handled on the server; it is not the response data.

## Reject a command without recording an event

When a rule forbids the action, return a validation failure, not a nullable event or an empty collection. Returning no events does not mark a command as failed, so pending aggregate or transactional writes could still commit.

Use `Result<TEvent, ValidationResult>` when `Handle()` must choose between an event and a rejection. Return a `ValidationResult.Error(...)` for the rejected decision. Arc processes the active alternative: the event is appended, or the blocking validation result fails the command and rolls back its pending transaction. A plain error DTO in a `Result` alternative remains response data; its type name does not make the command fail.

Put rules that need pre-flight feedback in a validator. A rule that depends on data available only during handling can reject from `Handle()`; see [choosing a rejection phase](../../commands/validation.md#choosing-a-rejection-phase). Neither form of rejection undoes an earlier immediate append, an explicit commit, or an external service call.

## Event tags

Attach structured named tags to every returned event using `[EventTag]`, `ICanProvideEventTags`, a returned `EventTags` value, or application-wide `ICanProvideCommandEventTags` providers. Command tags form a union with each wrapper's `NamedTags`; the same name with different values keeps both values. See [Event tags](./event-tags.md) for examples and merge rules.

## Event Source Id Resolution

The event source id identifies whose history the command changes. Arc resolves it before handling and stores it in the command context.

Implement `ICanProvideEventSourceId` to choose the identity explicitly. Otherwise Arc selects the first property typed as `EventSourceId`, derived from `EventSourceId<T>`, or marked with Chronicle's `[Key]`. Typed and attributed properties have equal priority, so declare one unambiguous candidate. If there is no candidate, Arc creates a new `EventSourceId` for the append.

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

Add stream metadata when events for one source need separate routes, such as onboarding and transactions for the same customer. Arc passes this metadata to the append, where it also affects the concurrency boundary.

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

The event sequence's configured concurrency strategy resolves its expected tail using the append's metadata. A routing attribute can therefore narrow the check even without `concurrency: true`. Set `concurrency: true` when you want to select the dimensions explicitly; see [concurrency scoping](./concurrency.md) before choosing the boundary.

### Event source definitions

The [event source definition guide](./event-source-definitions.md) covers choosing the stream, stream ids, what the definition guards and migrating from strings; [scenarios](./event-source-definition-scenarios.md) and [how routing works](./event-source-definition-routing.md) go further. Chronicle's [event sources](/chronicle/event-sources/) page describes the definition itself, and its [Dynamic Consistency Boundary](/chronicle/dynamic-consistency-boundary/) pages explain the boundary the append is checked against.

Use `[EventSource<TSource>]` when a command always appends through a registered Chronicle event source definition. The definition supplies the event source type, validates the optional stream name, records the definition in `EventContext.EventSource`, and supplies its concurrency dimensions unless a legacy `concurrency: true` attribute explicitly selects dimensions.

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.EventSources;

[EventSource]
[EventStream("transactions")]
public class Account : IEventSource;

[Command]
[EventSource<Account>("transactions")]
public record RecordTransaction(AccountId AccountId, decimal Amount)
{
    public TransactionRecorded Handle() => new(Amount);
}
```

A returned `EventForEventSourceId` can route one event differently from the command. The definition and the stream are one routing decision:

- An event that sets only `EventStream` uses the command's definition and that stream.
- An event that sets `EventSource` uses that definition and its own `EventStream`; it inherits nothing from the command's stream.
- An event that sets the legacy `EventSourceType` or `EventStreamType` is appended by those strings, not through the command's definition, because appending through the definition would silently discard them. Unset values inherit the command's strings. On a definition-routed command, Arc refuses a wrapper-named stream that a known definition does not declare; a source-only override with an inherited stream remains valid.
- `EventStreamId` and `Subject` on the event are independent of this and win over the command when set.

The concurrency dimensions the definition declares apply only when no legacy attribute opts into concurrency with `concurrency: true`. A legacy attribute that does not opt in carries metadata only, so it does not displace the definition's dimensions.

Do not combine a definition declaration with contradictory `[EventSourceType]` or `[EventStreamType]` attributes. Arc rejects the command context rather than append an event whose string metadata disagrees with the definition. Existing string attributes remain supported for commands that do not use definitions.

## Events for Specific Event Sources

Use `EventForEventSourceId` when a command must write to a source other than its own, or to several sources. A transfer, for example, must debit one account and credit another; one command-level id cannot identify both destinations.

`EventForEventSourceId` is a record that pairs an event with an explicit `EventSourceId`. Chronicle appends each event to its specified event source, independently of the event source id in the command context. Within an active [command transaction](./transactional-commands.md), the appends across all the event sources are atomic — if any of them is rejected, none of them land. Without an active transaction, a returned collection still appends as one batch: a refusal appends nothing. That includes a collection that sends events for the same event source id to several routes: the batch carries a guard for each route, and a violation of any of them refuses the whole batch. Sending several guards for one event source id needs a Chronicle kernel 19.84 or later; against an older kernel the Chronicle client refuses the batch with `IncompatibleConcurrencyScopesForEventSource` before sending anything, so nothing is appended.

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

These wrappers commit atomically, but ordinary unit-of-work staging groups them by source. `A1, B1, A2` can therefore commit as `A1, A2, B1`, even though Arc enumerates them in return order. Use [`EventsWithConcurrencyScopes`](#events-with-exact-concurrency-scopes) when global batch order matters.

A constraint violation, concurrency conflict, or append error rejects the whole batch and becomes a failed `CommandResult`. No event from the returned batch is appended.

A wrapper can also carry when the fact happened and how to tag it. Set `Occurred` to record a historical occurrence time, for example when importing or backfilling, and `Tags` to add tags to that one event. Chronicle merges them with any static `[Tag]` on the event type:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[Command]
public record ImportCredit(EventSourceId AccountId, decimal Amount, DateTimeOffset CreditedAt)
{
    public EventForEventSourceId Handle() =>
        new(AccountId, new FundsCredited(Amount))
        {
            Occurred = CreditedAt,
            Tags = ["imported"]
        };
}
```

Arc keeps both for a single wrapper, a collection, a mixed collection, and `EventsWithConcurrencyScopes`, whether the command appends immediately or through its transaction. Leave `Occurred` unset to use the append time. The wrapper's routing properties (`EventStreamType`, `EventStreamId`, `EventSourceType`, and `Subject`) win over the command context when set, as described in [per-event routing](#per-event-routing); otherwise those come from the command context, as described in [event stream metadata](#event-stream-metadata).

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

### Concurrency across targets

`EventForEventSourceId` does not reuse one target's expected tail for another. The command's concurrency declaration produces a scope for each target using that target's own tail. Each append uses the command's stream metadata unless its wrapper overrides the route.

For definition-routed events, the event sequence derives guards from each event's own stream and definition. It does not reuse one event's guard for another. Legacy-routing overrides that require different guards for the same source retain every distinct guard. All must hold for the transaction, or the immediate batch, to succeed. Guards for the same stream and event types collapse to one.

Several guards for one id require Chronicle kernel 19.84 or later. An older kernel throws `IncompatibleConcurrencyScopesForEventSource` before appending anything. Mixing legacy guards with a definition-derived guard for the same id also throws that error before enrollment or append: supplying a scope for the id stops Chronicle from deriving its definition guard.

An explicit `concurrency: true` declaration keeps its existing behavior. If your command needs a combination these routes cannot express, split the work into separate commands or return `EventsWithConcurrencyScopes` with an explicit scope.

## Events with exact concurrency scopes

Return `EventsWithConcurrencyScopes` when a command must protect the exact revision it read while deciding. A concurrency scope pairs that expected revision with the events it watches. The append fails if matching history changed before commit.

Automatic scopes resolve the target's expected tail after `Handle()` returns: during response processing for attribute-selected scopes, or during commit for the fallback scope. They cannot by themselves preserve an earlier read. Carry that read's revision with the returned events rather than checking against a newer tail.

The response contains two values:

- the `EventForEventSourceId` values, in append order; and
- the exact concurrency scopes the decision depended on, keyed by labels you choose.

### Exact scopes replace derived scopes

Returning `EventsWithConcurrencyScopes` replaces scopes Arc would derive from the command's routing declarations. This includes `concurrency: true` on `[EventSourceType]`, `[EventStreamType]` and `[EventStreamId]`, as well as an event source definition's concurrency dimensions.

Remove `concurrency: true` from those command attributes when returning exact scopes. It no longer affects the append, and [ARCCHR0016](../code-analysis/ARCCHR0016.md) offers a quick fix to remove it. The definition itself remains the policy for other writers and is not reported.

A [generated identifier's freshness guard](./fresh-identifiers.md) still applies. Returned scopes cannot replace its whole-source emptiness requirement with a narrower check.

### Per-event routing

Set routing on an `EventForEventSourceId` to override the command context for that event. This works for a single wrapper, collections, mixed collections, tuples such as `(FundsSettled, EventForEventSourceId)`, and `EventsWithConcurrencyScopes`. Plain events still use the command context.

A routing override counts as set when it differs from `EventSourceType.Default`, `EventStreamType.All` or `EventStreamId.Default`; a `Subject` override must be non-null. A value left at its default falls back to the command context. One event can therefore choose a different stream while the others keep the command's route.

Prefer typed overrides (`EventSource = typeof(Definition)` and `EventStream = "stream"`) when the target has an event source definition. Choosing another declared stream or another definition is supported; a different route is not itself a mistake. A stream-only typed override uses the command's definition.

Use `IEventRoutes` to resolve a definition route once and stamp returned events without repeating legacy strings:

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

[EventSource]
[EventStream("Transactions")]
[EventStream("Adjustments")]
public class AccountEventSource : IEventSource;

[Command]
[EventSource<AccountEventSource>("Transactions")]
public record AdjustAccount(EventSourceId AccountId, decimal Amount)
{
    public EventForEventSourceId Handle(IEventRoutes routes) =>
        routes.For<AccountEventSource>("Adjustments").Route(AccountId, new BalanceAdjusted(Amount));
}

[EventType]
public record BalanceAdjusted(decimal Amount);
```

This event uses the `Adjustments` stream and retains `AccountEventSource` and its concurrency policy, even though the command declares `Transactions`.

Legacy string overrides remain supported, but they append by strings and drop the definition from `EventContext.EventSource`; they also omit the selected definition's concurrency policy. [ARCCHR0021](../code-analysis/ARCCHR0021.md) suggests typed routing when literal strings name a declared stream, and calls an override redundant when it repeats the command route. Its quick fix retains the definition, so review the concurrency change before applying it.

[ARCCHR0020](../code-analysis/ARCCHR0020.md) warns when a literal wrapper route names a known definition and an undeclared stream on the wrapper itself. At runtime, a **definition-routed command** rejects a legacy wrapper route with `EventStreamDoesNotBelongToEventSource` when its resolved source matches a registered definition but its wrapper-named `EventStreamType` is undeclared. Arc checks each wrapper before enrolling or appending it, including wrappers in mixed collections, tuples, and `EventsWithConcurrencyScopes`. Collections and exact-scope responses are resolved before their first event is enrolled or appended. Computed strings get the same runtime check. Unknown source names, unset stream sentinels, source-only overrides with inherited streams, and commands without a definition keep their legacy behavior. A stream-only override is checked against the command's definition. Valid legacy strings are not silently converted to typed routes.

The framework resolves response handlers through their constructors taking `IEventSources`. Their obsolete legacy constructors remain available for manually constructed handlers, but do not validate per-event routing against the registry; migrate manual construction to the new constructors to get this check.

An empty response with no events and no concurrency scopes succeeds without appending or enrolling a batch. It is not a rejection and does not revalidate an earlier read. If an equivalent declaration should make the command invalid, [return a validation failure](#reject-a-command-without-recording-an-event) instead. If scopes are supplied, Arc forwards them through the normal append or transaction path even when the event list is empty.

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

`ICanProvideEventSourceId` deliberately selects `MemberId` as the input-time command identity, including for any identity-bound dependencies. The returned wrappers still target their explicit member and invitation ids. This removes the two-property ambiguity reported by [ARCCHR0002](../code-analysis/ARCCHR0002.md); `EventsWithConcurrencyScopes` is not a recognized exemption in the current analyzer.

The narrow ARCCHR0007 suppression documents an intentional read-only `IEventLog` dependency; the analyzer matches the parameter type, not whether a method writes.

Use an exact scope only for a revision the command actually read. `ConcurrencyScope.NotSet` retains the event sequence's configured strategy for an event-target label, while `ConcurrencyScope.None` deliberately disables checking for that label. An independent label must carry a concrete exact scope or `ConcurrencyScope.None` because there is no target from which Chronicle can infer a scope.
