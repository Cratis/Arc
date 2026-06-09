---
uid: Arc.Chronicle.Commands.Subject
---
# Setting Subject on commands

Use `Subject` on a Chronicle command when the compliance identity for appended events is different from the event source id. Chronicle passes the resolved subject to the EventStore when it appends events automatically, and Arc uses the same resolved subject when it releases dependent read models injected into the command handler or validator.

If you do not provide a subject, Chronicle does not send one explicitly and the EventStore falls back to its normal event source id behavior.

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
    : ICanProvideSubject
{
    public Subject GetSubject() => new(CustomerId.Value.ToString());

    public OrderPlaced Handle() => new(OrderId, CustomerId, Amount);
}

[EventType]
public record OrderPlaced(EventSourceId OrderId, CustomerId CustomerId, decimal Amount);
```

Use a `Subject` property directly when the command already has the final compliance identity:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record ImportCustomer(EventSourceId CustomerId, Subject Subject, string Email)
{
    public CustomerImported Handle() => new(CustomerId, Email);
}

[EventType]
public record CustomerImported(EventSourceId CustomerId, string Email);
```

Use `[Subject]` when the source value is not already a `Subject`:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record RegisterCustomer(EventSourceId CustomerId, [Subject] Guid PersonId, string Email)
{
    public CustomerRegistered Handle() => new(CustomerId, Email);
}

[EventType]
public record CustomerRegistered(EventSourceId CustomerId, string Email);
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
            new DependentRegistered(HouseholdId, PersonId, Name),
            new Subject(PersonId.ToString())
        );
}

[EventType]
public record DependentRegistered(EventSourceId HouseholdId, Guid PersonId, string Name);
```

The `Subject` value is append metadata. Chronicle does not treat it as the command response.

## When to use this page

This page focuses on how to set subject values on command appends and command-side read model dependencies. For the compliance background and how subject affects PII encryption and decryption, see [Subject](../compliance/subject.md).
