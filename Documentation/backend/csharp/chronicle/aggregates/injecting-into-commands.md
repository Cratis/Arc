---
title: Use aggregate roots in Arc commands
description: Inject an aggregate root into an Arc command handler, load it for the command's event source id, and return its commit result to report failures.
---

Declare an aggregate root as a `Handle()` parameter to make a command use that entity's state and decision methods. With the Chronicle integration registered, Arc discovers `IAggregateRoot` implementations as scoped services and calls `IAggregateRootFactory.Get<T>()` for the command's event source id. The factory replays the aggregate's handled events before your handler runs.

## Inject and await a mutation

This command adds an item using the `Order` implementation from [Defining an aggregate root](./defining-an-aggregate-root.md). The aggregate checks the quantity limit and applies the accepted event; the command returns the commit result:

```csharp
using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Keys;

[Command]
public record AddItemToOrder([Key] Guid OrderId, Guid ProductId, int Quantity)
{
    public async Task<AggregateRootCommitResult> Handle(Order order)
    {
        await order.AddItem(ProductId, Quantity);
        return await order.Commit();
    }
}
```

Await the mutation so its event is applied before committing. The aggregate has already enrolled that event for append, so do not return another copy from `Handle()`.

Return the result of `Commit()` because `Order` reports errors through `Failed(...)`; awaiting the commit and discarding its result can let a rejected change appear successful. Explicit commit completes the shared command unit of work, so return immediately afterward. Automatic completion does not collect private aggregate failure lists. Read [the commit limitation](./defining-an-aggregate-root.md#automatic-completion-and-its-current-limitation) before changing this to a `Task`-only handler.

## Which identity loads the aggregate

[Command identity resolution](../resolving-event-source-id.md) happens before dependency construction:

- `ICanProvideEventSourceId` takes precedence.
- Otherwise, use one unambiguous `EventSourceId`, `EventSourceId<T>`-derived, or Chronicle `[Key]` property.
- A keyless creation command receives a generated identity and can resolve a new aggregate with no history.
- A declared but unusable identity, such as `EventSourceId.Unspecified`, cannot resolve an aggregate and produces `UnableToResolveAggregateRootFromCommandContext`.

A tuple returned from `Handle()` supplies its identity after the aggregate has loaded. That identity can select the target of returned events, but cannot change the history already loaded or the target of events the aggregate applied. If a creation command needs an injected aggregate with a known id, generate or supply that id before resolution.

## Multiple aggregates

Automatic injection uses the command's identity for every aggregate type it resolves. Different types can therefore load for the same id; resolving the same type again within the command scope returns the same instance.

Use the factory explicitly when you need a second identity. This fragment loads a second account inside an asynchronous handler; it does not commit or implement a transfer:

```csharp
var destination = await aggregateRootFactory.Get<Account>(destinationId);
```

Here `aggregateRootFactory` is an `IAggregateRootFactory`, `destinationId` is an `EventSourceId`, and `Account` is your aggregate type. Loading it does not change the command context used by its injected read-model dependencies.

Each aggregate checks its own rules, but an explicit commit can complete their shared transaction. Collect failures from every participant before allowing that commit: one aggregate's `Commit()` does not check another's private `Failed(...)` list. Loading two aggregates is not, by itself, a protected decision over both.

## Lifetime and transaction

An injected instance is reused within the command scope after its initial rehydration. Applied events join the command's unit of work, the pending batch shared by the command's participants. The pipeline rolls back pending events if the command fails, or commits them if it succeeds, unless an explicit commit already completed the transaction.

This is why the command must return a failure it knows about before completion. The pipeline does not discover aggregate `Failed(...)` calls on its own, cannot undo an earlier explicit commit, and does not make external service calls transactional. See [Transactional commands](../commands/transactional-commands.md).

## What an aggregate guards

Arc captures the guarded scope's tail sequence number when it loads the aggregate, before `Handle()` runs. Every event the aggregate applies carries that expectation, allowing commit to reject a matching concurrent append. When the guarded scope is empty, it expects no matching history, so competing creations cannot both succeed. The [rehydration scope](./defining-an-aggregate-root.md#which-events-rehydration-reads) explains how an event source declaration and the handled event types determine the boundary.

That boundary does not automatically include other state the command reads. Two aggregates for two keys each guard their own source; an injected read model does not become part of either aggregate's replay state or guard. If a rule depends on that model, choose read protection for it rather than relying on injection alone.

Under read protection `On`, a protected model read of the same event source as the aggregate is refused with `ReadAndAggregateOnSameSource`. Decide from the aggregate's state, or mark an advisory read `[Unprotected("why")]` when it must not participate in the decision. A protected read of a different source can coexist with the aggregate and contributes its own guard. Outside `On`, aggregate `Commit()` refuses a unit of work with enrolled `[ProtectedDecision]` reads.

Aggregates have no snapshots: each command replays the key's handled events, so the work grows with that history. [Choosing how a command's rule holds](../consistency/index.md) compares this approach with constraints, read protection and stream decisions. [Rich aggregate](../consistency/scenarios/aggregate.md) walks through the combinations under `On`; [pitfalls](../consistency/pitfalls.md#an-aggregate-commit-whose-result-is-ignored) shows what happens when a handler discards a rejected commit result.

## Verify the boundary

Start with a spec that reconstructs history, applies another event, and checks the resulting state and exact append count. Then cover rejection through `Failed(...)`, including a failure after `Apply()`, and verify that the returned command result reports it. Test failure before and after explicit commit separately, because only pending writes can be rolled back.

Finally, run competing commands against the same source and verify which append is rejected. Replaying the right state proves state acquisition; the competing append checks whether the decision holds at commit.
