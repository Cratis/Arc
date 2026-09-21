---
title: Automatic database hookup
description: Register pooled EF contexts and select a supported provider from a connection string.
---

Out of the box we support the following databases:

- Sqlite
- PostgreSQL
- Microsoft SQL Server

The examples below are registration or service fragments for an existing host; supply the named contexts and import `Cratis.Arc.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore`, and `Microsoft.Extensions.DependencyInjection`. Contexts must forward their options to their base constructor. Start with [getting started](./getting-started.md) for the optional package and Arc setup.

Database hookup selects a provider; it does not apply migrations. SQLite strings use `Data Source=store.db`; PostgreSQL detection requires `Host=` and `Database=`; SQL Server detection looks for server/data-source and SQL Server-specific keywords. Unrecognized strings throw `UnsupportedDatabaseType`.

You can use the standard EF Core method with the Arc database detection extension:

```csharp
services.AddDbContext<MyDbContext>(opt => opt.UseDatabaseFromConnectionString("Data Source=store.db"));
```

> Note: From the connection string it will do the correct `.UseSqlite()`, `.UseNpgsql()` or `.UseSqlServer()` call on the builder.

However, **it is recommended** to use the Arc registration methods which use the pooled factory pattern for better performance and to support multiple database providers:

```csharp
services.AddDbContextWithConnectionString<MyDbContext>("Data Source=store.db", (serviceProvider, opt) => opt.EnableDetailedErrors());
```

This method automatically:

- Uses `AddPooledDbContextFactory` for improved performance
- Applies all `BaseDbContext` configurations (interceptors, service replacements)
- Registers both the factory and a scoped DbContext instance
- Supports multiple database providers in the same application

## Multiple Database Providers

The Arc Entity Framework integration uses the **pooled factory pattern** (`IDbContextFactory<T>`) internally. Different context types can use different database providers. A single context's options must select only one provider; this is not a prohibition on multiple providers anywhere in the application's DI container.

The pooled factory approach provides:

- **Multiple database provider support** - Different contexts can use different databases (SQLite, SQL Server, PostgreSQL)
- **Improved performance** - DbContext instances are pooled and reused
- **Reduced memory overhead** - Internal service providers are shared across pooled instances

All registration methods automatically register both:

- `IDbContextFactory<TDbContext>` - For creating DbContext instances
- `TDbContext` - Scoped instance created from the factory

### Using DbContext in Your Code

You can inject DbContexts directly as you normally would:

```csharp
public class MyService
{
    private readonly MyDbContext _context;
    
    public MyService(MyDbContext context)
    {
        _context = context;
    }
}
```

Or you can use `IDbContextFactory<T>` when you need more control over the DbContext lifetime:

```csharp
public class MyService
{
    private readonly IDbContextFactory<MyDbContext> _contextFactory;
    
    public MyService(IDbContextFactory<MyDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    public async Task ProcessAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Use context
    }
}
```

> **Important**: Pooling reuses context instances and their options. These helpers capture the supplied connection string; they do not implement tenant-aware database selection. Do not capture request-scoped tenant state in pooled configuration. Test any application-supplied isolation strategy across pooled reuse.

## Read Only DbContexts

For any **read-only** `DbContext` there is also an extension method:

```csharp
services.AddReadOnlyDbContextWithConnectionString<MyDbContext>("Data Source=store.db", (serviceProvider, opt) => opt.EnableDetailedErrors());
```

## Automatic Registration from Assemblies

The framework provides methods to automatically discover and register all `ReadOnlyDbContext` types from specified assemblies:

```csharp
// Register all ReadOnlyDbContext types from assemblies with a common options action
services.AddReadModelDbContextsFromAssemblies((serviceProvider, opt) => opt.UseDatabaseFromConnectionString(connectionString), assembly1, assembly2);

// Register all ReadOnlyDbContext types from assemblies with a connection string
services.AddReadModelDbContextsWithConnectionStringFromAssemblies(connectionString, (serviceProvider, opt) => { /* additional options */ }, assembly1, assembly2);
```

### Registration Filtering Rules

When using automatic registration, the framework applies the following filtering rules:

1. **Public Classes Only**: Only `public` DbContext classes will be automatically registered. Internal, private, or protected classes are ignored.

2. **Assembly Membership**: Only DbContext classes that belong to the specified assemblies will be considered for registration.

3. **Attribute-Based Exclusion**: Classes marked with the `IgnoreAutoRegistrationAttribute` will be excluded from automatic registration.

### Excluding DbContexts from Automatic Registration

If you have a DbContext that should not be automatically registered (for example, if it requires special configuration or should be registered manually), you can exclude it using the `IgnoreAutoRegistrationAttribute`:

```csharp
using Cratis.Arc;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

[IgnoreAutoRegistration]
public class SpecialDbContext(DbContextOptions<SpecialDbContext> options) : ReadOnlyDbContext(options)
{
}
```

This is useful for scenarios where:

- The DbContext requires special configuration
- You want to register it with different lifetime scopes
- It's used only in specific conditions
- You want to register it manually with custom options
