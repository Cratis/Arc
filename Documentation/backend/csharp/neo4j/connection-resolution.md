---
title: Resolve Neo4j connections per tenant in Cratis Arc
description: Configure tenant-specific Neo4j servers, databases and credentials in Cratis Arc using a template, an HTTP endpoint, or a custom resolver.
---

Connection resolution lets Cratis Arc choose a Neo4j server, database or credentials for the current tenant. Use it when tenant placement or authentication comes from your platform rather than one fixed connection. A resolver supplies those choices from configuration, a template, an HTTP endpoint, or your own implementation.

The settings below apply to Neo4j only and replace any application-wide connection-resolution configuration for this storage. If several storage integrations should share the same settings, [configure resolution once on Arc](../tenancy/connection-resolution.md#configure-it-once-for-the-whole-application) instead. The [tenancy guide](../tenancy/connection-resolution.md) explains how to choose a resolver; the [HTTP contract](../../../connection-resolution-contract.md) describes the endpoint an `Http` resolver calls. The request asks for the storage `Neo4j`.

## Fetch the identity from your platform

If tenants share a server and the [database naming](./tenancy.md), but connect with different credentials, resolve only the identity. Keep `Server` and `EventStore` configured:

```json
{
  "Cratis": {
    "Neo4j": {
      "Server": "neo4j+s://graph.example",
      "EventStore": "Organization",
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

Alternatively, select the HTTP identity resolver in code:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithNeo4j(
    configureOptions: neo4j => neo4j.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"))));
```

Arc resolves the connection once per service scope using the current tenant. The injected `IGraphSession` and `IGraphSessionFactory` in that scope use the same resolution.

## What Arc applies

| Resolved part | Effect |
| --- | --- |
| `ConnectionString` | A Bolt URI that replaces `Server`. |
| `Database` | Replaces the database name. Without it, the [tenant database naming](./tenancy.md) applies. |
| `Identity.UserName` and `Identity.Password` | Authenticate with a basic credential. |
| `Identity.AccessToken` | Authenticates with a bearer token. |
| `Schema` | Not used by Neo4j. |

When the resolution gives no identity, the configured `UserName` and password are used. Drivers are shared per server and resolution, never across credentials. The driver asks Arc for the identity whenever it opens a connection, which only resolves again when the cached answer is missing or expired. When the server rejects the credential or the token has expired, Arc drops the cached resolution and the driver tries again with a fresh one, so rotated credentials are picked up without a restart.

## Place tenants with a template

When tenant placement follows a naming rule, use a template instead of calling an endpoint. For example, this `ConnectionResolution` fragment names databases `graph-{tenant}`:

```json
"ConnectionResolution": {
  "Connection": {
    "Resolver": "Template",
    "Template": { "Database": "graph-{tenant}" }
  }
}
```

:::caution[Resolution replaces the default database naming]
A resolved `Database` bypasses the naming algorithm that Chronicle's Neo4j sink uses. In the template above, the default tenant resolves to `graph-Default`. Point the sink at the same destination, or Arc reads a database nothing writes to.
:::
