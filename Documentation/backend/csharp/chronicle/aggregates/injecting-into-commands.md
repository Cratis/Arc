---
title: Aggregate roots in commands
description: Resolve aggregates by the command's input identity, await mutations, and choose an explicit or automatic commit boundary.
---

With the Chronicle integration registered, Arc discovers `IAggregateRoot` implementations and registers them as command-scoped services. Declare one as a `Handle()` parameter: Arc reads the command-context event source id and calls `IAggregateRootFactory.Get<T>()` to rehydrate it before invoking your handler.

## Inject and await a mutation

This command fragment uses the complete `Order` implementation from [Defining an aggregate root](./defining-an-aggregate-root.md):

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

The aggregate applies its own events. Do not return copies of those events from `Handle()`, and do not ignore a mutation's `Task`.

This example explicitly commits because `Order` reports errors through `Failed(...)`. It returns immediately afterward: explicit commit finalizes the shared command unit of work. Automatic completion does not collect private aggregate failure lists. Read [the commit limitation](./defining-an-aggregate-root.md#automatic-completion-and-its-current-limitation) before changing this to a `Task`-only handler.

## Which identity loads the aggregate

[Command identity resolution](../resolving-event-source-id.md) happens before dependency construction:

- `ICanProvideEventSourceId` takes precedence.
- Otherwise, use one unambiguous `EventSourceId`, `EventSourceId<T>`-derived, or Chronicle `[Key]` property.
- A keyless creation command receives a generated identity and can resolve a new aggregate with no history.
- A declared but unusable identity, such as `EventSourceId.Unspecified`, cannot resolve an aggregate and produces `UnableToResolveAggregateRootFromCommandContext`.

A returned tuple identity arrives **after** the aggregate has loaded. It can select the target of returned events, but cannot retarget that aggregate's history or enrolled events. Generate or supply the identity before resolution when a creation command needs an injected aggregate and a known id.

## Multiple aggregates

Automatic injection selects one identity, not one CLR type. Different aggregate types can each resolve under that identity. Repeated resolution of the same type in a command shares its scoped instance.

For a second identity, use the factory explicitly. This is a lookup fragment inside an asynchronous handler; it does not commit or define a transfer operation:

```csharp
var destination = await aggregateRootFactory.Get<Account>(destinationId);
```

Here `aggregateRootFactory` is an `IAggregateRootFactory`, `destinationId` is an `EventSourceId`, and `Account` is your aggregate type. Factory loading does not replace the command context used by an aggregate's injected read-model dependencies. Make cross-source transaction and failure handling explicit rather than assuming multiple objects imply one validated aggregate boundary.

## Lifetime and transaction

An injected instance is resolved once per command scope and rehydrated on resolution. Applied events enroll in the command's unit of work. If the pipeline sees a failed command before commit, it rolls back pending events. If the command succeeds, it completes the pending transaction unless an explicit commit already completed it.

This lifetime does not give aggregate `Failed(...)` calls automatic propagation, undo an earlier manual commit, or make external service calls transactional. See [Transactional commands](../commands/transactional-commands.md).

## Verify the boundary

A useful aggregate spec reconstructs history, applies another event, and checks the resulting state and exact append count. Add cases for `Apply` followed by `Failed`, failure before and after explicit commit, and competing commands. A build proves signatures, not those behaviors.
