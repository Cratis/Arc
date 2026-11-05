---
title: Define an aggregate root in Arc
description: Define an Arc aggregate root that rebuilds state from events, rejects invalid changes, and returns its commit result from a command handler.
---

Define an aggregate root in Arc's Chronicle integration when an entity's rules are easier to express as methods over its event history. Derive from `Cratis.Arc.Chronicle.Aggregates.AggregateRoot`, rebuild state in event handlers, and call `Apply()` for each accepted change. Resolve the aggregate through command injection or `IAggregateRootFactory`; the factory supplies the context needed to replay and record events.

## State that survives replay

This example keeps an order's total quantity at or below 100. `AddItem` checks the requested change, while `OnItemAdded` updates the quantity for both replayed history and newly applied events. Keeping state changes in that handler means the next decision sees the same state whether the event was just applied or loaded from history.

```csharp
using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Chronicle.Events;
using Cratis.Concepts;

public record OrderId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly OrderId NotSet = new(Guid.Empty);

    public static OrderId New() => new(Guid.NewGuid());
    public static implicit operator OrderId(Guid value) => new(value);
}

public record ProductId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly ProductId NotSet = new(Guid.Empty);

    public static ProductId New() => new(Guid.NewGuid());
    public static implicit operator ProductId(Guid value) => new(value);
}

public record Quantity(int Value) : ConceptAs<int>(Value)
{
    public static readonly Quantity NotSet = new(0);

    public static implicit operator Quantity(int value) => new(value);
}

public class Order : AggregateRoot
{
    int _quantity;

    public async Task AddItem(ProductId productId, Quantity quantity)
    {
        if (quantity <= 0 || quantity > 100 - _quantity)
        {
            Failed("Quantity must be positive and the order total must not exceed 100.");
            return;
        }

        await Apply(new ItemAdded(productId, quantity));
    }

    public void OnItemAdded(ItemAdded @event) => _quantity += @event.Quantity;
}

/// <summary>
/// Records a product quantity added to an order.
/// </summary>
[EventType]
public record ItemAdded(ProductId ProductId, Quantity Quantity);
```

Keep each concept in its own file in the application. `OrderId` and `ProductId` distinguish the target order from the referenced product; `Quantity` carries the amount being added. The event keeps the product reference, while the order id lives in event context.

The quantity check assumes replayed state is already between 0 and 100. Subtracting the current quantity avoids overflowing when the input is a very large positive integer. This method rejects invalid new changes; it does not repair invalid history.

Always await `Apply()`: it enrolls the event for commit and runs the handler that updates local state. Do not also return the same `ItemAdded` from a command, because that declares another append. Keep event handlers free of notifications, external calls, and further appends; they run again when history is replayed.

Aggregate `Apply()` does not forward the command-context compliance subject. Its events use Chronicle's event-level subject resolution and fallback, even when the command sets or returns a subject. See the [aggregate subject limitation](../compliance/subject.md#aggregate-apply-limitation) before choosing the encryption identity.

## Declare the event source

An aggregate root that always works within one registered event source definition declares it with `[EventSource<TSource>]`, the same attribute commands use. The definition supplies the event source type, the optional stream name becomes the aggregate's event stream type, and every event the aggregate appends records the definition in `EventContext.EventSource`.

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.EventSources;

[EventSource]
[EventStream("transactions")]
public class Account : IEventSource;

[EventSource<Account>("transactions")]
public class AccountAggregate : AggregateRoot
{
    // handlers and commands as usual
}
```

Rehydration then reads the events of the declared event source type and stream type, as described in [Which events rehydration reads](#which-events-rehydration-reads).

Arc rejects an aggregate that names a stream its definition does not declare, a `Get<T>` call asking for a different event source type, or an `[EventStreamType]` that contradicts the declared stream. The legacy attributes keep working for aggregates that do not declare a definition.

## Which events rehydration reads

Rehydration means replaying handled events to rebuild the aggregate's state. Before reading them, Arc captures the tail sequence number of the scope it will guard. Applied events carry that expectation, and commit rejects a concurrent append that matches the guard. The declaration and whether the aggregate already exists determine which events are read and which concurrent writes cause a conflict.

| | Without `[EventSource<TSource>]` | With `[EventSource<TSource>]` |
| --- | --- | --- |
| Rehydration applies | every handled event for the event source id, in any stream | handled events of its own event source type, event stream type and event stream id |
| The commit rejects a concurrent append | to any stream for the event source id: handled event types for an existing aggregate, any event type for a new aggregate or one without handlers | to its event source type and event stream type, in the event stream id passed to `Get<T>` or else in any |
| Its own events are appended with | its event stream type, event stream id and event source type | the same, plus the declared event source |

Without a declaration, handled events appended anywhere for the id can shape the aggregate's state, including events written directly to the log or by another aggregate type. An existing aggregate with handlers guards those handled types across streams. A new aggregate keeps an any-event-type guard so competing creations cannot both succeed. Declare an event source when the aggregate owns one stream and other streams for the same id should neither shape its state nor block its commit.

## Event handler signatures

Method names are conventional, not the dispatch key. The first parameter identifies a registered event type. Explicitly registered event types work for both handlers and `Apply`, even without an `[EventType]` attribute. These are signature fragments, not method implementations:

```csharp
void On(TEvent @event)
Task On(TEvent @event)
void On(TEvent @event, EventContext context)
Task On(TEvent @event, EventContext context)
```

[ARCCHR0001](../code-analysis/ARCCHR0001.md) checks recognized handler candidates. A clean analyzer result is not proof that every intended handler was discovered; test replay and a subsequent mutation.

## Injected read models are dependencies, not aggregate replay state

Constructor injection can supply additional context through a registered read model, including an on-demand passive model. That model's resolver controls its key and state. Calling the aggregate factory with an explicit id does not change the command context used to resolve those dependencies.

An injected model does not update when this aggregate applies events. Keep the quantity used by `AddItem`, and any other state that must evolve with the aggregate, in its event handlers. [Read-model injection](../read-models/index.md) explains how to acquire separate model state; Chronicle's `IProjectionFor<T>` and `IReducerFor<T>` are projection and reducer interfaces, not a special aggregate-state API.

Acquiring a model also does not put it under the aggregate's concurrency guard. If the command must decide from that model, choose protection for the read explicitly. Under read protection `On`, a protected read of the same source as an aggregate is refused; see [what an aggregate guards](./injecting-into-commands.md#what-an-aggregate-guards) before combining them.

## Reporting failure and committing

Call `Failed(message, severity)` when a change is not allowed, then stop that operation before applying its event, as `AddItem` does above. The call records a validation result; it does not throw or stop execution. `AggregateRoot.Commit()` checks the recorded results: Error severity prevents that commit, while Warning and Information results are included without blocking it.

Use the `Order` from a command and return its commit result so Arc can report those failures to the caller:

```csharp
using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record AddItemToOrder(OrderId OrderId, ProductId ProductId, Quantity Quantity)
    : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => OrderId;

    public async Task<AggregateRootCommitResult> Handle(Order order)
    {
        await order.AddItem(ProductId, Quantity);
        return await order.Commit();
    }
}
```

Both ids derive from `EventSourceId<Guid>`, so `GetEventSourceId()` explicitly selects the order as the command target. Arc translates the returned result into the command outcome, including validation, constraint, concurrency, and append failures.

:::caution[Explicit commit is an early boundary]
Inside a command, `Commit()` commits the **shared command unit of work**, not just this aggregate. Previously enrolled events commit at that point. A later command failure cannot undo them. Return immediately after committing and do not treat subsequent work as part of that atomic batch.
:::

## Automatic completion and its current limitation

The command pipeline can commit enrolled aggregate events automatically when the command result succeeds. That works when the command's own result carries every rejection decision. Automatic completion commits the unit of work directly; it does not call each aggregate's `Commit()` or collect its private `Failed(...)` results.

Keep the explicit commit in this example rather than changing it to a `Task`-only handler. If an aggregate applies an event and later calls `Failed(...)`, automatic completion can still commit that event when the command appears successful. Either propagate failure through the command result before completion or return the aggregate's explicit commit result. With multiple aggregates, each failure must reach the command outcome: one aggregate's commit does not inspect another's private validation list.

Verify the decision boundary with a competing append, not just a replay test. The aggregate protects the scope described above, not every read or external dependency used by the command. Use append-time constraints or [exact concurrency scopes](../commands/events.md#events-with-exact-concurrency-scopes) when the rule needs a different boundary.

## Using the factory outside a command

This service fragment uses the same `Order` and returns its result to its caller:

```csharp
using Cratis.Arc.Chronicle.Aggregates;

public class OrderService(IAggregateRootFactory aggregates)
{
    public async Task<AggregateRootCommitResult> AddItem(
        OrderId orderId, ProductId productId, Quantity quantity)
    {
        var order = await aggregates.Get<Order>(orderId);
        await order.AddItem(productId, quantity);
        return await order.Commit();
    }
}
```

The caller must inspect the result and manage the surrounding lifetime. This fragment is not an HTTP endpoint and does not supply Arc command validation or authorization by itself.

To use the aggregate as a `Handle()` dependency, continue with [Aggregate roots in commands](./injecting-into-commands.md). For how its pending events share a transaction with other command work, read [Transactional commands](../commands/transactional-commands.md).
