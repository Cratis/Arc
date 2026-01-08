# Automatic Database Hookup

Out of the box we support the following databases:

- Sqlite
- PostgreSQL
- Microsoft SQL Server

There are different extension methods for adding `DbContext` types and also for resolving the correct database provider based on connection string.

You can use the standard EF Core method with the Arc database detection extension:

```csharp
services.AddDbContext<MyDbContext>(opt => opt.UseDatabaseFromConnectionString(".. your connection string.."));
```

> Note: From the connection string it will do the correct `.UseSqlite()`, `.UseNpgsql()` or `.UseSqlServer()` call on the builder.

However, **it is recommended** to use the Arc registration methods which use the pooled factory pattern for better performance and to support multiple database providers:

```csharp
services.AddDbContextWithConnectionString<MyDbContext>(".. your connection string..", opt => /* do whatever configuration you want */);
```

This method automatically:

- Uses `AddPooledDbContextFactory` for improved performance
- Applies all `BaseDbContext` configurations (interceptors, service replacements)
- Registers both the factory and a scoped DbContext instance
- Supports multiple database providers in the same application

## Multiple Database Providers

The Arc Entity Framework integration uses the **pooled factory pattern** (`IDbContextFactory<T>`) internally to support multiple database providers in the same application. This is important because Entity Framework Core does not allow multiple database providers to be registered in the same service provider.

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

> **Important**: When using multiple DbContexts with different database providers (e.g., SQLite for testing and SQL Server for production), the factory pattern ensures each DbContext gets its own isolated service provider, preventing conflicts between providers.

## Read Only DbContexts

For any **read-only** `DbContext` there is also an extension method:

```csharp
services.AddReadOnlyDbContextWithConnectionString<MyDbContext>(".. your connection string..", opt => /* do whatever configuration you want */);
```

## Automatic Registration from Assemblies

The framework provides methods to automatically discover and register all `ReadOnlyDbContext` types from specified assemblies:

```csharp
// Register all ReadOnlyDbContext types from assemblies with a common options action
services.AddReadModelDbContextsFromAssemblies(opt => opt.UseDatabaseFromConnectionString(connectionString), assembly1, assembly2);

// Register all ReadOnlyDbContext types from assemblies with a connection string
services.AddReadModelDbContextsWithConnectionStringFromAssemblies(connectionString, opt => /* additional options */, assembly1, assembly2);
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

[IgnoreAutoRegistration]
public class SpecialDbContext : ReadOnlyDbContext
{
    // This DbContext will be ignored during automatic registration
    // and must be registered manually if needed
}
```

This is useful for scenarios where:

- The DbContext requires special configuration
- You want to register it with different lifetime scopes
- It's used only in specific conditions
- You want to register it manually with custom options
