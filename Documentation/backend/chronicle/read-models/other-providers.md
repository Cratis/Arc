---
title: Read models from other providers
description: Inject a read model backed by Entity Framework Core or MongoDB into a command, and declare the key it is loaded by when there is no Chronicle to resolve one.
---

Injection is not Chronicle-only. Any provider that owns a read model's storage can make its `[ReadModel]` types injectable into a command, resolved by the same key, so a validator, `Provide()`, or `Handle()` takes the read model exactly as it would a Chronicle-backed one.

This compatibility reference covers *where the read model comes from* and *what key loads it*. Provider-neutral injection does not require Chronicle. The EF and MongoDB examples below assume standalone Arc and use its data annotations command key; if you register Chronicle, use its identity rules instead. The examples are dependency/key fragments: returning `CustomerRenamed` alone does not persist a rename without a handler or integration that performs that effect; a complete standalone rename must write through its provider. For where to put the parameter and what a nullable one means, see [Read models in commands](./injecting-into-commands.md).

## Shared domain values

These standalone examples use `ConceptAs<T>`, not Chronicle event-source identities. Keep each declaration in its own application file. The named types prevent passing a cart id where a customer id is expected:

```csharp
using Cratis.Concepts;

public record CustomerId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly CustomerId NotSet = new(Guid.Empty);

    public static CustomerId New() => new(Guid.NewGuid());
    public static implicit operator CustomerId(Guid value) => new(value);
}

public record CustomerName(string Value) : ConceptAs<string>(Value)
{
    public static readonly CustomerName NotSet = new(string.Empty);

    public static implicit operator CustomerName(string value) => new(value);
}

public record CartId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly CartId NotSet = new(Guid.Empty);

    public static CartId New() => new(Guid.NewGuid());
    public static implicit operator CartId(Guid value) => new(value);
}

public record ItemId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly ItemId NotSet = new(Guid.Empty);

    public static ItemId New() => new(Guid.NewGuid());
    public static implicit operator ItemId(Guid value) => new(value);
}

public record CustomerRenamed(CustomerId CustomerId, CustomerName Name);
```

## Entity Framework Core

A `[ReadModel]` entity carried by a `ReadOnlyDbContext` becomes injectable once the context is registered — there is nothing extra to wire up:

```csharp
[ReadModel]
public class Customer
{
    public required CustomerId Id { get; set; }
    public required CustomerName Name { get; set; }
}

public class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : ReadOnlyDbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
}
```

```csharp
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands.ModelBound;

[Command]
public record RenameCustomer([property: Key] CustomerId CustomerId, CustomerName NewName)
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
public record Customer(CustomerId Id, CustomerName Name)
{
    public static IEnumerable<Customer> AllCustomers(IMongoCollection<Customer> collection) =>
        collection.Find(_ => true).ToList();
}
```

```csharp
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands.ModelBound;

[Command]
public record RenameCustomer([property: Key] CustomerId CustomerId, CustomerName NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

The id member is whichever one MongoDB maps to `_id` — a member named `Id` by convention, or the one marked `[BsonId]`. Like the EF primary key it may be a `Guid`, `int`, `long`, `string`, or a `ConceptAs<T>` wrapping one of those. A read model with no member mapped to `_id` cannot be resolved by key, and injecting it fails with `MissingIdMapping`.

## Which provider resolves a read model

When a fallback and a declaring provider can both load a read model, declared ownership wins. Avoid having two providers declare the same CLR type: that conflict is registration-order-dependent.

| Provider | Owns a read model when | Claims it as |
| --- | --- | --- |
| Chronicle | a projection, model-bound projection, or reducer targets it | declared |
| Entity Framework Core | a `DbSet` on a `ReadOnlyDbContext` carries it | declared |
| MongoDB | — a collection is served for any read model | fallback |

A declaring provider wins over a fallback in either registration order. Two declaring providers (for example Chronicle and EF Core) replace one another; the later registration wins. MongoDB claims only what nothing else resolves, and it also leaves your own registration of a read model type alone. This matters beyond tidiness: Chronicle is the provider that releases a read model's compliance-protected values, so a read model Chronicle projects has to be resolved by Chronicle.

### What else the winner decides

The provider that claims a read model also decides which serialization boundary the injected instance crosses, and the three cross entirely different ones:

| Provider | Materializes a command-side read model through |
| --- | --- |
| Chronicle | ordinarily a service JSON payload deserialized with `System.Text.Json`; passive reducers can fold state in-process |
| Entity Framework Core | its own entity model |
| MongoDB | the driver's `BsonClassMap` and convention machinery |

So whatever customization belongs to one of those boundaries — a convention pack, a class-map customization, an element rename, a custom serializer, a JSON converter — reaches a command-side read model only when its own provider is the one that claimed it.

Chronicle and Entity Framework Core both declare. For each type claimed by either, MongoDB's fallback does not supply the command-side instance. Its BSON customization therefore does not govern that instance. Other types in the same application can still be MongoDB-owned. See [materialized and passive paths](./index.md#materialized-and-passive-paths).

:::warning[The same customization can be plainly at work on the query side]
A convention registered through `ICanProvideMongoDBConventionPacks` goes into the driver's global registry, so it applies wherever the driver materializes a read model — which includes queries served from an `IMongoCollection<T>`. Seeing it work there says nothing about the command side, and this is the shape the failure takes: the customization looks discovered and correct, because the surface anybody checks first is the one it does reach.
:::

To contribute a provider of your own, implement `ICanResolveReadModelForCommand` — reporting the types it resolves, the `ReadModelForCommandOwnership` it claims them with, and how to load one by key — and register it with `services.AddReadModelsForCommand(...)`.

## Declaring the key without Chronicle

Every provider loads by the resolved command key. When the Chronicle integration is registered, its resolver supplies that key through `ICanProvideEventSourceId`, an `EventSourceId`/`EventSourceId<T>`-derived property, or Chronicle `[Key]`. Otherwise Arc uses its provider-neutral key rules.

An application without Chronicle has none of those, so Arc reads the key from the command itself. Mark the property holding it with the data annotations `[Key]`:

```csharp
using System.ComponentModel.DataAnnotations;

[Command]
public record RenameCustomer([property: Key] CustomerId CustomerId, CustomerName NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

The key may be a `Guid`, `int`, `long`, `string`, or a `ConceptAs<T>` wrapping one of those — a concept resolves to the value it wraps rather than to its own `ToString()`.

When the key is not one property — a composite of two, or a value derived from them — the command declares it:

```csharp
[Command]
public record FindItem(CartId CartId, ItemId ItemId) : ICanProvideKeyForCommand
{
    public string GetKey() => $"{CartId.Value}/{ItemId.Value}";
    public bool Handle(CartItem? item) => item is not null;
}
```

The composite-key fragment assumes an application `CartItem` registered with a matching string key and imports from `Cratis.Arc.Commands` and `Cratis.Arc.Commands.ModelBound`.

Without Chronicle, nothing is inferred from the shape of a command. One carrying two identifiers and marking neither resolves no key, and injection fails as a validation error rather than silently picking one of them.

To key commands your own way across an application, implement `ICanResolveKeyForCommand`. It is discovered automatically and asked before the rule Arc ships, whichever order the two happen to be discovered in.

:::warning[Two attributes are spelled `[Key]`]
In an application **with** Chronicle, the data annotations `[Key]` is not used by the Chronicle resolver. If no other provider or recognized identity property supplies a key, Chronicle generates a fresh event source id; a lookup for existing state then usually finds nothing. [ARCCHR0008](../code-analysis/ARCCHR0008.md) reports it, so this is a build warning rather than a puzzling "the entity does not exist" at runtime.
:::

## See also

- [Read models in commands](./injecting-into-commands.md) — where to declare the dependency and what nullability means.
- [When resolution fails](./failures.md) — every error and what it means.
- [Arc without event sourcing](../../../arc-without-event-sourcing.md) — the whole slice, with the data stored straight in a collection.
