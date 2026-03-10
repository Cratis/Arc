---
uid: Arc.Chronicle.Commands.Events
---
# Events

When a [model-bound](../../commands/model-bound/index.md) command handler returns an event (or a collection of events), Chronicle appends those events to the event log automatically. This lets you keep command handlers focused on decisions and domain rules instead of event log plumbing.

```csharp
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

[Command]
public record RegisterCustomer(EventSourceId CustomerId, string Email)
{
    public CustomerRegistered Handle()
    {
        return new CustomerRegistered(CustomerId, Email);
    }
}

[EventType]
public record CustomerRegistered(EventSourceId CustomerId, string Email);
```

You can also return multiple events as a collection:

```csharp
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

[Command]
public record UpdateCustomerProfile(EventSourceId CustomerId, string DisplayName, string Email)
{
    public IEnumerable<object> Handle()
    {
        return new object[]
        {
            new CustomerDisplayNameChanged(CustomerId, DisplayName),
            new CustomerEmailChanged(CustomerId, Email)
        };
    }
}

[EventType]
public record CustomerDisplayNameChanged(EventSourceId CustomerId, string DisplayName);

[EventType]
public record CustomerEmailChanged(EventSourceId CustomerId, string Email);
```

Chronicle uses the command context to resolve the event source identity and event stream metadata before appending events.

## Event Source Id Resolution

Chronicle resolves the event source id for commands using a small set of conventions. This value is stored in the command context and is required for event appending.

Chronicle resolves the event source id in this order:

1. Implement `ICanProvideEventSourceId` on the command and return the id from `GetEventSourceId()`.
2. Add a property of type `EventSourceId` to the command.
3. Mark a property with `[Key]` and let Chronicle use its value as the event source id.

If none of these are present, Chronicle creates a new `EventSourceId` so the command still has a valid identity for event appends.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

[Command]
public record OpenAccount(Guid AccountId, string OwnerName) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => AccountId.ToString();
}

[Command]
public record RenameAccount(EventSourceId AccountId, string NewName);

[Command]
public record CloseAccount([Key] Guid AccountId);
```

## Event Stream Metadata

Chronicle supports additional metadata that can be attached to commands and used when appending events. This metadata tags the appended events with the specified stream identity, making them easier to query and react to.

### EventStreamId

Use `[EventStreamId]` to assign a specific event stream id to a command, or implement `ICanProvideEventStreamId` to supply it dynamically.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
[EventStreamId("customer-profile")]
public record UpdateCustomerProfile(EventSourceId CustomerId, string DisplayName, string Email);

[Command]
public record UpdateCustomerPreferences(EventSourceId CustomerId, string PreferenceKey, string PreferenceValue)
    : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => "customer-preferences";
}
```

If both a non-empty `[EventStreamId]` value and `ICanProvideEventStreamId` are used, Chronicle treats this as ambiguous and throws an `AmbiguousEventStreamId` exception. Choose one approach, or set the attribute value to `null` to defer to the interface.

### EventStreamType

Use `[EventStreamType]` to categorize events under a named stream type. This is useful for grouping related streams, such as separating onboarding events from transaction events for the same event source.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
[EventStreamType("Onboarding")]
public record RegisterCustomer(EventSourceId CustomerId, string Email);
```

### EventSourceType

Use `[EventSourceType]` to tag events with a specific event source type when they are appended.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
[EventSourceType("Customer")]
public record RegisterCustomer(EventSourceId CustomerId, string Email);
```

These metadata attributes tag the appended events without affecting concurrency control. To additionally participate in [concurrency scoping](./concurrency.md), set `concurrency: true` on the attribute.
