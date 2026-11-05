---
title: Resolve EF Core connections per tenant in Cratis Arc
description: Configure Cratis Arc to resolve an EF Core database and identity per tenant when a connection opens, with a separate identity for migrations.
---

Cratis Arc's EF Core connection resolution selects a tenant's database and connection identity when a connection opens. Use it when tenants need separate databases, servers or credentials. Without resolution, each Arc-registered `DbContext` uses its configured connection string for every tenant.

Choose the resolver using the [connection resolution guide](../tenancy/connection-resolution.md). If you already configure resolution once for every storage, keep that application-wide setup as described in [configure it once for the whole application](../tenancy/connection-resolution.md#configure-it-once-for-the-whole-application). Configure the EF Core options below only when you want to replace that setup for EF Core. For an `Http` resolver's endpoint, use the [connection resolution contract](../../../connection-resolution-contract.md).

## Configure it

Set `ConnectionResolution` under `Cratis:EntityFrameworkCore`. This example keeps the server from the configured connection string, names each database `tenant-{tenant}`, and asks an HTTP endpoint for the connection identity:

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

`EntityFrameworkCoreOptions` bind from that configuration section before `configureOptions` runs. Use the callback to set the same values in code or override configuration:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithEntityFrameworkCore(configureOptions: options =>
{
    options.ConnectionString = "Server=sql.example;Database=app;Encrypt=true";
    options.ConnectionResolution.Connection.UseTemplate(database: "tenant-{tenant}");
    options.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"));
}));
```

## How it works with pooled contexts

A pooled context can be reused by another request, so Arc resolves the connection when it opens rather than when the context is created. It applies each resolution over the originally configured connection string, not one resolved for an earlier tenant. Registering through Arc's helpers (`AddDbContextWithConnectionString`, `AddReadOnlyDbContext`, discovery, or `IEntityFrameworkCoreBuilder.AddDbContext`) installs the interceptor that does this when resolution is configured.

The resolved fields affect each provider as follows:

| Resolved part | SQL Server | PostgreSQL | SQLite |
| --- | --- | --- | --- |
| `ConnectionString` | Replaces the configured one | Replaces the configured one | Replaces the configured one |
| `Database` | `Initial Catalog` | `Database` | `Data Source` (the file) |
| `Identity.UserName` / `Password` | `User ID` / `Password` | `Username` / `Password` | Ignored |
| `Identity.AccessToken` | `SqlConnection.AccessToken` (Entra ID) | Used as the password | Ignored |
| `Schema` | Not applied | Not applied | Not applied |

For passwordless Entra ID with a managed identity, the resolver does not need to return an access token. Resolve a connection string that carries `Authentication=Active Directory Managed Identity;User Id=<client id>`, and the SQL client handles authentication. With `Microsoft.Data.SqlClient` 7.x, the `Active Directory` authentication modes need the `Microsoft.Data.SqlClient.Extensions.Azure` package.

:::caution[Do not combine an access token with a password]
SQL Server refuses an access token on a connection that also has a password or `Integrated Security`. Resolve one or the other.
:::

If opening fails to authenticate (SQL Server error 18456, PostgreSQL `28000` or `28P01`), Arc drops the cached resolution so the next open can pick up rotated credentials. The failed open is not retried by this invalidation; the next one resolves again.

## Run migrations as a different identity

Keep runtime credentials separate from credentials allowed to change the schema. Resolution uses `Runtime` by default. Wrap a migration in `ConnectionPurposeScope` so the resolver can return a schema-changing identity for that operation. Import `Cratis.Arc.Connections`:

```csharp
using (ConnectionPurposeScope.Begin(ConnectionPurpose.Schema))
{
    await dbContext.Database.MigrateAsync();
}
```

Inside this scope, the resolver receives `"purpose": "Schema"`; a template can also use `{purpose}`. Configure the resolver to distinguish these purposes rather than giving the runtime identity schema-changing permissions.

## Limits

- The connection string the context reports is the one last applied. Pass connection strings through resolution rather than combining it with `Database.SetConnectionString`.
- SQL Server change notifications for [observing](./observing.md) open their own connection from the reported connection string, which carries no access token.
