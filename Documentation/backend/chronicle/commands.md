---
uid: Arc.Chronicle.Commands
---
# Commands and Events

Chronicle extends the Arc command pipeline with event sourcing behavior. It works the same way in [Arc.Core](../core/index.md) and in the broader [Commands](../commands/index.md) system, so you can use the same command patterns in both environments.

This page focuses on one concept: returning events from a command handler and letting Chronicle append them automatically.

## Automatic Event Appending

When a model-bound command handler returns an event (or a collection of events), Chronicle appends those events to the event log automatically. This lets you keep command handlers focused on decisions and domain rules instead of event log plumbing.

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

Chronicle uses the command context to resolve the event source identity, event stream metadata, and concurrency scope before appending events.

## Event Source Id Resolution

Chronicle resolves the event source id for commands using a small set of conventions. This value is stored in the command context and is required for event appending.

Chronicle resolves the event source id in this order:

- Implement `ICanProvideEventSourceId` on the command and return the id from `GetEventSourceId()`.
- Add a property of type `EventSourceId` to the command.
- Mark a property with `[Key]` and let Chronicle use its value as the event source id.

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

## Event Stream Id and Event Source Type

Chronicle supports additional metadata that can be attached to commands and then used when appending events.

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

If both a non-empty `[EventStreamId]` value and `ICanProvideEventStreamId` are used, Chronicle treats this as ambiguous. Choose one or set the attribute value to `null` to use the interface.

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

## Concurrency Scope from Metadata

Chronicle can build a concurrency scope based on command metadata so event appends can participate in optimistic concurrency checks. The scope is built from any of these attributes when their `concurrency` flag is set to `true`:

- `EventStreamIdAttribute`
- `EventStreamTypeAttribute`
- `EventSourceTypeAttribute`

This builds a [concurrency scope](xref:Chronicle.ConcurrencyScope) using the metadata values in the command context.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;

[Command]
[EventStreamId("customer-profile", concurrency: true)]
[EventSourceType("Customer", concurrency: true)]
public record UpdateCustomerProfile(EventSourceId CustomerId, string DisplayName, string Email)
{
    public CustomerDisplayNameChanged Handle() => new(CustomerId, DisplayName);
}

[EventType]
public record CustomerDisplayNameChanged(EventSourceId CustomerId, string DisplayName);
```

If no metadata has `concurrency: true`, Chronicle does not include a concurrency scope when appending events.

