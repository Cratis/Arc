---
title: Register EF Core contexts and select a database in Arc
description: Register pooled EF Core contexts in Cratis Arc, select SQLite, PostgreSQL or SQL Server from a connection string, and control context discovery.
---

Cratis Arc selects SQLite, PostgreSQL or SQL Server from a connection string and registers pooled EF Core contexts. Register each context explicitly when connections or options differ, or scan assemblies when your read-only contexts share a connection.

Start with [getting started](./getting-started.md) to add the package and configure your Arc host. The examples here belong in that existing host. Supply your own context types, forward their options to the base constructor, and import `Cratis.Arc.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore`, and `Microsoft.Extensions.DependencyInjection`.

Provider detection is case-insensitive. SQLite strings use `Data Source=store.db` or `Filename=`; PostgreSQL requires both `Host=` and `Database=`; SQL Server requires `Server=` or `Data Source=` together with SQL Server-specific keywords such as `Initial Catalog=` or `Encrypt=`. Unrecognized strings throw `UnsupportedDatabaseType`. Selecting a provider does not apply migrations.

To use only provider detection with standard EF Core registration:

```csharp
services.AddDbContext<MyDbContext>(opt => opt.UseDatabaseFromConnectionString("Data Source=store.db"));
```

`UseDatabaseFromConnectionString` calls `.UseSqlite()`, `.UseNpgsql()` or `.UseSqlServer()` on the options builder. It does not add Arc's context registration services.

Use `AddDbContextWithConnectionString` when you also want Arc's mapping and query support:

```csharp
services.AddDbContextWithConnectionString<MyDbContext>("Data Source=store.db", (serviceProvider, opt) => opt.EnableDetailedErrors());
```

This helper registers a pooled factory and a scoped context created by that factory. It supplies the entity registrar used by `BaseDbContext`, installs `ConceptAs<T>` query support, and adds observation interceptors when observation services are registered.

## Multiple Database Providers

Different context types can use different database providers in the same application. For example, register one context with a SQLite connection string and another with a SQL Server connection string. Each context's options must select only one provider.

Arc's helpers register both `IDbContextFactory<TDbContext>` and a scoped `TDbContext` created from it. Pooling reuses context instances after they are disposed, so choose how to obtain a context based on who should own its lifetime.

### Using DbContext in Your Code

Inject the context when its lifetime should follow the service scope:

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

Inject `IDbContextFactory<T>` when an operation should create and dispose its own context:

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

Pooling reuses context instances and their options. Without [connection resolution](./connection-resolution.md), every tenant uses the supplied connection string. Configure resolution to select a tenant's database or identity when a connection opens; do not capture request-scoped tenant state in pooled options.

## Read Only DbContexts

For a `ReadOnlyDbContext` subclass, use the read-only helper to disable tracking and reject `SaveChanges`:

```csharp
services.AddReadOnlyDbContextWithConnectionString<MyDbContext>("Data Source=store.db", (serviceProvider, opt) => opt.EnableDetailedErrors());
```

## Automatic Registration from Assemblies

Pass the assemblies you want to scan and a shared connection or options callback. These methods register `ReadOnlyDbContext` subclasses, not unrelated context classes:

```csharp
// Register all ReadOnlyDbContext types from assemblies with a common options action
services.AddReadModelDbContextsFromAssemblies((serviceProvider, opt) => opt.UseDatabaseFromConnectionString(connectionString), assembly1, assembly2);

// Register all ReadOnlyDbContext types from assemblies with a connection string
services.AddReadModelDbContextsWithConnectionStringFromAssemblies(connectionString, (serviceProvider, opt) => { /* additional options */ }, assembly1, assembly2);
```

### Registration Filtering Rules

Assembly scanning includes exported, concrete `ReadOnlyDbContext` subclasses from the assemblies you pass. It excludes abstract classes and classes marked with `IgnoreAutoRegistrationAttribute`.

The builder's `WithEntityFrameworkCore` discovery is broader: it finds public `BaseDbContext` subclasses and gives read-only subclasses read-only registration. It runs only when `AutoDiscoverDbContexts` is enabled and the configured connection string is nonempty.

### Excluding DbContexts from Automatic Registration

Add `IgnoreAutoRegistrationAttribute` when a context needs an explicit connection or options instead of the shared discovery settings:

```csharp
using Cratis.Arc;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

[IgnoreAutoRegistration]
public class SpecialDbContext(DbContextOptions<SpecialDbContext> options) : ReadOnlyDbContext(options)
{
}
```

The attribute excludes the context from both assembly scanning and builder discovery; register it explicitly if the application needs it. Once registration is in place, configure [entity mappings](./entity-mapping.md) for its tables, keys and relationships.
