---
title: "ARCCHR0003: Reactor must not reach the default event log"
description: A reactor appends to the default event log directly instead of returning the events from its handler method.
---

## Rule

A reactor produces side-effect events by **returning** them from its handler method — Chronicle appends what a handler returns. Reaching the default event log yourself performs the same write, but outside the side-effect pipeline.

This rule fires on every way of reaching it:

| Shape | What it looks like |
| --- | --- |
| Injected event log | `MyReactor(IEventLog eventLog) : IReactor` |
| Event log through the store | `eventStore.EventLog.Append(...)` |
| Default log named explicitly | `eventStore.GetEventSequence(EventSequenceId.Log).Append(...)` |
| Enlisted in a unit of work | `eventStore.EventLog.Transactional.Append(...)` |
| Reached with a null-conditional | `eventStore?.EventLog.Append(...)` |

`Transactional` hands back the same sequence enlisted in a unit of work, so the write is identical — the chain is one member longer. Every `Append*` overload counts, `AppendMany` included, and `?.` anywhere in the chain changes nothing.

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

## What The Rule Cannot See

The analyzer reads the call in front of it. It does not follow values, so an append the rule would otherwise report goes unreported whenever the thing being appended to arrives from somewhere else in the method:

| Shape | Why it is missed |
| --- | --- |
| `var log = eventStore.EventLog; log.Append(...)` | The sequence is held in a local. |
| `var store = eventStore; store.EventLog.Append(...)` | The store is held in a local, and a local can hold another store as readily as this one. |
| `GetEventSequence(sequenceId)` where `sequenceId` is a parameter or a field | The sequence is only known at run time; it may or may not be the default log. |
| `GetEventSequence(flag ? EventSequenceId.Log : EventSequenceId.Outbox)` | Same — the analyzer cannot pick a branch. |
| An append in a base class that does not itself implement `IReactor` | The rule decides from the type the append sits in, and that type is not a reactor. |
| An append in a helper type the reactor delegates to | Same — the helper is not a reactor. |

None of this is a suppression mechanism to reach for. It is the honest boundary of a syntactic rule: **it catches the shape you write by accident, not the one you write to get around it.** Every one of these still costs you `ReactorScenario<T>.Produced` at spec time, which is where the absence really bites.

## Existing Code May Now Warn

ARCCHR0003 previously matched only an injected `IEventLog`. It now also matches an append through an injected `IEventStore`, so a reactor with that shape starts warning on the first build after you upgrade — and if you build with warnings as errors, that is a build break rather than a warning.

**The migration is to return the event.** Change the handler from `Task` to the event type and drop the store:

```csharp
// Before — warns, and ReactorScenario<T>.Produced sees nothing
public Task On(UserInvited @event, EventContext context) =>
    eventStore.EventLog.Append(context.EventSourceId, new InvitationIssued(@event.Email));

// After — Chronicle appends the returned event, and ShouldHaveProduced<InvitationIssued>() works
public InvitationIssued On(UserInvited @event) => new(@event.Email);
```

If the append targets a different event source, return `EventForEventSourceId(id, @event)` — or an `IEnumerable<object>` mixing bare events and wrappers. If it targets a different sequence or a different store, the rule does not fire in the first place.

## Ordered manual appends

Keep the warning for ordinary default-log appends. If a reactor must inspect an append result before deciding the next append, wait for observer completion, or pass append options that returned events cannot express, a **narrow, justified suppression** is appropriate. Return events instead when you do not need that control. Do not turn the rule off project-wide to accommodate one workflow.

This handler-body excerpt assumes an `IEventStore eventStore` held by the reactor, an `EventContext context`, application event records `InvitationIssued` and `InvitationTracked` (the latter accepts an `EventSequenceNumber`), and an application exception `InvitationAppendFailed`. It uses the first append's result to construct the second event:

```csharp
// The second append needs the first append's sequence number; returned events cannot supply it here.
#pragma warning disable ARCCHR0003
var issued = await eventStore.EventLog.Append(context.EventSourceId, new InvitationIssued(@event.Email));
if (!issued.IsSuccess)
{
    throw new InvitationAppendFailed();
}
var tracked = await eventStore.EventLog.Append(context.EventSourceId, new InvitationTracked(issued.SequenceNumber));
#pragma warning restore ARCCHR0003
if (!tracked.IsSuccess)
{
    throw new InvitationAppendFailed();
}
```

Keep `#pragma warning restore ARCCHR0003` immediately after the ordered appends, and explain why returning events cannot represent this particular workflow. Handle every append failure; an earlier successful append is not undone if a later append fails. The same narrow-scope rule applies if your reason is a completion wait or append options rather than an append result.

Manual appends bypass the **returned-side-effect observation**: `ReactorScenario<T>.Produced` does not contain them, and `ShouldHaveProduced<T>()` cannot assert them. Add separate tests for the actual appends, their outcomes, and their order. Suppression changes only the diagnostic: it gives no replay protection or transactional guarantee. Decide replay behavior explicitly and make the workflow safe to retry.

## Why This Rule Exists

The two shapes write the same event to the same sequence, so the rule's observation boundary applies to both. Returning an event lets Chronicle observe it as a reactor side effect; manual appends need their own tests and explicit replay and failure handling.

The cost shows up later, in the testing surface. `ReactorScenario<T>.Produced` is *the side effects the reactor returned from its handler methods* — a handler that returns bare `Task` and appends through an injected store produces nothing by that definition, so `ShouldHaveProduced<T>()` throws and the sanctioned assertion surface is simply unavailable. Nothing at authoring time tells you that you have left the contract; this rule does.

## Related Rules

- [ARCCHR0005](ARCCHR0005.md) — Chronicle is used but not wired up
