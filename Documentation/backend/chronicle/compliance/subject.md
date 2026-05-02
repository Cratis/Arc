---
uid: Arc.Chronicle.Commands.Subject
---
# Subject

The `Subject` is Chronicle's compliance identity — the value used to key per-subject material such as PII encryption keys. When you append an event that contains `[PII]`-annotated properties, Chronicle encrypts those properties under the subject's key. Selecting the correct subject on the command ensures that events land under the right encryption key so they can be decrypted later.

When no explicit subject is supplied, Chronicle defaults to the `EventSourceId`. Setting a subject on the command is only necessary when the compliance identity differs from the aggregate identity — for example, when a command mutates an _order_ aggregate but the PII belongs to the _customer_.

## Resolution Order

Arc resolves the subject from the command in this order:

1. **Return a `Subject`** directly from `Handle()`.
2. **Implement `ICanProvideSubject`** on the command record and return the subject from `GetSubject()`.
3. **Decorate a property with `[Subject]`** from `Cratis.Chronicle` — Arc reads its value and converts it to a `Subject`.

If none of these are present, no subject is passed to Chronicle and it falls back to using the `EventSourceId`.

## Returning Subject from Handle

The simplest approach when the subject is computed inside the handler:

```csharp
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record PlaceOrder(EventSourceId OrderId, CustomerId CustomerId, decimal Amount)
{
    public (OrderPlaced, Subject) Handle() =>
        (new OrderPlaced(OrderId, CustomerId, Amount), new Subject(CustomerId.Value.ToString()));
}

[EventType]
public record OrderPlaced(EventSourceId OrderId, CustomerId CustomerId, decimal Amount);
```

Arc detects the `Subject` in the tuple response and passes it to `Append` automatically.

## ICanProvideSubject Interface

Use `ICanProvideSubject` when the subject is derived from command properties and you want an explicit, discoverable contract:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record PlaceOrder(EventSourceId OrderId, CustomerId CustomerId, decimal Amount)
    : ICanProvideSubject
{
    public Subject GetSubject() => new(CustomerId.Value.ToString());

    public OrderPlaced Handle() => new(OrderId, CustomerId, Amount);
}
```

## [Subject] Attribute on a Property

When a command property directly represents the compliance identity, mark it with `[Subject]` from `Cratis.Chronicle`. Arc reads the property value and converts it to a `Subject`:

```csharp
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record PlaceOrder(EventSourceId OrderId, [Subject] CustomerId CustomerId, decimal Amount)
{
    public OrderPlaced Handle() => new(OrderId, CustomerId, Amount);
}
```

If the property is already of type `Subject`, it is used directly. Any other type is converted via its `ToString()` representation.

## Relationship to PII Decryption

Chronicle uses the subject as the lookup key for PII encryption keys. When a read model contains `[PII]`-annotated properties, Arc's [PII](./pii.md) interceptor calls `Release()` with the subject to decrypt those values before they are served to the client. The subject set here at append time must therefore match the subject used on the read model's `[Subject]`-marked property.
