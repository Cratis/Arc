# Property Extensions

Cratis Arc provides extension methods for Entity Framework Core's `PropertyBuilder` to help configure properties with cross-database compatibility in mind.

## AsGuid()

The `AsGuid()` extension method configures a GUID property for optimal storage based on the database provider, ensuring compatibility across different database providers while using native types when available.

> **Note**: You should use this in conjunction with the migration-based column type configuration, see [Common Column Types](./common-column-types.md) which provides the `GuidColumn()` method for creating GUID columns in migrations.

### Why AsGuid()?

Different database providers handle GUIDs differently:

- **PostgreSQL**: Native UUID type with optimal performance
- **SQL Server**: UNIQUEIDENTIFIER type with optimal performance  
- **SQLite**: No native GUID support, requires string conversion

The `AsGuid()` method uses the provided database information to apply the appropriate configuration:

- For **SQLite**: Converts GUIDs to strings using the "D" format (e.g., `550e8400-e29b-41d4-a716-446655440000`)
- For **PostgreSQL and SQL Server**: Uses native GUID types for optimal performance

This provides the best of both worlds: optimal performance on databases with native GUID support and compatibility for SQLite.

### Usage

#### Recommended Usage

```csharp
public class MyDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id)
                .AsGuid(Database.GetDatabaseType()); // Pass the database type for proper provider detection
        });
    }
}
```

> **Important**: The `database` parameter is required to ensure optimal configuration based on the actual database provider being used.

### What it does

The `AsGuid()` method:

1. **Uses the provided database parameter** to determine the database provider (PostgreSQL, SQL Server, or SQLite)
2. **For SQLite**: Converts `Guid` values to string using the `ToString("D")` format for storage and parses them back when reading
3. **For PostgreSQL and SQL Server**: Uses native GUID types without conversion for optimal performance
4. **Ensures optimal behavior** for each supported database provider

### When to use

Use `AsGuid()` when:

- You need consistent GUID behavior across multiple database providers
- Your application might be deployed with different database backends
- You want optimal performance on databases with native GUID support
- You want automatic SQLite compatibility with proper provider detection

### Why the Database parameter is required

The `database` parameter is required because:

- **Ensures optimal performance**: Native GUID types are used for PostgreSQL and SQL Server, string conversion only for SQLite
- **Explicit provider detection**: Eliminates guesswork and ensures the correct configuration is applied
- **Better debugging**: Makes it clear which database provider configuration is being used
- **Prevents misconfiguration**: Avoids scenarios where incorrect assumptions about the database provider could lead to suboptimal storage

## AsConcept()

The `AsConcept()` extension method configures a property to use value conversion for concept types based on `ConceptAs<T>`. This method provides automatic conversion between domain concepts and their underlying primitive values for database storage.

> **Note**: This method is typically used for manual property configuration. In most cases, you should use the automatic conversion provided by the [`BaseDbContext`](./base-db-context.md) or the [`ApplyConceptAsConversion`](./concept-as-conversion.md) extension method.

### Why AsConcept()?

When working with domain concepts (types that inherit from `ConceptAs<T>`), you need proper value conversion to:

- **Store primitive values**: Only the underlying primitive value is stored in the database, keeping the schema clean and performant
- **Maintain type safety**: Ensures concepts are properly converted back to their domain types when loading from the database
- **Handle different databases**: Provides special handling for GUID concepts on SQLite (using string conversion) while using native types on other databases
- **Enable change tracking**: Configures proper value comparison for Entity Framework's change tracking system

### AsConcept() Usage

#### Manual Property Configuration

```csharp
public class StoreDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.Id)
                .AsConcept(Database.GetDatabaseType()); // Configure concept property manually
            
            entity.Property(e => e.Name)
                .AsConcept(Database.GetDatabaseType()); // Works with any ConceptAs<T> type
            
            entity.Property(e => e.Email)
                .AsConcept(Database.GetDatabaseType());
        });
    }
}

public class Customer
{
    public CustomerId Id { get; set; }
    public CustomerName Name { get; set; }
    public EmailAddress Email { get; set; }
}

public record CustomerId(Guid Value) : ConceptAs<Guid>(Value);
public record CustomerName(string Value) : ConceptAs<string>(Value);
public record EmailAddress(string Value) : ConceptAs<string>(Value);
```

> **Important**: The `database` parameter is required to ensure optimal configuration based on the actual database provider being used.

### How AsConcept() works

The `AsConcept()` method:

1. **Detects concept types**: Automatically identifies if the property is a `ConceptAs<T>` type
2. **Creates value converters**: Generates appropriate `ValueConverter` instances that convert between concepts and their primitive values
3. **Handles GUID concepts specially**: For GUID-based concepts on SQLite, uses string conversion; for other databases, uses native GUID storage
4. **Configures value comparison**: Sets up proper `ValueComparer` for change tracking and equality operations
5. **Returns early for non-concepts**: If the property is not a concept type, returns without modification

### When to use AsConcept()

Use `AsConcept()` when:

- You need manual control over individual concept property configuration
- You're not using the [`BaseDbContext`](./base-db-context.md) which provides automatic concept conversion
- You want to selectively configure only specific concept properties
- You're working with a custom `DbContext` that doesn't apply global concept conversion

### When NOT to use

Avoid `AsConcept()` when:

- You're using [`BaseDbContext`](./base-db-context.md) - concepts are automatically configured
- You're using [`ApplyConceptAsConversion()`](./concept-as-conversion.md) - all concepts are configured automatically
- The property is not a concept type - the method will return without changes but it's unnecessary overhead

### Special handling for GUID concepts

For concept types based on `Guid` (e.g., `ConceptAs<Guid>`):

- **SQLite**: Converts to string using "D" format for storage compatibility
- **PostgreSQL and SQL Server**: Uses native GUID storage for optimal performance

This ensures consistent behavior across database providers while maintaining optimal performance where native GUID support is available.
