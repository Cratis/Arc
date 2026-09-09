---
title: MongoDB tenancy
description: Preserve default database aliases and scope collections to the intended tenant.
---

For tenancy concepts and tenant resolution, see the [tenancy overview](../tenancy/index.md).

The default resolver returns the configured database for both `TenantId.NotSet` and `TenantId.Default` (`IsDefault`). For a named tenant it appends `+<tenant>` to the configured database name.

Collections and databases are scoped: resolve them after establishing the tenant context, and do not capture them in singleton services. Database naming is not authorization; tenant resolution and access checks must prevent unauthorized tenant selection.

The example below deliberately uses a tenant prefix instead of the default suffix, while preserving both default aliases. Apply naming changes as a data migration decision, not an incidental refactor.

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

        return tenantId.IsDefault
            ? baseName
            : $"{tenantId.Value}_{baseName}";
    }
}
```

## Registering the Resolver

```csharp
builder.UseCratisMongoDB(configureMongoDB: mongodb =>
{
    mongodb.WithDatabaseResolver<CustomMongoDatabaseNameResolver>();
});
```

## When to Use a Custom Resolver

- Match existing naming conventions.
- Add environment or region prefixes.
- Integrate with legacy database layouts.
- Implement specialized isolation or sharding rules.
