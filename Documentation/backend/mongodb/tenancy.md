# Tenancy

For tenancy concepts and tenant resolution, see the [tenancy overview](../tenancy/overview.md).

This page focuses on MongoDB-specific tenant database naming through a custom database name resolver.

## Custom Database Resolvers

You can create a custom database naming strategy by implementing a database name resolver and registering it with the MongoDB integration.

## Custom Resolver Example

```csharp
using Cratis.Arc.MongoDB;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;

public class CustomMongoDatabaseNameResolver(
    ITenantIdAccessor tenantIdAccessor,
    IOptions<MongoDBOptions> options) : IMongoDatabaseNameResolver
{
    public string Resolve()
    {
        var baseName = options.Value.Database;
        var tenantId = tenantIdAccessor.Current;

        return tenantId == TenantId.NotSet
            ? baseName
            : $"{tenantId.Value}_{baseName}";
    }
}
```

## Registering the Resolver

```csharp
builder.AddCratisArcMongoDB(mongodb =>
{
    mongodb.WithDatabaseNameResolver<CustomMongoDatabaseNameResolver>();
});
```

## When to Use a Custom Resolver

- Match existing naming conventions.
- Add environment or region prefixes.
- Integrate with legacy database layouts.
- Implement specialized isolation or sharding rules.

