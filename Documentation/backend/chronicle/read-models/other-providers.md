---
title: Read models from other providers
description: Inject a read model backed by Entity Framework Core or MongoDB into a command, and declare the key it is loaded by when there is no Chronicle to resolve one.
---

Injection is not Chronicle-only. Any provider that owns a read model's storage can make its `[ReadModel]` types injectable into a command, resolved by the same key, so a validator, `Provide()`, or `Handle()` takes the read model exactly as it would a Chronicle-backed one.

This page is about *where the read model comes from* and *what key loads it*. For where to put the parameter and what a nullable one means, see [Read models in commands](./injecting-into-commands.md).

## Entity Framework Core

A `[ReadModel]` entity carried by a `ReadOnlyDbContext` becomes injectable once the context is registered — there is nothing extra to wire up:

```csharp
[ReadModel]
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : ReadOnlyDbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
}
```

```csharp
[Command]
public record RenameCustomer([Key] Guid CustomerId, string NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

`WithEntityFrameworkCore()` discovers the `ReadOnlyDbContext`, and the command's resolved key (here the `[Key]` on `CustomerId`) loads the entity by its primary key. The primary key may be a `Guid`, `int`, `long`, `string`, or a `ConceptAs<T>` wrapping one of those.

The nullable rules are identical: a nullable `Customer?` receives `null` when no row exists, and a non-nullable `Customer` fails the command with [`ReadModelDoesNotExistForCommand`](./failures.md#readmodeldoesnotexistforcommand).

## MongoDB

`WithMongoDB()` does the same for the read models MongoDB holds. There is nothing to declare — a `[ReadModel]` becomes injectable, resolved by the document `_id`:

```csharp
[ReadModel]
public record Customer(Guid Id, string Name)
{
    public static IEnumerable<Customer> AllCustomers(IMongoCollection<Customer> collection) =>
        collection.Find(_ => true).ToList();
}
```

```csharp
[Command]
public record RenameCustomer([Key] Guid CustomerId, string NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

The id member is whichever one MongoDB maps to `_id` — a member named `Id` by convention, or the one marked `[BsonId]`. Like the EF primary key it may be a `Guid`, `int`, `long`, `string`, or a `ConceptAs<T>` wrapping one of those. A read model with no member mapped to `_id` cannot be resolved by key, and injecting it fails with `MissingIdMapping`.

## Which provider resolves a read model

More than one provider can be able to load the same read model, and the order an application registers them in should not decide the outcome. What decides it is whether an artifact in the application says the provider *owns* the read model:

| Provider | Owns a read model when | Claims it as |
|---|---|---|
| Chronicle | a projection, model-bound projection, or reducer targets it | declared |
| Entity Framework Core | a `DbSet` on a `ReadOnlyDbContext` carries it | declared |
| MongoDB | — a collection is served for any read model | fallback |

A declaring provider always wins, in either registration order. MongoDB claims only what nothing else resolves, and it also leaves your own registration of a read model type alone. This matters beyond tidiness: Chronicle is the provider that releases a read model's compliance-protected values, so a read model Chronicle projects has to be resolved by Chronicle.

### What else the winner decides

The provider that claims a read model also decides which serialization boundary the injected instance crosses, and the three cross entirely different ones:

| Provider | Materializes a command-side read model through |
|---|---|
| Chronicle | a JSON payload deserialized with `System.Text.Json` |
| Entity Framework Core | its own entity model |
| MongoDB | the driver's `BsonClassMap` and convention machinery |

So whatever customization belongs to one of those boundaries — a convention pack, a class-map customization, an element rename, a custom serializer, a JSON converter — reaches a command-side read model only when its own provider is the one that claimed it.

Chronicle and Entity Framework Core both declare. In an application whose read models are owned by either, MongoDB never claims a command-side read model, and no MongoDB serialization customization reaches one — however the MongoDB integration is configured, and in whatever order anything is registered.

:::warning[The same customization can be plainly at work on the query side]
A convention registered through `ICanProvideMongoDBConventionPacks` goes into the driver's global registry, so it applies wherever the driver materializes a read model — which includes queries served from an `IMongoCollection<T>`. Seeing it work there says nothing about the command side, and this is the shape the failure takes: the customization looks discovered and correct, because the surface anybody checks first is the one it does reach.
:::

To contribute a provider of your own, implement `ICanResolveReadModelForCommand` — reporting the types it resolves, the `ReadModelForCommandOwnership` it claims them with, and how to load one by key — and register it with `services.AddReadModelsForCommand(...)`.

## Declaring the key without Chronicle

Every provider loads a read model by the command's key, and Chronicle is what resolves that key — from `ICanProvideEventSourceId`, from a property assignable to `EventSourceId`, or from one carrying `Cratis.Chronicle.Keys.KeyAttribute`.

An application without Chronicle has none of those, so Arc reads the key from the command itself. Mark the property holding it with the data annotations `[Key]`:

```csharp
using System.ComponentModel.DataAnnotations;

[Command]
public record RenameCustomer([property: Key] Guid CustomerId, string NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

The key may be a `Guid`, `int`, `long`, `string`, or a `ConceptAs<T>` wrapping one of those — a concept resolves to the value it wraps rather than to its own `ToString()`.

When the key is not one property — a composite of two, or a value derived from them — the command declares it:

```csharp
[Command]
public record MoveItem(Guid CartId, Guid ItemId) : ICanProvideKeyForCommand
{
    public string GetKey() => $"{CartId}/{ItemId}";
}
```

Nothing is inferred from the shape of a command. One carrying two identifiers and marking neither resolves no key, and injection fails as a validation error rather than silently picking one of them.

To key commands your own way across an application, implement `ICanResolveKeyForCommand`. It is discovered automatically and asked before the rule Arc ships, whichever order the two happen to be discovered in.

:::warning[Two attributes are spelled `[Key]`]
In an application **with** Chronicle, the data annotations `[Key]` does nothing. Chronicle resolves keys from `Cratis.Chronicle.Keys.KeyAttribute`, invents a fresh event source id when it finds no key property, and every read model keyed by that command then resolves to nothing. [ARCCHR0008](../code-analysis/ARCCHR0008.md) reports it, so this is a build warning rather than a puzzling "the entity does not exist" at runtime.
:::

## See also

- [Read models in commands](./injecting-into-commands.md) — where to declare the dependency and what nullability means.
- [When resolution fails](./failures.md) — every error and what it means.
- [Arc without event sourcing](../../../arc-without-event-sourcing.md) — the whole slice, with the data stored straight in a collection.
