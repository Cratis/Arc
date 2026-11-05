---
title: Cratis Arc compliance subject selection
description: Set a command subject in Cratis Arc to choose whose key encrypts returned events, then match that identity when releasing personal read-model values.
---

In Cratis Arc, a command's `Subject` tells Cratis Chronicle whose key to use for personally identifiable information (PII) in events returned by the handler. Choose it when the person the data belongs to differs from the event source: an order event may contain personal values belonging to a customer, not to the order itself.

For these return-driven appends, an explicit command subject supplies the encryption identity. Without an explicit append subject, Chronicle checks the event's `[Subject]` metadata, then falls back to the `EventSourceId`. Choose the identity before appending and use the same identity when releasing protected read-model values. Subject selection is not an authorization check or a guarantee of regulatory compliance.

## Resolution Order

Before loading command dependencies, Arc uses `ICanProvideSubject` if the command implements it. Otherwise it looks for a property typed as `Subject` or marked `[Subject]` from `Cratis.Chronicle`, then a matching constructor-parameter declaration. Typed and attributed properties have equal priority, so declare one unambiguous subject rather than relying on which property is found first.

After `Handle()` returns, a returned `Subject` overrides the subject used to append returned events. It cannot change dependencies that were already loaded. Neither stage infers the subject from the authenticated user.

## Aggregate Apply limitation

Aggregate `Apply()` does not forward the command-context subject. Its events use Chronicle's event-level subject resolution and fallback. Setting or returning a subject on the command does not change those events' subject. Use event-level metadata when needed, and see [defining an aggregate root](../aggregates/defining-an-aggregate-root.md) for that separate append path.

The examples below show three ways to supply a subject for returned events. They are command/type fragments using the application's `CustomerId` concept, not a complete PII model. Each explicitly selects the order's event-source id so the customer reference cannot become the append target. The attribute example assumes `CustomerId : ConceptAs<Guid>`, a compliance identity rather than an event-source identity.

## Returning Subject from Handle

Return a `Subject` alongside the event when the handler computes whose data it contains:

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record PlaceOrder(EventSourceId OrderId, CustomerId CustomerId, decimal Amount)
    : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => OrderId;

    public (OrderPlaced, Subject) Handle() =>
        (new OrderPlaced(CustomerId, Amount), new Subject(CustomerId.Value.ToString()));
}

/// <summary>
/// Records the customer and amount of a placed order.
/// </summary>
[EventType]
public record OrderPlaced(CustomerId CustomerId, decimal Amount);
```

Arc detects the `Subject` in the tuple response and supplies it as append metadata. It does not become the command response, even when the tuple contains another response value. Here, `OrderId` selects the event source while `CustomerId` selects the encryption subject.

## ICanProvideSubject Interface

Implement `ICanProvideSubject` when command properties already identify the subject. Arc can read that identity before invoking the handler:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record PlaceOrder(EventSourceId OrderId, CustomerId CustomerId, decimal Amount)
    : ICanProvideEventSourceId, ICanProvideSubject
{
    public EventSourceId GetEventSourceId() => OrderId;
    public Subject GetSubject() => new(CustomerId.Value.ToString());

    public OrderPlaced Handle() => new(CustomerId, Amount);
}
```

## [Subject] Attribute on a Property

Mark a command property with `[Subject]` from `Cratis.Chronicle` when it directly represents the compliance identity. Arc reads its value and converts it to a `Subject`:

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record PlaceOrder(EventSourceId OrderId, [Subject] CustomerId CustomerId, decimal Amount)
    : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => OrderId;

    public OrderPlaced Handle() => new(CustomerId, Amount);
}
```

If the property is already of type `Subject`, Arc uses it directly even without `[Subject]`. Any other `[Subject]`-annotated type is converted via its `ToString()` representation.

## Relationship to PII Decryption

A command subject chooses the key for new events. It does not choose whose existing read-model values a query or command dependency can decrypt.

For queries, Arc's interceptor calls `Release(instance)`. The Chronicle client resolves the subject from the read-model instance, not the query caller. Materialized reads may already have been released by the server using stored subject metadata.

For stored command dependencies, Arc calls the non-generic `GetInstanceById(Type, key)`, then requests an additional `Release(instance)` whenever an instance exists. This happens whether or not the command declares a subject. Arc does not pass the command subject to `Release`; Chronicle resolves identity from the read model.

A passive reducer can construct state from events in-process instead of receiving a materialized document that has already been released. Model the read model's subject explicitly and verify materialized and passive paths separately. Returning a subject from `Handle()` cannot repair a dependency loaded earlier.

Continue with [PII release behavior](./pii.md) to handle erased values and release failures. Keep query authorization independent: a matching subject identifies a key, not permission to read the data.
