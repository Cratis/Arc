---
title: Cratis Arc command read models with EF Core or MongoDB
description: Use EF Core or MongoDB read models as Cratis Arc command inputs. Configure provider ownership and declare the key that loads each row or document.
---

Cratis Arc can inject an Entity Framework Core entity or MongoDB document into a command without Chronicle. Register the storage provider and declare the command's lookup key; a validator, `Provide()`, or `Handle()` can then receive the model as an input.

The examples below use standalone Arc with the data annotations command key. If you also register Chronicle, use its identity rules instead. Each example shows a dependency and its lookup key, not a complete rename operation: returning `CustomerRenamed` does not write the new name to either store. Your standalone command must persist the change through its provider.

Use this page to establish which provider loads a model and which key selects it. [Read models in commands](./injecting-into-commands.md) explains where to place the dependency and how nullability handles missing state; Chronicle-specific read protection does not extend to these stores.

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

Put the `[ReadModel]` entity in a `DbSet` on a registered `ReadOnlyDbContext`. That declares EF Core's ownership of the type and makes it available as a command dependency:

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

`WithEntityFrameworkCore()` discovers the `ReadOnlyDbContext`. Arc resolves the command key from `[Key]` on `CustomerId`, and EF Core uses it to load the entity by primary key. Supported primary-key shapes are `Guid`, `int`, `long`, `string`, and a `ConceptAs<T>` wrapping one of those. This resolver requires a single-property primary key; a missing or composite primary key fails with `EntityDoesNotHavePrimaryKey`.

The nullable rules are identical: a nullable `Customer?` receives `null` when no row exists, and a non-nullable `Customer` fails the command with [`ReadModelDoesNotExistForCommand`](./failures.md#readmodeldoesnotexistforcommand).

## MongoDB

Register MongoDB with `WithMongoDB()`. Its fallback resolver makes a `[ReadModel]` injectable by document `_id` when no declaring provider or application registration already owns that type:

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

Give each model one intended owner. The owner controls how Arc loads command-side state; merely having a MongoDB collection for a type does not make MongoDB its command provider. Declared ownership wins over fallback, while two declaring providers compete by registration order:

| Provider | Owns a read model when | Claims it as |
| --- | --- | --- |
| Chronicle | a projection, model-bound projection, or classic reducer targets it, or Chronicle discovers it as an eligible self-reducing model (19.58.0+) | declared |
| Entity Framework Core | a `DbSet` on a `ReadOnlyDbContext` carries it | declared |
| MongoDB | — a collection is served for any read model | fallback |

A declaring provider wins over a fallback in either registration order. If Chronicle and EF Core both declare the same CLR type, the later registration wins. MongoDB resolves only otherwise-unclaimed models and leaves your own read-model registration alone.

Keep a Chronicle-backed model with Chronicle when it needs Chronicle's release of compliance-protected values. This includes [self-reducing models](./index.md#self-reducing-read-models). Discovery still checks the model's shape: listing a plain reducer implementation in the reducer artifacts does not make that implementation its own injectable read model.

### What else the winner decides

The winning provider also determines how values become a command-side instance:

| Provider | Materializes a command-side read model through |
| --- | --- |
| Chronicle | ordinarily a service JSON payload deserialized with `System.Text.Json`; passive reducers can fold state in-process |
| Entity Framework Core | its own entity model |
| MongoDB | the driver's `BsonClassMap` and convention machinery |

Apply serialization customization to the provider that actually loads the instance. A MongoDB convention pack or BSON class map cannot control a model deserialized from Chronicle JSON. A JSON converter does not replace EF Core's entity mapping.

Chronicle and Entity Framework Core both declare. For each type claimed by either, MongoDB's fallback does not supply the command-side instance. Its BSON customization therefore does not govern that instance. Other types in the same application can still be MongoDB-owned. See [materialized and passive paths](./index.md#materialized-and-passive-paths).

:::caution[Query-side customization does not prove command-side ownership]
`ICanProvideMongoDBConventionPacks` registers conventions in the driver's global registry. They apply to queries served from `IMongoCollection<T>` because the driver materializes those results. If the same customization appears to be ignored in a command, check which provider owns its read-model dependency before changing the convention.
:::

To contribute a provider of your own, implement `ICanResolveReadModelForCommand` — reporting the types it resolves, the `ReadModelForCommandOwnership` it claims them with, and how to load one by key — and register it with `services.AddReadModelsForCommand(...)`.

## Declaring the key without Chronicle

Every provider loads by the resolved command key. When the Chronicle integration is registered, its resolver supplies that key through `ICanProvideEventSourceId`, an `EventSourceId`/`EventSourceId<T>`-derived property, or Chronicle `[Key]`. Otherwise Arc uses its provider-neutral key rules.

Without Chronicle, mark the command property holding the lookup key with the data annotations `[Key]`:

```csharp
using System.ComponentModel.DataAnnotations;

[Command]
public record RenameCustomer([property: Key] CustomerId CustomerId, CustomerName NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

The key may be a `Guid`, `int`, `long`, `string`, or a `ConceptAs<T>` wrapping one of those — a concept resolves to the value it wraps rather than to its own `ToString()`.

If the stored key combines properties or is derived from them, implement `ICanProvideKeyForCommand` to supply it explicitly:

```csharp
[Command]
public record FindItem(CartId CartId, ItemId ItemId) : ICanProvideKeyForCommand
{
    public string GetKey() => $"{CartId.Value}/{ItemId.Value}";
    public bool Handle(CartItem? item) => item is not null;
}
```

The composite-key fragment assumes an application `CartItem` registered with a matching string key and imports from `Cratis.Arc.Commands` and `Cratis.Arc.Commands.ModelBound`. It produces one lookup value; it does not add support for an EF Core composite primary key.

Arc does not guess which identifier is the key. A command carrying two identifiers and marking neither resolves no key, so read-model injection fails as a validation error, even for a nullable model parameter. Nullability handles a valid key with no matching row or document, not the absence of a key. Declaring the key also prevents adding a second identifier from silently changing the lookup.

For an application-wide key convention, implement `ICanResolveKeyForCommand`. Arc discovers it automatically and asks it before the built-in rule, regardless of discovery order.

:::caution[Two attributes are spelled `[Key]`]
In an application **with** Chronicle, the data annotations `[Key]` is not used by the Chronicle resolver. If no other provider or recognized identity property supplies a key, Chronicle generates a fresh event source id; a lookup for existing state then usually finds nothing. [ARCCHR0008](../code-analysis/ARCCHR0008.md) reports it, so this is a build warning rather than a puzzling "the entity does not exist" at runtime.
:::

## See also

- [Read models in commands](./injecting-into-commands.md) — where to declare the dependency and what nullability means.
- [When resolution fails](./failures.md) — every error and what it means.
- [Arc without event sourcing](../../../../arc-without-event-sourcing.mdx) — the whole slice, with the data stored straight in a collection.
