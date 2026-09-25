---
title: Setting Subject on commands
description: Tell Chronicle which compliance identity a command writes under, so PII is encrypted under the right key.
---

Use `Subject` on a Chronicle command when the compliance identity for returned events is different from the event source id. Arc passes the resolved subject as metadata for return-driven appends. This is separate from read-model decryption: `Release(instance)` resolves its subject from the instance, not from the command's subject.

If you do not provide a command subject, Chronicle consults event-level subject metadata before the final event-source-id fallback. See the [subject resolution reference](../compliance/subject.md) for precedence and dependency timing.

Aggregate `Apply()` does not forward the command-context subject in the current integration. Its events use Chronicle's event-level subject resolution and fallback. Setting or returning a subject on the command does not retag those events. See the [aggregate limitation](../compliance/subject.md#aggregate-apply-limitation).

The examples are routing fragments with application domain types, not complete PII encryption examples. Annotate event properties or shared concepts independently when they contain personal data.

## Set Subject on the command

When the subject is already part of the command, put it on the record itself.

Implement `ICanProvideSubject` when you want the subject to be computed:

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

/// <summary>
/// Records the customer and amount of a placed order.
/// </summary>
[EventType]
public record OrderPlaced(CustomerId CustomerId, decimal Amount);
```

`GetEventSourceId()` selects the order explicitly, even if the application's `CustomerId` also derives from an event-source id. The event keeps that foreign customer reference; the order's own id is in event context.

Use a `Subject` property directly when the command already has the final compliance identity:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record ImportCustomer(EventSourceId CustomerId, Subject Subject, string Email)
{
    public CustomerImported Handle() => new(Email);
}

/// <summary>
/// Records the email address of an imported customer.
/// </summary>
[EventType]
public record CustomerImported(string Email);
```

Use `[Subject]` when the source value is not already a `Subject`:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record RegisterCustomer(EventSourceId CustomerId, [Subject] Guid PersonId, string Email)
{
    public CustomerRegistered Handle() => new(Email);
}

/// <summary>
/// Records the email address supplied when a customer registered.
/// </summary>
[EventType]
public record CustomerRegistered(string Email);
```

Chronicle converts the `[Subject]` value to `Subject` by calling `ToString()`.

## Override Subject from Handle()

Return `Subject` in the tuple from `Handle()` when the subject is decided inside the handler. A returned subject overrides any subject that was resolved from the command itself.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record RegisterDependent(EventSourceId HouseholdId, Guid PersonId, string Name)
{
    public (DependentRegistered, Subject) Handle() =>
        (
            new DependentRegistered(PersonId, Name),
            new Subject(PersonId.ToString())
        );
}

/// <summary>
/// Records the person registered as a household dependent.
/// </summary>
[EventType]
public record DependentRegistered(Guid PersonId, string Name);
```

The `Subject` value is append metadata. Chronicle does not treat it as the command response. A returned subject arrives after validators, `Provide()`, and handler dependencies have been resolved; it cannot change their earlier release or retag already-enrolled events.

## When to use this page

This page focuses on setting subject values on command appends. For the compliance background and how subject affects PII encryption and decryption, see [Subject](../compliance/subject.md).
