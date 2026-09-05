---
title: Aggregates
description: Use an aggregate root when a command's decision must hold against the entity's own history — Arc resolves it from the command's key and commits what it applies.
---

Most commands can decide from what they carry plus a projected read model. Some can't. "Withdraw 200" has to be checked against *this* account's actual history, and it has to stay correct when two withdrawals arrive at once — a read model that lags by a few milliseconds will happily approve both.

That is what an aggregate root is for. It rehydrates from the entity's own event stream, applies new events under its own rules, and commits them as one unit. Where a read model is a *snapshot you read*, an aggregate root is *the thing that decides and records*.

## The difference in one line

```csharp
[Command]
public record WithdrawFunds([Key] Guid AccountId, decimal Amount)
{
    public Task Handle(Account account) => account.Withdraw(Amount);
}
```

`Account` is an aggregate root. Arc resolved it for `AccountId` and replayed its events to rebuild current state before `Handle()` ran. Whatever the aggregate applies is enrolled in the command's transaction and committed when the command succeeds — or rolled back when it fails. You never fetch it, never call `Commit()`, and never touch the event log.

## Which one do I reach for

| | Read model | Aggregate root |
|---|---|---|
| Answers | "what does this look like now?" | "is this change allowed, and what happened?" |
| Built from | events, materialized to a sink | events, replayed per command |
| Consistency | eventual | consistent within the aggregate boundary |
| Can emit events | no | yes |
| Reach for it when | gating on projected state, computing inputs | an invariant must hold under concurrency |

They compose. Validate against a read model to give the user a fast, specific message, and let the aggregate enforce the invariant that actually must not break. See [Read models](../read-models/index.md).

## How Arc wires it up

The same key resolution that picks a read model picks the aggregate — a `[Key]` property, a property that converts to `EventSourceId`, or `ICanProvideEventSourceId`. See [Resolving EventSourceId](../resolving-event-source-id.md).

- **Discovered automatically** — every type implementing `IAggregateRoot` is registered without configuration.
- **Resolved per command** — the instance is command-scoped and bound to that command's event source id, rehydrated from its stream on resolution.
- **Committed for you** — applied events are enrolled in the command's transaction and committed on success, rolled back on failure.

If the command carries no usable key, resolution fails with `UnableToResolveAggregateRootFromCommandContext`.

## Topics

| Topic | Description |
| ----- | ----------- |
| [Defining an aggregate root](./defining-an-aggregate-root.md) | Writing the class itself — applying events, `On` methods, and how state is rebuilt. |
| [Aggregate roots in commands](./injecting-into-commands.md) | Taking one as a `Handle()` dependency, key resolution, and lifetime. |
