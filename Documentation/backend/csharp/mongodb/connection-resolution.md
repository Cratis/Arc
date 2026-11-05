---
title: Resolve MongoDB connections per tenant in Cratis Arc
description: Configure tenant-specific MongoDB servers, databases, and credentials in Cratis Arc using a template, an HTTP endpoint, or a custom resolver.
---

Connection resolution lets Cratis Arc choose a MongoDB server, database, or credentials for the current tenant. Use it when tenant placement or authentication comes from your platform rather than one fixed connection. A resolver supplies those choices from configuration, a template, an HTTP endpoint, or your own implementation.

The settings below apply to MongoDB only and replace any application-wide connection-resolution configuration for this storage. If several storage integrations should share the same settings, [configure resolution once on Arc](../tenancy/connection-resolution.md#configure-it-once-for-the-whole-application) instead. The [tenancy guide](../tenancy/connection-resolution.md) explains how to choose a resolver; the [HTTP contract](../../../connection-resolution-contract.md) describes the endpoint an `Http` resolver calls.

## Fetch the identity from your platform

If tenants share a server and database naming scheme but connect with different credentials, resolve only the identity. Keep `Server` and `Database` configured:

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

Alternatively, select the HTTP identity resolver in code, keeping the normal MongoDB connection settings in configuration:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithMongoDB(
    configureOptions: mongo => mongo.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"))));
```

Arc resolves the connection once per service scope using the current tenant. The injected `IMongoClient`, `IMongoDatabase`, and `IMongoCollection<T>` in that scope use the same resolution.

## What Arc applies

| Resolved part | Effect |
| --- | --- |
| `ConnectionString` | Replaces `Server`. |
| `Database` | Replaces the database name. Without it, the [tenant database naming](./tenancy.md) applies as before. |
| `Identity.UserName` and `Identity.Password` | Becomes a SCRAM credential. It authenticates against the connection string's `authSource`, else its database, else `admin`. |
| `Identity.AccessToken` | Becomes a `MONGODB-OIDC` credential. When the driver requests a new token, Arc invalidates the cached resolution and resolves again through the existing client's callback. |
| `Schema` | Not used by MongoDB. |

Clients are shared per server and credential, never across credentials. A connection that fails to authenticate drops the cached resolution, and the next scope resolves fresh credentials.

Change-stream [observation](./observing-collections.md) follows the same resolution. Shared watches are keyed by the client's servers and credential together with the database name, keeping observations for different connections separate.

## Place tenants with a template

When tenant placement follows a naming rule, use a template instead of calling an endpoint. For example, this `ConnectionResolution` fragment names databases `tenant-{tenant}`:

```json
"ConnectionResolution": {
  "Connection": {
    "Resolver": "Template",
    "Template": { "Database": "tenant-{tenant}" }
  }
}
```

:::caution[Resolution replaces the default tenant naming]
A resolved `Database` bypasses default tenant naming. Without it, the default tenant uses the configured database name and other tenants use `{Database}+{tenant}`. In the template above, the default tenant resolves to `tenant-Default`. Data already stored under the old names stays there, so plan a data migration before switching.
:::
