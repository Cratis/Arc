---
title: ConceptAs conversion
description: Map strongly typed value records to EF provider values.
---

The ConceptAs conversion feature provides automatic type conversion support for Cratis [Concepts](../../general/index.md) in Entity Framework Core. This feature ensures that domain concepts are properly stored and retrieved from the database while maintaining type safety and domain integrity.

## What it does

The ConceptAs conversion automatically configures Entity Framework Core to handle properties that are of `ConceptAs<T>` type. When a concept property is encountered:

1. **Storage**: The underlying primitive value of the concept is stored in the database
2. **Retrieval**: The primitive value is automatically converted back to the concept instance when loaded from the database
3. **Comparison**: Proper value comparison is configured for change tracking and querying

With the conversion configured, entities keep their domain types while EF stores the underlying values. You do not need a separate converter for every concept.

## Why it's important

Using ConceptAs conversion provides several key benefits:

- **Domain Integrity**: Maintains strong typing and domain semantics throughout your application stack
- **Automatic Configuration**: No need for manual value converter setup for each concept property
- **Database Efficiency**: Stores only the primitive value, keeping database schema clean and performant
- **Type Safety**: Prevents mixing of different concept types that share the same underlying primitive type
- **Consistency**: Ensures all concept types are handled uniformly across your application

## Model Usage

Your entity models can use concepts directly without any special configuration:

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

With `BaseDbContext`, the conversion will automatically:

- Store `CustomerId` as a Guid value (as a string through Arc's SQLite converter)
- Store `CustomerName` and `EmailAddress` as `string` values in the database
- Convert back to the appropriate concept types when loading entities

## Manual Configuration

If you're not using the [`BaseDbContext`](./base-db-context.md), you can manually apply ConceptAs conversion in your `DbContext`:

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

The conversion system uses reflection to:

1. Identify all properties in your entities that implement `ConceptAs<T>`
2. Create appropriate `ValueConverter` instances that convert between the concept and its underlying primitive type
3. Configure `ValueComparer` instances for proper change tracking and equality comparisons
4. Apply these converters to the Entity Framework model builder

The conversion is handled by the `ConceptAsConversion.ApplyConceptAsConversion()` extension method, which automatically discovers and configures all concept properties in your model.
