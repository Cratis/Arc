---
title: Resolving EventSourceId
description: How Arc's optional Chronicle integration selects command identity, and why response identity and query binding are separate stages.
---

The Chronicle integration selects an `EventSourceId` before a model-bound command runs. That identity selects its command-scoped read models and aggregate roots, and supplies the default target for returned events. It is not inferred from the authenticated user.

## Resolution order for commands

1. An `ICanProvideEventSourceId` implementation supplies the identity explicitly.
2. Otherwise, Arc selects the first matching public property: an `EventSourceId`, an `EventSourceId<T>`-derived type, or a property carrying Chronicle's `[Key]`. A matching positional constructor parameter can carry `[Key]` too.
3. With no candidate, Arc generates a new identity for creation commands. A declared but unusable key is different: it can resolve to `EventSourceId.Unspecified` and fail dependency resolution.

There is **no typed-property-before-keyed-property precedence**. Declare exactly one candidate or implement the provider. An arbitrary implicit conversion on a `ConceptAs<Guid>` does not make it a runtime identity candidate. Use a domain identity derived from `EventSourceId<Guid>` instead.

The following is a complete command/type example; host registration and persistence configuration are described in [Cratis package](./cratis-package.md).

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

For a legacy primitive key, mark the command property with `Cratis.Chronicle.Keys.KeyAttribute`. Do not substitute `System.ComponentModel.DataAnnotations.KeyAttribute`; that is the provider-neutral Arc key convention, not the Chronicle resolver's attribute.

## Input identity versus response identity

An `EventSourceId` or `EventSourceId<T>` response can override the default target for **return-driven appends** after `Handle()` finishes. It cannot retroactively reload a validator's read model, retarget an injected aggregate, or move events the aggregate already enrolled under its input identity.

A raw `Guid` remains an ordinary response. A keyless command can legitimately return an unrelated confirmation Guid while events use the generated fallback. [ARCCHR0010](./code-analysis/ARCCHR0010.md) is a heuristic warning to check that intent, not a runtime prohibition. See [Returning EventSourceId](./commands/returning-event-source-id.md).

## Query arguments are ordinary Arc binding

Ordinary model-bound queries bind arguments by name and convert them to their declared types. They do **not** invoke the Chronicle command-key resolver. A typed id helps conversion and type safety; `[Key]` on a query argument does not select a read model automatically.

Reuse `AccountId` and `OwnerName` from the command example so the query preserves the same domain vocabulary. This query/type fragment assumes the MongoDB integration is registered. The filter, not a command-key convention, selects the document:

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
