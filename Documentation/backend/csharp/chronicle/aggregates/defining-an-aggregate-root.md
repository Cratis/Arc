---
title: Defining an aggregate root
description: Rebuild aggregate state with event handlers, apply new facts, and choose the correct commit boundary.
---

An aggregate makes a decision from an entity's event history. In Arc's optional Chronicle integration, derive from `Cratis.Arc.Chronicle.Aggregates.AggregateRoot`, apply facts with `Apply`, and rebuild state in event handlers. The factory supplies the mutation context; do not construct an aggregate yourself for production use.

## State that survives replay

This complete aggregate/type example records order quantities. `OnItemAdded` handles both replayed history and newly applied events, so the next decision sees the updated quantity.

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

Keep each concept in its own file in the application. `OrderId` and `ProductId` distinguish the target order from the referenced product; `Quantity` carries the amount being added. The event keeps the foreign product reference, while the order id lives in event context.

The bound assumes valid replayed state between 0 and 100; subtracting the current quantity avoids overflowing when the input is a very large positive integer.

`Apply` is asynchronous: always await it. Do not also return the same `ItemAdded` from a command; that would declare another append. Event handlers rebuild internal state only. Do not send notifications, call external systems, or append more events from a replay handler.

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

An aggregate root appends to one scope: its event source id, event source type, event stream type and event stream id. Its commit checks for concurrent appends in that same scope, and rehydration reads the handled events of that scope only, so every event that shapes the aggregate's state is also one the commit protects.

| Dimension | Without `[EventSource<TSource>]` | With `[EventSource<TSource>]` |
| --- | --- | --- |
| Event stream type | `[EventStreamType]`, or the aggregate's type name | the declared stream, `[EventStreamType]`, or the aggregate's type name |
| Event source type | the one passed to `Get<T>`, otherwise any | the definition's |
| Event stream id | the one passed to `Get<T>`, otherwise any | the one passed to `Get<T>`, otherwise any |

Chronicle treats the default event source type, the default event stream id and the `All` event stream type as "any", for the read and the commit check alike.

An event appended for the same event source id outside that scope does not change the aggregate's state. That includes an event appended without an event stream type, for example straight to the event log, because Chronicle stores it under `All` rather than the aggregate's stream type. Append the events an aggregate decides on through the aggregate itself, or with its event stream type.

## Event handler signatures

Method names are conventional, not the dispatch key. The first parameter identifies a registered event type. These are signature fragments, not method implementations:

```csharp
void On(TEvent @event)
Task On(TEvent @event)
void On(TEvent @event, EventContext context)
Task On(TEvent @event, EventContext context)
```

[ARCCHR0001](../code-analysis/ARCCHR0001.md) checks recognized handler candidates. A clean analyzer result is not proof that every intended handler was discovered; test replay and a subsequent mutation.

## Injected read models are dependencies, not aggregate replay state

Constructor injection can supply a registered read model, including an on-demand passive model. Its resolver controls its key and state. The aggregate factory's explicit id does not automatically become a new command context for those dependencies.

An injected model does not continually update when this aggregate applies events. Use aggregate handlers for state that must evolve during replay and mutation. For ordinary snapshot context, use [read-model injection](../read-models/index.md). Chronicle's projection and reducer interfaces are `IProjectionFor<T>` and `IReducerFor<T>`; they are not a special aggregate-state API.

## Reporting failure and committing

`Failed(message, severity)` accumulates aggregate validation results. `AggregateRoot.Commit()` checks them: an Error prevents that commit; Warning and Information results are included without blocking it.

For the `Order` above, return the commit result so Arc can propagate these failures:

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

The command pipeline can commit enrolled aggregate events automatically when the command result succeeds. That is useful when the command's own result carries all rejection decisions. However, automatic completion commits the unit of work directly: it does **not** call each aggregate's `Commit()` or collect its private `Failed(...)` results.

Do not replace the explicit commit in this example with a `Task`-only handler. In particular, an aggregate that applies an event and later calls `Failed(...)` can leave enrolled events while the command still appears successful. Propagate failure through the command result before completion, or retain the explicit commit/result path above and accept its boundary. Multi-aggregate failure aggregation needs particular care; one aggregate's commit does not inspect another aggregate's private validation list.

Rehydration alone also does not prove a concurrency invariant. Test the revision captured by the aggregate path and the competing append behavior for your client/server versions; use append-time constraints or [exact concurrency scopes](../commands/events.md#events-with-exact-concurrency-scopes) when appropriate.

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

For the full boundary, see [Transactional commands](../commands/transactional-commands.md); for injection and identity timing, see [Aggregate roots in commands](./injecting-into-commands.md).
