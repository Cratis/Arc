---
title: Connection resolution
description: Resolve the Entity Framework Core connection string, database and identity per tenant, including access tokens and separate identities for migrations.
---

By default, every Arc-registered `DbContext` connects with the one configured connection string. Use connection resolution when each tenant's database sits on its own server or database, or connects with its own identity. For the concepts and the resolvers, read [connection resolution](../tenancy/connection-resolution.md) first; for the endpoint an `Http` resolver calls, see the [connection resolution contract](../../../connection-resolution-contract.md).

This page configures resolution for Entity Framework Core only. To configure it once for every storage, set it on Arc instead, as described in [configure it once for the whole application](../tenancy/connection-resolution.md#configure-it-once-for-the-whole-application). What you configure here replaces the application-wide configuration for Entity Framework Core.

## Configure it

`EntityFrameworkCoreOptions` bind from the `Cratis:EntityFrameworkCore` section. The `configureOptions` callback runs afterwards and wins.

```json
{
  "Cratis": {
    "EntityFrameworkCore": {
      "ConnectionString": "Server=sql.example;Database=app;Encrypt=true",
      "ConnectionResolution": {
        "Connection": {
          "Resolver": "Template",
          "Template": { "Database": "tenant-{tenant}" }
        },
        "Identity": {
          "Resolver": "Http",
          "Http": { "Endpoint": "https://platform.example/connections" }
        }
      }
    }
  }
}
```

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithEntityFrameworkCore());
```

The same configuration in code:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithEntityFrameworkCore(configureOptions: options =>
{
    options.ConnectionString = "Server=sql.example;Database=app;Encrypt=true";
    options.ConnectionResolution.Connection.UseTemplate(database: "tenant-{tenant}");
    options.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"));
}));
```

## How it works with pooled contexts

Arc resolves when a connection **opens**, not when a context is created. A pooled context keeps its connection object between leases, but every lease opens it again for whoever holds the lease. Arc applies the resolution over the configured connection string each time, never over one resolved for an earlier tenant. Contexts you register through Arc's helpers (`AddDbContextWithConnectionString`, `AddReadOnlyDbContext`, discovery, or `IEntityFrameworkCoreBuilder.AddDbContext`) get this automatically.

| Resolved part | SQL Server | PostgreSQL | SQLite |
| --- | --- | --- | --- |
| `ConnectionString` | Replaces the configured one | Replaces the configured one | Replaces the configured one |
| `Database` | `Initial Catalog` | `Database` | `Data Source` (the file) |
| `Identity.UserName` / `Password` | `User ID` / `Password` | `Username` / `Password` | Ignored |
| `Identity.AccessToken` | `SqlConnection.AccessToken` (Entra ID) | Used as the password | Ignored |
| `Schema` | Not applied | Not applied | Not applied |

For passwordless Entra ID with a managed identity, you need no token at all. Resolve a connection string that carries `Authentication=Active Directory Managed Identity;User Id=<client id>`, and the SQL client signs in on its own. With `Microsoft.Data.SqlClient` 7.x, the `Active Directory` authentication modes need the `Microsoft.Data.SqlClient.Extensions.Azure` package.

:::caution[Do not combine an access token with a password]
SQL Server refuses an access token on a connection that also has a password or `Integrated Security`. Resolve one or the other.
:::

When opening fails to authenticate (SQL Server error 18456, PostgreSQL `28000` or `28P01`), Arc drops the cached resolution. That open still fails, and the next one resolves again.

## Run migrations as a different identity

Resolution happens for `Runtime` by default. Wrap schema changes so your resolver can hand out an identity that may alter the schema:

```csharp
using (ConnectionPurposeScope.Begin(ConnectionPurpose.Schema))
{
    await dbContext.Database.MigrateAsync();
}
```

The resolver receives `"purpose": "Schema"`, and a template can use `{purpose}`.

## Limits

- The connection string the context reports is the one last applied. Pass connection strings through resolution rather than combining it with `Database.SetConnectionString`.
- SQL Server change notifications for [observing](./observing.md) open their own connection from the reported connection string, which carries no access token.
