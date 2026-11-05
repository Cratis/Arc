---
title: Resolve command EventSourceId in Arc and Chronicle
description: Arc's Chronicle integration selects a stream identity before command dependencies run; returned identities and query arguments follow separate rules.
---

The optional integration between Cratis Arc and Cratis Chronicle selects an `EventSourceId`, the identity of an entity's event stream, before a model-bound command runs. Use it to load the right command-scoped read models and aggregate roots and to target returned events. The authenticated user's identity does not select that stream.

For a command that changes an existing entity, supply its stream identity in the command input. Returning an identity later cannot change which state the command used to make its decision.

## Resolution order for commands

1. An `ICanProvideEventSourceId` implementation supplies the identity explicitly.
2. Otherwise, Arc selects the first matching public property: an `EventSourceId`, an `EventSourceId<T>`-derived type, or a property carrying Chronicle's `[Key]`. A matching positional constructor parameter can carry `[Key]` too.
3. With no candidate, Arc generates a new identity. This suits creation commands; it does not locate an existing entity. A declared but unusable key can instead resolve to `EventSourceId.Unspecified` and fail dependency resolution.

Arc does not prefer a typed property over a `[Key]` property. Declare exactly one candidate or implement the provider so the selected stream is unambiguous. An implicit conversion on an ordinary `ConceptAs<Guid>` does not make it an identity candidate; derive the domain identity from `EventSourceId<Guid>` instead.

In this command/type example, `AccountId` selects the stream and `OwnerName` is an ordinary domain value. Register the host and persistence as described in [Cratis package](./cratis-package.md):

```csharp
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Concepts;

public record AccountId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly AccountId NotSet = new(Guid.Empty);
    public static AccountId New() => new(Guid.NewGuid());
    public static implicit operator AccountId(Guid value) => new(value);
}

public record OwnerName(string Value) : ConceptAs<string>(Value)
{
    public static readonly OwnerName NotSet = new(string.Empty);
    public static implicit operator OwnerName(string value) => new(value);
}

[Command]
public record OpenAccount(AccountId AccountId, OwnerName OwnerName)
{
    public AccountOpened Handle() => new(OwnerName);
}

/// <summary>
/// Records that an account was opened with its initial owner name.
/// </summary>
[EventType]
public record AccountOpened(OwnerName OwnerName);
```

`OpenAccount.AccountId` is recognized through its `EventSourceId<Guid>` base, so it needs no `[Key]` attribute. For a legacy primitive key, mark the command property with `Cratis.Chronicle.Keys.KeyAttribute`. `System.ComponentModel.DataAnnotations.KeyAttribute` belongs to Arc's provider-neutral key convention and is not the attribute this Chronicle resolver uses.

## Input identity versus response identity

An `EventSourceId` or `EventSourceId<T>` response can override the default target for return-driven appends after `Handle()` finishes. By then, command dependencies have already used the input identity. The response cannot reload a validator's read model, retarget an injected aggregate, or move events that aggregate already enrolled.

A raw `Guid` remains an ordinary response. A keyless command can legitimately return an unrelated confirmation Guid while events use the generated fallback. [ARCCHR0010](./code-analysis/ARCCHR0010.md) is a heuristic warning to check that intent, not a runtime prohibition. See [Returning EventSourceId](./commands/returning-event-source-id.md).

## Query arguments are ordinary Arc binding

Ordinary model-bound queries bind arguments by name and convert them to their declared types. They do **not** invoke the Chronicle command-key resolver. A typed id helps conversion and type safety; `[Key]` on a query argument does not select a read model automatically.

This query reuses `AccountId` and `OwnerName` from the command example and assumes the MongoDB integration is registered. The `id` argument is bound by name. The explicit filter selects the document; the command-key resolver does not participate:

```csharp
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

[ReadModel]
public record AccountOverview(AccountId Id, OwnerName OwnerName)
{
    public static AccountOverview? ById(
        AccountId id,
        IMongoCollection<AccountOverview> collection) =>
        collection.Find(account => account.Id == id).FirstOrDefault();
}
```

## Related references

- [Command context values](../commands/command-context.md#command-context-values)
- [Read models in commands](./read-models/injecting-into-commands.md)
- [Aggregate roots in commands](./aggregates/injecting-into-commands.md)
- [Setting Subject](./commands/subject.md) — compliance identity is separate from stream identity.
