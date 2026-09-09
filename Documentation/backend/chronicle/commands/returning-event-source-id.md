---
title: Returning EventSourceId from a command
description: Hand the generated identity back to the caller when the command is what creates the entity.
---

When a command creates a customer and chooses the identity in `Handle()`, return a `CustomerId` alongside the event. Derive it from `EventSourceId<Guid>` so it names the domain entity and tells Chronicle where to append. The caller receives the same identity for its next query or command.

## Return EventSourceId together with the event

When `Handle()` returns a tuple containing an event and an `EventSourceId` or `EventSourceId<T>`-derived value, Chronicle uses the returned id for the automatic append. Use shared domain concepts for the identity and customer details:

```csharp
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Concepts;

public record CustomerId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly CustomerId NotSet = new(Guid.Empty);
    public static CustomerId New() => new(Guid.NewGuid());
    public static implicit operator CustomerId(Guid value) => new(value);
}

public record CustomerEmail(string Value) : ConceptAs<string>(Value)
{
    public static readonly CustomerEmail NotSet = new(string.Empty);
    public static implicit operator CustomerEmail(string value) => new(value);
}

public record CustomerDisplayName(string Value) : ConceptAs<string>(Value)
{
    public static readonly CustomerDisplayName NotSet = new(string.Empty);
    public static implicit operator CustomerDisplayName(string value) => new(value);
}

[Command]
public record RegisterCustomer(CustomerEmail Email, CustomerDisplayName DisplayName)
{
    public (CustomerId, CustomerRegistered) Handle()
    {
        var customerId = CustomerId.New();

        return (customerId, new CustomerRegistered(Email, DisplayName));
    }
}

/// <summary>
/// Records a customer's registration with their email and display name.
/// </summary>
[EventType]
public record CustomerRegistered(CustomerEmail Email, CustomerDisplayName DisplayName);
```

The tuple order does not matter. This alternative `Handle()` on the same command returns the event first:

```csharp
public (CustomerRegistered, CustomerId) Handle()
{
    var customerId = CustomerId.New();

    return (new CustomerRegistered(Email, DisplayName), customerId);
}
```

Chronicle recognizes the identity in either position; the event does not need to carry it in its payload.

## Use typed ids

Reuse `CustomerId` in subsequent commands and read models so unrelated identities cannot be swapped accidentally. `CustomerEmail` and `CustomerDisplayName` likewise keep two different domain values distinct in C#. These concepts retain simple underlying wire values; add concept validators when their invariants should follow them through validation.

Bare `EventSourceId` remains supported by the API, but `CustomerId : EventSourceId<Guid>` gives that framework meaning a domain name. Return the domain type rather than converting it to a raw `Guid`.

## A raw Guid is only a response

Do not return `(Guid, event)` when the `Guid` is meant to identify the new event source:

```csharp
[Command]
public record RegisterCustomer(CustomerEmail Email, CustomerDisplayName DisplayName)
{
    // ARCCHR0010: the response and persisted event source receive different ids
    public (Guid, CustomerRegistered) Handle() =>
        (Guid.NewGuid(), new CustomerRegistered(Email, DisplayName));
}
```

A raw `Guid` is an ordinary command response. It does not carry event-source semantics, even though `Guid` can be converted to `EventSourceId` when a command property is explicitly selected as the key. Because this command declares no key, Chronicle creates a fallback event source id before `Handle()` runs and appends `CustomerRegistered` under that fallback—not under the returned `Guid`.

[ARCCHR0010](../code-analysis/ARCCHR0010.md) heuristically reports supported signatures and offers a bounded quick fix to `EventSourceId<Guid>` when the edit compiles. Prefer a domain identity such as `CustomerId` when this value really identifies the event source.

An unrelated confirmation Guid with a generated fallback event source is valid. Do not change persistence routing just to silence the warning; document that intent and suppress ARCCHR0010 narrowly as shown in its reference.

## Important behavior

- Chronicle treats the returned `EventSourceId` as append metadata and as the command response value.
- If the command record already had an event source id, the returned value wins for return-driven appends. It does not reload earlier dependencies, retarget an injected aggregate, or move already-enrolled events.
- If you need to target several different event sources from one command, use [Events](./events.md#events-for-specific-event-sources) and return `EventForEventSourceId` values instead.
