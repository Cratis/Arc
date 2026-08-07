---
title: "ARCCHR0003: Reactor must not reach the default event log"
description: A reactor appends to the default event log directly instead of returning the events from its handler method.
---

## Rule

A reactor produces side-effect events by **returning** them from its handler method — Chronicle appends what a handler returns. Reaching the default event log yourself performs the same write, but outside the side-effect pipeline.

This rule fires on every way of reaching it:

| Shape | What it looks like |
|---|---|
| Injected event log | `MyReactor(IEventLog eventLog) : IReactor` |
| Event log through the store | `eventStore.EventLog.Append(...)` |
| Default log named explicitly | `eventStore.GetEventSequence(EventSequenceId.Log).Append(...)` |
| Enlisted in a unit of work | `eventStore.EventLog.Transactional.Append(...)` |
| Reached with a null-conditional | `eventStore?.EventLog.Append(...)` |

`Transactional` hands back the same sequence enlisted in a unit of work, so the write is identical — the chain is simply one member longer. Every `Append*` overload counts, `AppendMany` included, and `?.` anywhere in the chain changes nothing.

## Severity

Warning

## Example

### Violation

```csharp
[Reactor]
public class IncomingInvitationReactor(IEventStore eventStore) : IReactor
{
    [OnceOnly]
    public Task On(UserInvited @event, EventContext context) =>
        // ARCCHR0003: appends to the sequence the return type already targets
        eventStore.EventLog.Append(context.EventSourceId, new InvitationIssued(@event.Email));
}
```

### Fix

```csharp
[Reactor]
public class IncomingInvitationReactor : IReactor
{
    [OnceOnly]
    public InvitationIssued On(UserInvited @event) => new(@event.Email);
}
```

Return a single event, an `IEnumerable<object>`, or `EventForEventSourceId` wrappers when the events belong to a different event source. To trigger work in another slice, inject `ICommandPipeline` and [execute a command](../reactors/command-side-effects.md).

## When It Does Not Fire

**Injecting `IEventStore` is not itself a violation.** The rule reports the *append*, not the dependency — a reactor that injects the store to read from it, to reach `IReadModels`, or to route to a different sequence is left alone.

Routing to another sequence is the case that matters most:

```csharp
[Reactor]
public class AcceptanceOutbox(IEventStore eventStore) : IReactor
{
    [OnceOnly]
    public Task On(InvitationAccepted @event, EventContext context) =>
        eventStore.GetEventSequence(EventSequenceId.Outbox)
            .Append(context.EventSourceId, @event);
}
```

A returned side-effect event is always appended to the **default event log** — neither a bare event nor an `EventForEventSourceId` carries an `EventSequenceId` — so the outbox is not expressible as a return value and the store is the only way to reach it. The rule stays silent whenever `GetEventSequence` names anything other than the default log, including a sequence resolved at runtime.

**Another event store is the same kind of exception.** A returned event goes to *this* reactor's own store, in *this* namespace. A store the reactor obtains at runtime targets a different one, so the rule's advice would send the event somewhere else entirely:

```csharp
[Reactor]
public class AuthorReplicator(IChronicleClient client) : IReactor
{
    [OnceOnly]
    public async Task On(AuthorRegistered @event, EventContext context)
    {
        var other = await client.GetEventStore("Reporting", "tenant-x");
        await other.EventLog.Append(context.EventSourceId, @event);
    }
}
```

The rule reports an append only through the event store the reactor **holds** — a constructor parameter, a field, or a property. Anything else is treated as a store the reactor went and found.

## Why This Rule Exists

The two shapes write the same event to the same sequence, so the rule's reason applies to both: replay and side-effect semantics stay Chronicle's concern only while the append goes through the return type.

The cost shows up later, in the testing surface. `ReactorScenario<T>.Produced` is *the side effects the reactor returned from its handler methods* — a handler that returns bare `Task` and appends through an injected store produces nothing by that definition, so `ShouldHaveProduced<T>()` throws and the sanctioned assertion surface is simply unavailable. Nothing at authoring time tells you that you have left the contract; this rule does.

## Related Rules

- [ARCCHR0005](ARCCHR0005.md) — Chronicle is used but not wired up
