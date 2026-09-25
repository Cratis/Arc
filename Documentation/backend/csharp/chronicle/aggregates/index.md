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
    public async Task<AggregateRootCommitResult> Handle(Account account)
    {
        await account.Withdraw(Amount);
        return await account.Commit();
    }
}
```

This command fragment assumes an `Account : AggregateRoot` with an asynchronous `Withdraw` method, plus imports for `Cratis.Arc.Chronicle.Aggregates`, `Cratis.Arc.Commands.ModelBound`, and `Cratis.Chronicle.Keys`. Arc loads its history for `AccountId` before `Handle()` runs. Returning `Commit()` propagates aggregate `Failed(...)` results, but commits the shared transaction at that point. Automatic completion is also supported; it does not collect those aggregate failures. See [commit boundaries and the current limitation](./defining-an-aggregate-root.md#automatic-completion-and-its-current-limitation).

## Which one do I reach for

| | Read model | Aggregate root |
| --- | --- | --- |
| Answers | "what does this look like now?" | "is this change allowed, and what happened?" |
| Built from | materialized events, or on-demand passive state | events, replayed on resolution |
| Consistency | depends on backing; a snapshot is not a concurrency lock | depends on captured revision and append-time enforcement |
| Can emit events | no | yes |
| Reach for it when | gating on projected state, computing inputs | an invariant must hold under concurrency |

They compose. Validate against a read model to give the user a fast, specific message, and let the aggregate enforce the invariant that actually must not break. See [Read models](../read-models/index.md).

## How Arc wires it up

The same key resolution that picks a read model picks the aggregate — a `[Key]` property, an `EventSourceId` or `EventSourceId<T>`-derived property, or `ICanProvideEventSourceId`. See [Resolving EventSourceId](../resolving-event-source-id.md).

- **Discovered automatically** — every type implementing `IAggregateRoot` is registered without configuration.
- **Resolved per command** — the instance is command-scoped and bound to that command's event source id, rehydrated from its stream on resolution.
- **Committed for you** — applied events are enrolled in the command's transaction and committed on success, rolled back on failure.

A command with no declared key receives a generated creation identity. A declared but unusable identity can fail with `UnableToResolveAggregateRootFromCommandContext`. Rehydration alone does not prove a concurrency guarantee: verify the aggregate revision and append behavior for the invariant you enforce.

## Topics

| Topic | Description |
| ----- | ----------- |
| [Defining an aggregate root](./defining-an-aggregate-root.md) | Writing the class itself — applying events, `On` methods, and how state is rebuilt. |
| [Aggregate roots in commands](./injecting-into-commands.md) | Taking one as a `Handle()` dependency, key resolution, and lifetime. |
