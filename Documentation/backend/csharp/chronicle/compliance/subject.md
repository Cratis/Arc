---
title: Subject
description: Set the compliance subject on a command so Chronicle encrypts PII under the right identity.
---

The `Subject` is Chronicle's compliance identity — the value used to key per-subject material such as PII encryption keys. When you append an event that contains `[PII]`-annotated properties, Chronicle encrypts those properties under the subject's key. For return-driven appends, selecting the correct subject on the command supplies the encryption identity for the returned events.

When no append subject is supplied, Chronicle first resolves event-level `[Subject]` metadata and ultimately falls back to the `EventSourceId`. For returned events, set a command subject when their compliance identity must differ from that event-level resolution or fallback — for example, when an _order_ event's PII belongs to the _customer_.

## Resolution Order

Before dependencies are loaded, `ICanProvideSubject` supplies the subject if implemented. Otherwise Arc looks for a property typed as `Subject` or marked `[Subject]` from `Cratis.Chronicle`, then a matching constructor-parameter declaration. Typed and attributed properties share a predicate: there is no typed-before-attributed priority. Declare one unambiguous subject.

After `Handle()` returns, a returned `Subject` overrides append metadata for return-driven events. That later value cannot change earlier dependency resolution. Neither stage infers the subject from the authenticated user.

## Aggregate Apply limitation

Aggregate `Apply()` does not forward the command-context subject in the current integration. Its events use Chronicle's event-level subject resolution and fallback. Setting or returning a subject on the command does not retag those events. See [defining an aggregate root](../aggregates/defining-an-aggregate-root.md) for the separate aggregate append path.

The return-driven examples below are command/type fragments using the application's `CustomerId` concept; they illustrate subject routing, not a complete PII model. Each explicitly selects the order's event-source id so a foreign customer reference cannot become the append target. The final attribute example assumes `CustomerId : ConceptAs<Guid>`, a compliance identity rather than an event-source identity.

## Returning Subject from Handle

The simplest approach when the subject is computed inside the handler:

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

Arc detects the `Subject` in the tuple response and passes it to `Append` automatically. The `Subject` is treated as append metadata, so it does not become the command response even when you also return another response value in the same tuple.

## ICanProvideSubject Interface

Use `ICanProvideSubject` when the subject is derived from command properties and you want an explicit, discoverable contract:

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

When a command property directly represents the compliance identity, mark it with `[Subject]` from `Cratis.Chronicle`. Arc reads the property value and converts it to a `Subject`:

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

Append metadata and read-model release have different sources of identity:

- Materialized reads can already be released by the server using stored subject metadata.
- Arc's query interceptor calls `Release(instance)`. The Chronicle client resolves the release subject from the read-model instance, not from the query caller.
- For command dependencies, Arc calls the non-generic `GetInstanceById(Type, key)`. It makes an additional `Release(instance)` call only when the input command context carries a subject and the instance exists. It does **not** pass that command subject to `Release`.

This distinction matters for passive reducers, which can construct state in-process rather than receiving an already-released materialized document. Do not assume every dependency path decrypts identically, or that returning a subject from `Handle()` can repair a dependency already loaded. Model the read model's subject explicitly and test materialized and passive paths separately. See [PII release behavior](./pii.md).

The release subject must identify the key used at encryption time. Setting a subject is not authorization: protect query access independently.
