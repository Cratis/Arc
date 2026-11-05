---
title: Set Subject on Chronicle command events
description: Set a Chronicle command's compliance subject when returned events belong to a different identity, and understand when a returned Subject takes effect.
---

Set a command's `Subject` when returned events need a compliance identity other than their event source id. In Cratis Chronicle, the subject identifies whose key protects personally identifiable information (PII). An order can keep the order's event source id while its customer's subject supplies the encryption identity. Cratis Arc passes the subject as append metadata; you still annotate personal data on event properties or shared concepts.

Without a command subject, Chronicle checks event-level subject metadata before falling back to the event source id. [Subject resolution](../compliance/subject.md) explains that precedence. Read-model decryption is separate: `Release(instance)` resolves its subject from the instance, not from the command.

Aggregate `Apply()` also follows a separate path and does not forward the command-context subject. Setting or returning a command subject does not retag aggregate events; they use event-level subject resolution and fallback. See the [aggregate limitation](../compliance/subject.md#aggregate-apply-limitation) if your command writes through an aggregate.

The examples below show subject routing with application domain types. They are not complete PII encryption examples.

## Set Subject on the command

Choose a command-level subject when the compliance identity is known before handling. Implement `ICanProvideSubject` to derive it from the command's properties. In this order example, `GetEventSourceId()` selects the order's history and `GetSubject()` selects the customer:

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

Selecting the order explicitly avoids ambiguity if `CustomerId` also derives from an event source id. The event payload keeps the customer reference; the order's own identity is in the event context.

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

Return `Subject` alongside the event when the handler decides the compliance identity. The returned subject overrides the command's earlier subject for return-driven appends, regardless of tuple order.

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

The `Subject` value is append metadata, not the caller's response. It arrives after validators, `Provide()`, and handler dependencies have been resolved, so it cannot change data already released to those dependencies or retag events already enrolled. Set the subject on the command when it must be available earlier.

## When to use this page

Once the command routes events under the right subject, use the [compliance subject reference](../compliance/subject.md) to check event-level fallback and read-model release behavior. Setting an append subject alone does not model or annotate the personal data in those events.
