---
title: Store ConceptAs values in MongoDB with Cratis Arc
description: Store ConceptAs value types as BSON primitives in Cratis Arc, keeping typed IDs and names in C# while handling nulls and existing documents.
---

Cratis Arc stores `ConceptAs<T>` values as primitives in BSON, MongoDB's binary document format, rather than as wrapper documents. This lets you distinguish a user ID from other IDs in C# while storing the underlying value in MongoDB. [MongoDB setup](./getting-started.md) registers the concept serializer automatically; Chronicle is not required.

## Declare value records

```csharp
using Cratis.Concepts;

public record UserId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly UserId NotSet = new(Guid.Empty);
    public static implicit operator UserId(Guid value) => new(value);
    public static UserId New() => new(Guid.NewGuid());
}

public record ProductName(string Value) : ConceptAs<string>(Value)
{
    public static readonly ProductName NotSet = new(string.Empty);
    public static implicit operator ProductName(string value) => new(value);
}
```

Both declarations derive from `ConceptAs<T>`, which is a record and therefore needs a derived record rather than a class. `UserId` is an ordinary Guid-backed application identity; it has no dependency on Chronicle.

## Store primitive values

Model declaration using the types above:

```csharp
public class Product
{
    public required UserId OwnerId { get; set; }
    public required ProductName Name { get; set; }
    public UserId? ReviewerId { get; set; }
}
```

`Name` is stored as a BSON string, and Guid concepts use Standard UUID binary representation. A null concept reference is stored as BSON null. Field names follow your [naming policy](./naming-policies.md) and ID mapping.

For optional values, make the concept reference nullable, as `ReviewerId` does with `UserId?`. `ConceptAs<Guid?>` does not satisfy the base record's `IComparable` constraint. The example's `NotSet` value is different from null: it is a concept containing `Guid.Empty`, which is stored as a UUID value.

## Serialization and validation

`ConceptSerializer<T>` unwraps values on writes and reconstructs concepts on reads. It can read the primitive form and a legacy wrapper document containing `Value` or `value`. Construction with a non-concept type, such as `new ConceptSerializer<string>()`, throws `TypeIsNotAConcept`.

The underlying BSON type still determines value ranges and precision. Check round trips for the values and nulls your application uses, especially when reading existing documents. The [serializer reference](./serializers.md) explains the date/time representations and their precision limits.

A string-backed concept can also represent an email address:

```csharp
using Cratis.Concepts;

public record EmailAddress(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator EmailAddress(string value) => new(value);
}
```

This declaration gives the value a type; it does not validate email syntax. Put input rules in the application's validation layer rather than relying on BSON serialization. Collections of concepts also use the underlying values. Dictionary keys need a representation supported by the driver, so check that separately before using concepts as keys.

## Query with typed values

Method fragment for an injected `IMongoCollection<Product>` named `collection` (`MongoDB.Driver` imported):

```csharp
var ownerId = UserId.New();
var products = await collection.Find(product => product.OwnerId == ownerId).ToListAsync();
```

The member serializer renders `ownerId` as its BSON primitive in the filter. Replace `UserId.New()` with an ID from your application when looking up existing products; generating a new ID does not create a matching document.

## Optional Chronicle integration

If an identifier represents a Chronicle event source, follow the integration's [event-source ID conventions](../chronicle/resolving-event-source-id.md). Those conventions describe an event-sourcing contract; ordinary MongoDB Guid concepts do not need it.

For other document-specific storage choices, continue with [class mapping](./class-mapping.md). Use [convention packs](./convention-packs.md) when the same rule should apply to several types.
