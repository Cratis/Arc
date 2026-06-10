---
uid: Arc.Chronicle.Commands.ReturningEventSourceId
---
# Returning EventSourceId from a command

Return `EventSourceId` from `Handle()` when the command decides which event source to append to at runtime. This is the pattern to use when the command does not already carry the final identity as part of its input.

## Return EventSourceId together with the event

When `Handle()` returns a tuple that contains both an event and an `EventSourceId`, Chronicle uses the returned id for the automatic append.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record RegisterCustomer(string Email, string DisplayName)
{
    public (EventSourceId, CustomerRegistered) Handle()
    {
        var customerId = EventSourceId.New();
        return (customerId, new CustomerRegistered(Email, DisplayName));
    }
}

[EventType]
public record CustomerRegistered(string Email, string DisplayName);
```

The tuple order does not matter. Chronicle looks for the `EventSourceId` value anywhere in the tuple.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record RegisterCustomer(string Email, string DisplayName)
{
    public (CustomerRegistered, EventSourceId) Handle()
    {
        var customerId = EventSourceId.New();
        return (new CustomerRegistered(Email, DisplayName), customerId);
    }
}

[EventType]
public record CustomerRegistered(string Email, string DisplayName);
```

## Use typed ids

If your solution uses a type that derives from `EventSourceId`, you can return that type in the tuple as well.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

public record CustomerId(Guid Value) : EventSourceId<Guid>(Value);

[Command]
public record RegisterCustomer(string Email)
{
    public (CustomerRegistered, CustomerId) Handle()
    {
        var customerId = new CustomerId(Guid.NewGuid());
        return (new CustomerRegistered(Email), customerId);
    }
}

[EventType]
public record CustomerRegistered(string Email);
```

## Important behavior

- Chronicle treats the returned `EventSourceId` as append metadata and as the command response value.
- If the command record already had an event source id, the returned tuple value wins.
- If you need to target several different event sources from one command, use [Events](./events.md#events-for-specific-event-sources) and return `EventForEventSourceId` values instead.
