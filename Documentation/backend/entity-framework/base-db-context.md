# Base Db Context

The `BaseDbContext` provides a pre-configured Entity Framework Core context that automatically applies common conventions and converters. This eliminates the need for manual configuration while ensuring consistent behavior across your application.

## Converters

The `BaseDbContext` provides automatic converter application when used with the Arc registration methods (`AddDbContextWithConnectionString` or `AddReadOnlyDbContext`). These methods configure all necessary services and interceptors at registration time to support the pooled factory pattern.

The `BaseDbContext` automatically applies converters to all `DbSet<>` types defined on the context and any types that are referenced by those entity types.

## How Converters Are Applied

The `BaseDbContext` determines which entity types should have converters applied. An entity type is considered relevant if it:

- Is an owned entity type
- Is directly exposed as a `DbSet<>` on the context
- Is referenced as a property or collection element by any type exposed as a `DbSet<>` on the context

This ensures that converters are applied not only to top-level entities but also to any related entities that are part of your domain model hierarchy.

### JSON Conversion

Automatically applies [JSON conversion](./json.md) for properties marked with the `[Json]` attribute, allowing complex objects to be stored as JSON in the database with cross-provider compatibility.

### ConceptAs Conversion

Automatically applies [ConceptAs conversion](./concept-as-conversion.md) for all properties that implement `ConceptAs<T>`, ensuring domain concepts are properly stored and retrieved while maintaining type safety.

### GUID Conversion

Automatically applies [GUID conversion](./guid-conversion.md) for all `Guid` properties, optimizing storage format and performance for each database provider.

## Usage

All you need to do is inherit from `BaseDbContext` and register it as you'd do with any other `DbContext`.

Take the following `DbContext`:

```csharp
using Cratis.Arc.EntityFrameworkCore;

public class StoreDbContext : BaseDbContext
{
    public DbSet<Customer> Customers { get; set; }
}
```

Then you register it as you normally would:

```csharp
services.AddDbContext<StoreDbContext>(opt => ...);
```

Or leveraging the [automatic database hookup](./automatic-database-hookup.md) extensions provided by Cratis Arc.

## Important: DbContext Pooling

The `BaseDbContext` is designed to work with the pooled factory pattern used by the Arc registration methods. When using `AddDbContextWithConnectionString` or `AddReadOnlyDbContext`, all configuration (service replacements, interceptors, observation support) is applied at registration time.

**Do not override `OnConfiguring`** in your derived DbContext classes when using pooled contexts, as EF Core does not allow modifying options in `OnConfiguring` when pooling is enabled. All configuration must be done during registration.

If you need custom configuration, pass it through the optional `optionsAction` parameter:

```csharp
services.AddDbContextWithConnectionString<StoreDbContext>(
    connectionString,
    opt => opt.EnableSensitiveDataLogging() // Custom configuration here
);
```
