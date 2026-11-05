---
title: Select MongoDB databases by tenant
description: Cratis Arc selects a MongoDB database from the current tenant. Keep default tenants on the base database or customize naming for your existing layout.
---

Cratis Arc selects a MongoDB database using the current tenant: default tenants use the configured database name, and named tenants use a tenant-specific suffix. This separates each named tenant's collections into its own database without changing the code that requests a collection.

Establish the tenant context before resolving a database or collection. Both are scoped services, so a singleton must not capture them; otherwise it can keep using the database selected for an earlier scope. Database naming does not authorize access. Tenant resolution and access checks must prevent unauthorized tenant selection. See the [tenancy overview](../tenancy/index.md) for that setup, or [tenancy end to end](../tenancy/end-to-end.md) to compare MongoDB with EF Core and Chronicle.

With the configured database name `app`, the default resolver chooses:

| Current tenant | Database |
| --- | --- |
| `TenantId.NotSet` | `app` |
| `TenantId.Default` | `app` |
| A named tenant with value `north` | `app+north` |

`tenantId.IsDefault` covers both default aliases. Preserve that behavior in a custom resolver so default-tenant reads still reach the database where their data is stored. Choosing a different name can return empty results without an error if that database does not exist.

## Custom Database Resolvers

Implement `IMongoDatabaseNameResolver` if your existing databases use a different naming pattern. Apply naming changes as a data migration decision: a resolver selects a database name but does not move existing data.

## Custom Resolver Example

This resolver uses a tenant prefix instead of Arc's suffix. A named tenant `north` with base database `app` resolves to `north_app`; both default aliases still resolve to `app` because the branch checks `IsDefault`.

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

Register the resolver in the [MongoDB host configuration](./getting-started.md) before resolving databases or collections:

```csharp
builder.UseCratisMongoDB(configureMongoDB: mongodb =>
{
    mongodb.WithDatabaseResolver<CustomMongoDatabaseNameResolver>();
});
```

## When to Use a Custom Resolver

Use a custom resolver to match existing database names or include an environment or region in the name. Check the result for both default aliases and a named tenant before directing application traffic to the new layout.

A name resolver only changes the database name; it does not configure sharding or change the server or credentials. For tenant-specific servers or credentials, continue with [connection resolution](./connection-resolution.md).
