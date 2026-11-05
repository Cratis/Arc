---
title: Arc aggregate roots for command decisions
description: Use an Arc aggregate root to decide from one entity's event history, apply events, and return validation and concurrency failures to the caller.
---

An aggregate root keeps an entity's decision rules together with the state rebuilt from its event history. Use one in Arc's Chronicle integration when methods such as `Withdraw` or `AddItem` are a useful way to express which changes the entity allows.

Loading state and protecting a decision are separate concerns. Replaying an account's events gives a withdrawal the balance to check; an append-time concurrency check rejects its write if relevant events arrived after loading. An aggregate captures that expectation when it loads and attaches it to the events it applies. A plain read-model snapshot does not supply that protection, but a read model used with [read protection](../consistency/index.md) can also support a protected decision. Choose an aggregate for how it organizes the rules, not because it is the only way to enforce them.

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

This command fragment assumes an `Account : AggregateRoot` with an asynchronous `Withdraw` method, plus imports for `Cratis.Arc.Chronicle.Aggregates`, `Cratis.Arc.Commands.ModelBound`, and `Cratis.Chronicle.Keys`. Arc rebuilds the account's state for `AccountId` before `Handle()` runs. The method checks its rules and applies events; returning `Commit()` lets Arc report validation and append failures to the caller.

Explicit commit completes the shared command transaction, so return it last. Automatic completion can commit applied events too, but does not collect aggregate `Failed(...)` results. Keep the explicit result path when the aggregate reports failures that way; see [commit boundaries and the current limitation](./defining-an-aggregate-root.md#automatic-completion-and-its-current-limitation).

## Which one do I reach for

| Concern | Read model | Aggregate root |
| --- | --- | --- |
| State acquisition | Materialized projection or on-demand computation | Replay of handled events on resolution |
| Where rules live | Validators or command logic using the model | Methods on the aggregate |
| Decision protection | A plain snapshot is not protected; read protection captures a guard for supported reads | Loading captures a guard for the aggregate's scope; its applied events carry that guard |
| Recording changes | The command returns or appends events | Aggregate methods call `Apply()` |

For a few checks over a flat model, a validator with read protection may be enough. An aggregate is useful when several rules and transitions belong to one entity's lifecycle. [Choosing how a command's rule holds](../consistency/index.md) compares these approaches with append-time constraints and stream decisions.

Read-model dependencies can provide additional context, but they do not become replay state or automatically inherit the aggregate's guard. Under read protection `On`, a protected read of the same source as the aggregate is refused. See [read-model injection](../read-models/index.md) for acquiring that context and [aggregate command boundaries](./injecting-into-commands.md#what-an-aggregate-guards) for the combination rules.

## How Arc wires it up

With the Chronicle integration registered, Arc discovers `IAggregateRoot` implementations and registers them as scoped services. A `Handle()` parameter resolves an instance for the command's event source id and rehydrates it: Arc replays the events into its handlers before calling your command.

The id comes from `ICanProvideEventSourceId`, an unambiguous `EventSourceId` or `EventSourceId<T>`-derived property, or a Chronicle `[Key]` property. A command with no declared key receives a generated creation identity. A declared but unusable identity can fail with `UnableToResolveAggregateRootFromCommandContext`. Follow [Resolving EventSourceId](../resolving-event-source-id.md) when the command carries more than one candidate id.

Applied events join the command's unit of work, the pending batch shared by its participants. The pipeline commits that batch on success or rolls it back on failure unless an explicit commit already completed it. Returning the aggregate's commit result is how the examples here make its validation failures part of that command outcome.

## Topics

| Topic | Description |
| ----- | ----------- |
| [Defining an aggregate root](./defining-an-aggregate-root.md) | Build an order aggregate, replay its state, and report rejected changes. |
| [Aggregate roots in commands](./injecting-into-commands.md) | Use the aggregate from `Handle()` and choose its identity and commit boundary. |
