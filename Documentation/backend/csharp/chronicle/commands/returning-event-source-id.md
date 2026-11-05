---
title: Return EventSourceId from Chronicle commands
description: Return a typed EventSourceId with a command event so Chronicle appends under the new entity's id and the caller receives that same identity.
---

Return a typed event source id alongside the creation event when a command chooses a new entity's identity in `Handle()`. Cratis Chronicle uses that id for the returned event, and Cratis Arc sends the same identity to the caller for its next query or command. For a customer, `CustomerId : EventSourceId<Guid>` gives the value both a domain name and append-routing meaning.

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

Chronicle recognizes the identity in either position. The event does not need to carry its own source id in its payload; that identity is stored in the event context.

## Use typed ids

Reuse `CustomerId` in subsequent commands and read models to keep unrelated identities distinct in C#. `CustomerEmail` and `CustomerDisplayName` serve the same purpose for the two customer details. These concepts retain simple underlying wire values; add concept validators when the values also have rules to enforce.

Bare `EventSourceId` remains supported by the API, but `CustomerId : EventSourceId<Guid>` gives that framework meaning a domain name. Return the domain type rather than converting it to a raw `Guid`.

## A raw Guid is only a response

:::caution[Do not use a raw Guid as the new event source id]
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
:::

[ARCCHR0010](../code-analysis/ARCCHR0010.md) heuristically reports supported signatures and offers a bounded quick fix to `EventSourceId<Guid>` when the edit compiles. Prefer a domain identity such as `CustomerId` when this value really identifies the event source.

An unrelated confirmation Guid with a generated fallback event source is valid. Do not change persistence routing just to silence the warning; document that intent and suppress ARCCHR0010 narrowly as shown in its reference.

## Important behavior

The returned `EventSourceId` serves as both append metadata and the command response. It wins over an id resolved from the command record for return-driven appends, but it arrives after dependencies have been loaded. Returning a different id does not reload those dependencies, retarget an injected aggregate, or move events already enrolled in the transaction.

`CustomerId.New()` in this example chooses an identity inside the handler. That is not an Arc `[GeneratedValue]` capture and does not automatically require the source to be empty. Use a [guarded generated parameter](./fresh-identifiers.md) when a create command must reject an occupied destination rather than extend its history.

For one command that writes to several sources, return [`EventForEventSourceId` values](./events.md#events-for-specific-event-sources) instead of trying to select every destination with one returned id.
