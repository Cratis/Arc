---
title: "ARCCHR0006: Reactor handler invoking ICommandPipeline.Execute does not say what replay should do"
description: A reactor handler calls ICommandPipeline.Execute without declaring what should happen to that call when the observer replays.
---

## Rule

A reactor handler that calls `ICommandPipeline.Execute` produces a side effect — running a command, which can itself append events, call external systems, or both. When the observer replays (redaction, revision, an observer rewind), the same handler runs again for the same event unless something says otherwise, so `Execute` runs again and the side effect repeats.

This rule fires whenever a handler Chronicle would actually dispatch to reaches `ICommandPipeline.Execute` — directly, or through any number of private helpers on the same reactor — without a decision for what replay should do: neither `[OnceOnly]` nor a `[Replay]` handler for the same event type.

## Severity

Warning

## Example

### Violation

```csharp
using Cratis.Chronicle.Reactors;
using Cratis.Arc.Commands;

public class OrderProcessing(ICommandPipeline commandPipeline) : IReactor
{
    // ARCCHR0006: replay re-executes ChargeCard for the same order
    public Task On(OrderPlaced @event) =>
        commandPipeline.Execute(new ChargeCard(@event.OrderId));
}
```

### Fix

Mark the handler `[OnceOnly]` when replay should simply not repeat the charge:

```csharp
using Cratis.Chronicle.Reactors;
using Cratis.Arc.Commands;

public class OrderProcessing(ICommandPipeline commandPipeline) : IReactor
{
    [OnceOnly]
    public Task On(OrderPlaced @event) =>
        commandPipeline.Execute(new ChargeCard(@event.OrderId));
}
```

Or declare a `[Replay]` handler when replay should do something else — including nothing, stated explicitly:

```csharp
using Cratis.Chronicle.Reactors;
using Cratis.Arc.Commands;

public class OrderProcessing(ICommandPipeline commandPipeline) : IReactor
{
    public Task On(OrderPlaced @event) =>
        commandPipeline.Execute(new ChargeCard(@event.OrderId));

    [Replay]
    public Task OnReplay(OrderPlaced @event) => Task.CompletedTask;
}
```

## Choosing between [OnceOnly] and [Replay]

The two attributes answer different questions, and they are not interchangeable.

**`[OnceOnly]` is scoped to the whole replay of an event source, not to one occurrence of the event.** While a source is being replayed, every occurrence of that handler's event type on that source is skipped — not only whichever occurrence already ran live. That is the right shape for a side effect that must never repeat because it already happened during the live pass: send the confirmation email once, charge the card once. It is the wrong shape whenever the handler has to run again during a replay — an event type that legitimately recurs on the same source and needs the same (or different) handling every time a rebuild replays it. `[OnceOnly]` does not distinguish that case from the one it is designed for: the handler is skipped for every occurrence, silently, with nothing in the observer's state to say so.

**`[Replay]` declares a separate handler for the same event type that takes over for the entire replay**, leaving the live handler untouched. This is the right shape whenever replay should do something rather than nothing — including calling `Execute` again deliberately, running an adjusted version of the side effect, or doing nothing at all. An empty `[Replay]` body is a legitimate, common answer: it is the explicit version of what `[OnceOnly]` does implicitly, and this rule treats declaring one the same way it treats `[OnceOnly]` — as proof the decision was made, not as a decision this rule second-guesses.

## When It Does Not Fire

The rule only considers the handlers Chronicle's dispatch would actually select for an event type, and follows a call through private helpers on the same reactor back to the handler that reaches it:

| Shape | Why it is left alone |
|---|---|
| The handler, or the whole reactor class, carries `[OnceOnly]` | The replay decision has already been made |
| A sibling handler for the same event type carries `[Replay]` | The decision has been made, even if the live handler is unmarked |
| A private method shares an event type with a public handler | Chronicle would only ever dispatch to the public one; the private method's call is unreachable in practice |
| `Execute` is called on something other than `ICommandPipeline` | Not the call this rule is about |
| The type does not implement `IReactor` | Not a reactor |

## What The Rule Cannot See

The rule reads calls within one reactor's own declared members. A few shapes reach `Execute` in ways it cannot follow:

| Shape | Why it is missed |
|---|---|
| `Execute` called through a helper on a different type the reactor delegates to | The call graph this rule walks is limited to methods declared on the reactor itself |
| A handler inherited from a base type | The rule inspects the reactor type's own declared members, not members it inherits |
| An event type recognized only through a base type or interface, rather than as the parameter's exact declared type | Dispatch-candidate detection looks for `[EventType]` on the parameter's own type |
| A reactor that returns a command for `ICommandPipeline` to execute, rather than calling `Execute` itself | There is no `Execute` invocation for the rule to find — nothing here indicates a side effect needing a replay decision |

None of this is a suppression mechanism to reach for. It is the boundary of a call-graph rule confined to one type: it catches the call you write directly or through a private helper, not one reached through another type entirely.

## When The Rule Is Wrong

A handler can call `Execute` in a shape neither attribute fits cleanly — the command is itself idempotent by construction, for instance, so re-running it during replay is safe by design rather than by a `[OnceOnly]`/`[Replay]` decision. **Suppress the diagnostic — do not mark the handler `[OnceOnly]` just to silence the warning:**

```csharp
[SuppressMessage("Arc.Chronicle", "ARCCHR0006", Justification = "ChargeCard is idempotent per order and safe to re-execute on replay")]
public Task On(OrderPlaced @event) =>
    commandPipeline.Execute(new ChargeCard(@event.OrderId));
```

Marking the handler `[OnceOnly]` to make the warning go away is the one response this rule tries hardest to avoid inviting: it changes replay behavior — skipping the handler for every occurrence, silently — for a case where that was never the actual answer. A suppression says "considered, and this is fine as written" without changing what the handler does.

## Why This Rule Exists

The two mechanisms look interchangeable from a distance — both are attributes, both are about replay — and choosing the wrong one fails exactly the way that is hardest to notice: nothing throws, no spec run against the live path catches it, and the difference only shows up the next time the observer actually replays, as either a duplicated side effect or a silently skipped one. Naming the decision at the call site — mark it, declare a `[Replay]` handler, or suppress with a reason — moves that failure from replay time to build time.

## Related Rules

- [ARCCHR0004](ARCCHR0004.md) — [EventType] repeats the type name as its id
