# Resolving EventSourceId

Chronicle resolves an `EventSourceId` anywhere it needs an identity for an aggregate, event append, or read model lookup. The same conventions work whether the value comes from a command record or from query arguments bound from the HTTP request.

For details on how Chronicle stores resolved values in the command pipeline, see [Command Context Values](../commands/command-context.md#command-context-values).

## Why Chronicle Resolves EventSourceId

Chronicle needs an event source id to:

- append events to the correct event source
- load aggregate roots for model-bound commands
- resolve read models used in command handlers and validators
- match read model queries to a specific aggregate identity

## Resolution Order for Commands

When Chronicle inspects a command, it resolves the event source id in this order:

1. Implement `ICanProvideEventSourceId` and return the id from `GetEventSourceId()`.
2. Add a property whose type is `EventSourceId` or derives from it.
3. Mark a property with `[Key]` and let Chronicle convert that value to `EventSourceId`.

If none of these are present, Chronicle creates a new `EventSourceId` so automatic event appends still have a valid identity.

```csharp
using Cratis.Arc.Commands.ModelBound;
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

## Resolution for Query Arguments

Chronicle uses the same identity conventions when you pass arguments to query methods that target a specific read model instance. In practice this usually means:

- an argument of type `EventSourceId`
- an argument of a type that derives from `EventSourceId`
- an argument or bound property marked with `[Key]`

Those arguments can come from route parameters, query string parameters, or request bodies through Arc's normal query binding rules.

```csharp
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using MongoDB.Driver;

[ReadModel]
public record CustomerOverview(EventSourceId Id, string Name)
{
    public static CustomerOverview? ById(
        EventSourceId id,
        IMongoCollection<CustomerOverview> collection) =>
        collection.Find(_ => _.Id == id).FirstOrDefault();

    public static CustomerOverview? ByLegacyId(
        [Key] Guid customerId,
        IMongoCollection<CustomerOverview> collection) =>
        collection.Find(_ => _.Id == customerId.ToString()).FirstOrDefault();
}
```

## Read Models and Aggregate Roots

When you inject a read model or aggregate root into a model-bound command, Chronicle uses the resolved event source id from the current command context to load the correct instance.

For command-specific guidance, see:

- [Events](commands/events.md)
- [Setting Subject](commands/subject.md)
- [Returning EventSourceId from a Command](commands/returning-event-source-id.md)
