# Getting Started with Entity Framework Core

Arc provides two approaches for integrating Entity Framework Core into your application:

1. **Builder Pattern** (`WithEntityFrameworkCore`) - Recommended when using Arc's `IArcBuilder` with full auto-discovery and observation support
2. **Direct Registration** - Flexible approach that works independently of Arc

## Builder Pattern (Recommended)

When using Arc's application framework, the `WithEntityFrameworkCore()` extension method provides the most streamlined setup with automatic DbContext discovery and observation support.

### Basic Setup

```csharp
builder.AddCratisArc(configureBuilder: arcBuilder =>
{
    arcBuilder.WithEntityFrameworkCore(options =>
    {
        options.ConnectionString = "Server=localhost;Database=MyDb;Trusted_Connection=true";
    });
});
```

This single configuration:

- **Automatically discovers** all DbContext types inheriting from `BaseDbContext` or `ReadOnlyDbContext`
- **Registers them** with the connection string using appropriate patterns (read-only vs read-write)
- **Enables observation** support for real-time change notifications
- **Applies** all Arc conventions (ConceptAs support, entity mapping, etc.)

### Configuration Options

The `EntityFrameworkCoreOptions` class provides the following configuration:

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `ConnectionString` | `string` | `""` | The database connection string. Required for auto-discovery. |
| `AutoDiscoverDbContexts` | `bool` | `true` | Whether to automatically discover and register DbContext types. |

### Auto-Discovery

When `AutoDiscoverDbContexts` is enabled (default), the framework automatically:

1. Scans for all types inheriting from `BaseDbContext`
2. Identifies which ones are `ReadOnlyDbContext` subtypes
3. Registers read-only contexts with `AddReadOnlyDbContextWithConnectionString`
4. Registers read-write contexts with `AddDbContextWithConnectionString`
5. Excludes any types marked with `[IgnoreAutoRegistration]`

```csharp
// Your DbContext types - automatically discovered and registered
public class OrdersDbContext : BaseDbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }
    
    public DbSet<Order> Orders { get; set; }
}

public class ReportingDbContext : ReadOnlyDbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }
    
    public DbSet<OrderSummary> OrderSummaries { get; set; }
}
```

### Disabling Auto-Discovery

If you need manual control over DbContext registration, disable auto-discovery:

```csharp
builder.AddCratisArc(configureBuilder: arcBuilder =>
{
    arcBuilder.WithEntityFrameworkCore(
        configureOptions: options =>
        {
            options.ConnectionString = "Server=localhost;Database=MyDb;Trusted_Connection=true";
            options.AutoDiscoverDbContexts = false;
        },
        configureEfCore: efBuilder =>
        {
            // Manual registration with custom options
            efBuilder.AddDbContext<OrdersDbContext>((sp, opts) =>
            {
                opts.EnableSensitiveDataLogging();
            });
        });
});
```

### Using the Builder for Manual Registration

The `IEntityFrameworkCoreBuilder` provides methods for manual DbContext registration:

```csharp
arcBuilder.WithEntityFrameworkCore(
    configureOptions: options =>
    {
        options.ConnectionString = "Server=localhost;Database=MyDb;Trusted_Connection=true";
        options.AutoDiscoverDbContexts = false;
    },
    configureEfCore: efBuilder =>
    {
        // Use connection string from options
        efBuilder.AddDbContext<OrdersDbContext>();
        
        // Or specify a different connection string
        efBuilder.AddDbContext<ArchiveDbContext>("Server=archive;Database=Archive;Trusted_Connection=true");
    });
```

### Excluding Types from Auto-Discovery

Use the `[IgnoreAutoRegistration]` attribute to exclude specific DbContext types:

```csharp
[IgnoreAutoRegistration]
public class TestDbContext : BaseDbContext
{
    // This context won't be auto-registered
}
```

## Direct Registration (Flexible Approach)

If you're not using Arc's builder pattern or need more flexibility, you can register Entity Framework Core support directly on `IServiceCollection`. This approach doesn't require the full Arc framework.

### Basic Direct Registration

```csharp
// Add observation services (optional, but recommended)
services.AddEntityFrameworkCoreObservation();

// Register your DbContext with connection string
services.AddDbContextWithConnectionString<OrdersDbContext>(
    "Server=localhost;Database=MyDb;Trusted_Connection=true");
```

### Direct Registration with Options

```csharp
services.AddDbContextWithConnectionString<OrdersDbContext>(
    "Server=localhost;Database=MyDb;Trusted_Connection=true",
    (serviceProvider, options) =>
    {
        options.EnableSensitiveDataLogging();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    });
```

### Read-Only DbContext Registration

```csharp
services.AddReadOnlyDbContextWithConnectionString<ReportingDbContext>(
    "Server=localhost;Database=MyDb;Trusted_Connection=true");
```

### Assembly-Based Discovery

For direct registration with assembly scanning:

```csharp
// Discover and register all ReadOnlyDbContext types from assemblies
services.AddReadModelDbContextsWithConnectionStringFromAssemblies(
    "Server=localhost;Database=MyDb;Trusted_Connection=true",
    optionsAction: null,
    typeof(Program).Assembly,
    typeof(OrdersDbContext).Assembly);
```

## Choosing the Right Approach

| Feature | Builder Pattern | Direct Registration |
| ------- | --------------- | ------------------- |
| Auto-discovery | ✅ Built-in | ⚠️ Assembly-based only |
| Observation support | ✅ Automatic | ⚠️ Manual setup required |
| Arc integration | ✅ Full | ❌ Not required |
| Flexibility | Good | Maximum |
| Configuration | Centralized | Distributed |

**Use Builder Pattern when:**

- You're using Arc's `IArcBuilder` pattern
- You want automatic DbContext discovery
- You want observation support without extra configuration
- You prefer centralized configuration

**Use Direct Registration when:**

- You're not using the full Arc framework
- You need maximum flexibility in registration
- You're integrating with an existing application
- You want fine-grained control over each DbContext

## Important: Registration Order

When using direct registration with observation support, ensure you register observation services **before** calling `AddCratisArc()`:

```csharp
// Register observation services first
services.AddEntityFrameworkCoreObservation();

// Then register your DbContexts
services.AddDbContextWithConnectionString<OrdersDbContext>(connectionString);

// Finally, add Arc
builder.AddCratisArc();
```

This ensures the singleton `IEntityChangeTracker` is properly shared across all interceptors.

> **Note**: When using `WithEntityFrameworkCore()`, this ordering is handled automatically.

## Next Steps

- [Base DbContext](./base-db-context.md) - Learn about the base DbContext class
- [Read Only DbContexts](./read-only.md) - Implement read-only contexts for queries
- [Observing DbSet](./observing.md) - Create reactive queries with real-time updates
- [Entity Mapping](./entity-mapping.md) - Configure entities using clean patterns
