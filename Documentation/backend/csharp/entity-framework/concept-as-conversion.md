---
title: Map ConceptAs values with EF Core in Cratis Arc
description: Map ConceptAs records to EF Core columns in Cratis Arc, keeping named domain values in C# and storing their underlying values in the database.
---

Cratis Arc maps `ConceptAs<T>` records to EF Core columns through value converters. Use these [concepts](/arc/glossary/) to keep identifiers and other domain values distinct in C#, while storing their underlying values in the database.

## What it does

A value converter takes a concept's `Value` when writing a column and constructs the concept again when reading it. Arc also configures an EF value comparer so change tracking can compare concept values. You do not need a separate converter for every concept type.

`BaseDbContext` applies these converters during model creation. If you use an ordinary `DbContext`, apply the conversion explicitly as shown below.

## Why it's important

A `CustomerId` says more than `Guid` about what a property or parameter expects. Keeping that name in your entity helps you see which identifier an API needs. The conversion preserves that C# distinction without introducing a separate table or structured column for the wrapper.

Choose constraints such as required values, maximum lengths and keys in your [entity mappings](./entity-mapping.md). A concept converter handles representation; it does not define those database constraints.

## Model Usage

Declare the entity properties using your concept types. This example has a required identifier and name, with an optional email address:

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

Use records because `ConceptAs<T>` is a record. Represent optionality with a nullable concept reference (`EmailAddress?`), not a nullable primitive argument such as `ConceptAs<Guid?>`, which violates the generic constraint. These records do not require a Chronicle identity type.

With `BaseDbContext`, `CustomerName` and `EmailAddress` map to string values. `CustomerId` maps to a Guid value, except on SQLite where Arc converts it to a string. When EF loads a customer, it constructs the corresponding concept records from those stored values.

## Manual Configuration

If your context does not inherit from [`BaseDbContext`](./base-db-context.md), apply the conversion after adding the entity types to the model. The following context exposes `Customer` through a `DbSet<Customer>`:

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

This configures model conversion. Arc's [context registration helpers](./automatic-database-hookup.md) separately install `ConceptAs<T>` query support; keep that registration when you need concepts in LINQ expressions.

## How it works

`ConceptAsConversion.ApplyConceptAsConversion()` inspects the entity types passed to it for concept properties. For each property, it calls `AsConcept(databaseType)` to install a value converter and comparer. The provider argument selects the string representation for Guid-backed concepts on SQLite.

If you need to inspect that storage choice before creating a migration, continue to [Guid conversion](./guid-conversion.md).
