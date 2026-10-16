---
title: Connection resolution
description: Resolve the MongoDB server, database and identity per tenant through configuration, a template, an HTTP endpoint or your own resolver.
---

Use connection resolution when each tenant's MongoDB database lives somewhere your platform decides, or authenticates with its own credentials. For the concepts and the resolvers, read [connection resolution](../tenancy/connection-resolution.md) first; for the endpoint an `Http` resolver calls, see the [connection resolution contract](../../../connection-resolution-contract.md).

This page configures resolution for MongoDB only. To configure it once for every storage, set it on Arc instead, as described in [configure it once for the whole application](../tenancy/connection-resolution.md#configure-it-once-for-the-whole-application). What you configure here replaces the application-wide configuration for MongoDB.

## Fetch the identity from your platform

Keep `Server` and `Database` as they are, and resolve only who connects:

```json
{
  "Cratis": {
    "MongoDB": {
      "Server": "mongodb://cluster.example:27017",
      "Database": "app",
      "ConnectionResolution": {
        "Identity": {
          "Resolver": "Http",
          "Http": {
            "Endpoint": "https://platform.example/connections",
            "Headers": { "X-Api-Key": "from-your-secret-store" }
          }
        }
      }
    }
  }
}
```

The same in code:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithMongoDB(
    configureOptions: mongo => mongo.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"))));
```

Each request scope now resolves for its tenant. `IMongoClient`, `IMongoDatabase` and `IMongoCollection<T>` in that scope all come from the same resolution.

## What Arc applies

| Resolved part | Effect |
| --- | --- |
| `ConnectionString` | Replaces `Server`. |
| `Database` | Replaces the database name. Without it, the [tenant database naming](./tenancy.md) applies as before. |
| `Identity.UserName` and `Identity.Password` | Becomes a SCRAM credential. It authenticates against the connection string's `authSource`, else its database, else `admin`. |
| `Identity.AccessToken` | Becomes a `MONGODB-OIDC` credential. When the driver needs a new token, Arc resolves again, so a rotating token never needs a new client. |
| `Schema` | Not used by MongoDB. |

Clients are shared per server and credential, never across credentials. A connection that fails to authenticate drops the cached resolution, and the next scope resolves fresh credentials.

Change-stream [observation](./observing-collections.md) follows the same resolution, partitioned per server and database.

## Place tenants with a template

When placement follows a rule, you do not need an endpoint:

```json
"ConnectionResolution": {
  "Connection": {
    "Resolver": "Template",
    "Template": { "Database": "tenant-{tenant}" }
  }
}
```

:::caution[Resolution replaces the default tenant naming]
A resolved `Database` replaces the `{Database}+{tenant}` naming entirely, including for the default tenant, which resolves as `Default`. Data already stored under the old names stays there. Treat switching as a data migration.
:::
