---
title: Concepts in MongoDB
description: Store strongly typed standalone value records with explicit BSON and nullability contracts.
---

Use `ConceptAs<T>` to distinguish values such as a user ID and a product name without storing a wrapper document. Arc's MongoDB integration registers `ConceptSerializationProvider` during setup; Chronicle is not needed.

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

These are complete type declarations. `ConceptAs<T>` is a record, so derive a **record**, not a class. A standalone Guid-backed identity such as `UserId` is valid; do not replace every Guid concept with a Chronicle identity.

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

`Name` is stored as a BSON string. Guid concepts are stored as Standard UUID **binary**, not a JSON string. A null concept reference is stored as BSON null. Actual element names follow your [naming policy](./naming-policies.md) and ID mapping.

Use nullable concept references for optional values (`UserId?`). `ConceptAs<Guid?>` is invalid because nullable Guid does not meet the primitive generic constraint. Do not confuse the concept's non-null sentinel (`NotSet`) with an absent/null concept.

## Serialization and validation

`ConceptSerializer<T>` unwraps values on writes and reconstructs concepts on reads. It can read the primitive form and a legacy wrapper document containing `Value` or `value`. Construction with a non-concept type, such as `new ConceptSerializer<string>()`, throws `TypeIsNotAConcept`.

BSON representation and range limits still apply to the underlying primitive. Custom serializer availability alone does not guarantee every possible `ConceptAs<T>` behaves identically: verify representative values and nulls for your type. See [serializers](./serializers.md), especially date/time precision.

Do not define both a positional record constructor and another constructor with the same `string` signature. A simple email value type is:

```csharp
using Cratis.Concepts;

public record EmailAddress(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator EmailAddress(string value) => new(value);
}
```

This declaration does **not** validate email syntax. Put input rules in the application's validation layer; BSON serialization is not command validation. Collections of concepts also use their underlying values, but dictionary key representation must satisfy the driver's dictionary rules—do not assume arbitrary concept keys work with document-form dictionaries.

## Query with typed values

Method fragment for an injected `IMongoCollection<Product>` named `collection` (`MongoDB.Driver` imported):

```csharp
var ownerId = UserId.New();
var products = await collection.Find(product => product.OwnerId == ownerId).ToListAsync();
```

The member serializer renders the concept as its BSON primitive. A newly generated ID normally returns no rows unless matching data exists.

## Optional Chronicle integration

If the identifier represents a Chronicle event source, use that integration's [event-source ID conventions](../chronicle/resolving-event-source-id.md). That is an additional event-sourcing contract, not a MongoDB bootstrap requirement or a restriction on ordinary Guid concepts.

Continue with [class mapping](./class-mapping.md) and [convention packs](./convention-packs.md).
