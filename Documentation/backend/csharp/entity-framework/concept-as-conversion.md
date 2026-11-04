---
title: ConceptAs conversion
description: Map strongly typed value records to EF provider values.
---

ConceptAs conversion lets your Entity Framework Core entities use Cratis [Concepts](/arc/glossary/) while storing their underlying values. `BaseDbContext` applies the conversion during model creation; an ordinary `DbContext` needs the explicit configuration shown below.

## What it does

For a `ConceptAs<T>` property selected by the conversion, Arc configures:

1. **Storage**: Convert the concept to its underlying primitive value for the database.
2. **Retrieval**: Reconstruct the concept from that value when loading the entity.
3. **Comparison**: Configure value comparison for EF change tracking.

The entity keeps its domain type while EF uses the provider value. You do not need to write a separate converter for each concept type.

## Why it's important

A `CustomerName` and an `EmailAddress` are distinct C# types even though both wrap a string. Conversion lets you keep that distinction in entity properties while storing primitive values. Define domain rules in the concept or the application's validation rather than relying on the storage mapping.

## Model Usage

Declare the concepts and use them directly as entity properties. The conversion must be active on the context that maps this model:

```csharp
using Cratis.Concepts;

public class Customer
{
    public required CustomerId Id { get; set; }
    public required CustomerName Name { get; set; }
    public EmailAddress? Email { get; set; }
}

public record CustomerId(Guid Value) : ConceptAs<Guid>(Value);
public record CustomerName(string Value) : ConceptAs<string>(Value);
public record EmailAddress(string Value) : ConceptAs<string>(Value);
```

Use records because `ConceptAs<T>` is a record. Represent optionality with a nullable concept reference (`EmailAddress?`), not a nullable primitive argument such as `ConceptAs<Guid?>`, which violates the generic constraint. These are standalone value types; no Chronicle identity type is required.

For this model, `BaseDbContext` configures the conversion to:

- Store `CustomerId` as a Guid value (as a string through Arc's SQLite converter)
- Store `CustomerName` and `EmailAddress` as `string` values in the database
- Convert back to the appropriate concept types when loading entities

## Manual Configuration

An ordinary `DbContext` does not apply this convention. If your context does not derive from [`BaseDbContext`](./base-db-context.md), apply ConceptAs conversion during model creation:

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.EntityFrameworkCore;

public class StoreDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes();
        modelBuilder.ApplyConceptAsConversion(entityTypes, Database.GetDatabaseType());
        base.OnModelCreating(modelBuilder);
    }
}
```

> Note: This is automatically configured for you when using the [`BaseDbContext`](./base-db-context.md).

## How it works

`ConceptAsConversion.ApplyConceptAsConversion()` examines the entity types supplied to it and selects their `ConceptAs<T>` properties. It attaches a `ValueConverter` for the concept-to-provider mapping and a `ValueComparer` for change tracking to the EF model.

Query translation remains a separate provider concern.
