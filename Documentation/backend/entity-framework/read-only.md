# Read Only DbContext

Typically in a CQRS model, your read models are not meant to be used for creating or updating state in the
database. They're meant to be read-only. In fact, you don't even want to take advantage of the EntityFramework change tracking.

There are 2 ways of doing read-only DbContexts; inheritance or using the registration methods that will configure without
having to use inheritance.

## Purpose-Built Read Models

A core principle of effective read model design is that **each read model should be purpose-built for a specific scenario** rather than reused across multiple use cases. This means:

- Each `DbContext` represents a specific view or feature's data needs
- The entities and their relationships are tailored to exactly what that scenario requires
- You don't share read models between different features that have different relationship requirements

This approach offers several benefits:

- **Clarity**: Each read model clearly expresses what data a specific feature needs
- **Performance**: No unnecessary data is loaded, and no conditional logic is needed to decide which relationships to include
- **Maintainability**: Changes to one feature's data needs don't affect other features
- **Simplicity**: The code remains straightforward without complex `.Include()` chains or conditional loading logic

By following this pattern, automatic eager loading (described below) becomes a natural fit—since each read model is purpose-built, all its relationships are needed every time, eliminating the need for selective inclusion.

## ReadOnlyDbContext base class

The `ReadOnlyDbContext` base class gives you a base class that also inherits from the [`BaseDbContext`](./base-db-context.md) to
give you the common tools.

All you need to do for your `DbContext` is to inherit from it as shown below:

```csharp
using Cratis.Arc.EntityFrameworkCore;

public class StoreDbContext : ReadOnlyDbContext
{
    public DbSet<Customer> Customers { get; set; }
}
```

Then you register it as you normally would:

```csharp
services.AddDbContext<StoreDbContext>(opt => ...);
```

## Automatic Eager Loading

The `ReadOnlyDbContext` automatically enables eager loading for all navigation properties across all entities. This means that when you query an entity, all its related entities will be automatically included without having to explicitly call `.Include()`.

This behavior aligns perfectly with the purpose-built read model approach—since each DbContext is designed for a specific scenario with well-defined data needs, all relationships are typically required and should be loaded together.

### Disabling Eager Loading Globally

If you want to disable automatic eager loading for a specific DbContext, you can override the `IsEagerLoadingEnabled` property:

```csharp
using Cratis.Arc.EntityFrameworkCore;

public class StoreDbContext : ReadOnlyDbContext
{
    public DbSet<Customer> Customers { get; set; }
    
    protected override bool IsEagerLoadingEnabled => false;
}
```

### Disabling Eager Loading Per Query

For individual queries where you don't want eager loading, use the `.IgnoreAutoIncludes()` method:

```csharp
// This query will NOT load related entities automatically
var customers = await context.Customers
    .IgnoreAutoIncludes()
    .ToListAsync();
```

### Disabling Eager Loading Per Entity

To disable automatic eager loading for specific navigation properties in your entity configuration, you can use the `AutoInclude(false)` method in your entity configuration:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    modelBuilder.Entity<Customer>()
        .Navigation(c => c.Orders)
        .AutoInclude(false);
}
```

This will prevent the `Orders` navigation from being automatically included when querying `Customer` entities, even though eager loading is enabled globally.

## Register

The other option is to let you inherit from whatever base `DbContext` you want and then instead leverage the extension methods:

```csharp
services.AddReadOnlyDbContext<StoreDbContext>((serviceProvider, opt) => ...);
```

### Discover and register all in assemblies

For convenience you can get all types inheriting from `DbContext` automatically discovered and registered in one call.

```csharp
services.AddReadModelDbContextsFromAssemblies((serviceProvider, opt) =>
{
    /* Configure any options */
},
[Assembly.GetExecutingAssembly()]);
```

Or if you want it to automatically configure it with the correct database:

```csharp
services.AddReadModelDbContextsWithConnectionStringFromAssemblies(
    ".. your connection string..",
    (serviceProvider, opt) =>
    {
        /* Configure any options */
    },
    [Assembly.GetExecutingAssembly()]);
```
