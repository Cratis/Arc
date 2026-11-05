---
title: Set up Neo4j in Cratis Arc
description: Connect a Cratis Arc host to Neo4j, inject the tenant-aware graph session and run a first Cypher query through a model-bound query.
---

Add `Cratis.Arc.Neo4j` when an Arc query or command needs to read a Neo4j graph. The setup connects your host to a server and makes a session on the current tenant's database available through dependency injection. It does not require Chronicle, but it reads the databases that Chronicle's Neo4j graph sink writes.

## Install the optional provider

Start from a working [ASP.NET Core Arc host](../asp-net-core/index.md) or [lightweight Arc host](../core/getting-started.md), then install:

```bash
dotnet add package Cratis.Arc.Neo4j
```

The package references the ASP.NET shared framework as well as Arc Core, so that framework must be available to your host.

## Configure connection settings

In your host's `appsettings.json`, add:

```json
{
  "Cratis": {
    "Neo4j": {
      "Server": "bolt://localhost:7687",
      "EventStore": "Organization",
      "UserName": "neo4j",
      "PasswordSecret": "NEO4J_PASSWORD"
    }
  }
}
```

| Option | Description |
| --- | --- |
| `Server` | Required. The Bolt URI, such as `bolt://localhost:7687` or `neo4j+s://graph.example`. |
| `EventStore` | Required. The name of the event store whose graph you read. Use the event store Chronicle's Neo4j sink writes; it is the first part of the [database name](./tenancy.md). |
| `UserName` | Optional. The user to authenticate as. |
| `PasswordSecret` | Optional. The name of the configuration key holding the password. The key can come from any configuration provider, such as an environment variable, user secrets or a key vault, so the password never has to be written in options. |
| `Password` | Optional. A literal password for local development. `PasswordSecret` wins when both are set. |
| `DatabasePrefix` | Optional. A prefix on every database name, to share one server between applications. |
| `DatabaseMode` | `PerNamespace` (default) or `Single`. See [tenancy](./tenancy.md). |
| `SingleDatabase` | The database used in `Single` mode. Defaults to `neo4j`. |
| `ConnectionResolution` | Optional. See [connection resolution](./connection-resolution.md). |

Keep production credentials in your host's secret configuration, not in source control.

## Enable the integration

```csharp
using Cratis.Arc;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc(configureBuilder: arc => arc.WithNeo4j());

var app = builder.Build();
app.UseCratisArc();
await app.RunAsync();
```

`WithNeo4j()` works through `IArcBuilder`, including the lightweight host's `AddCratisArc` configuration callback. For existing ASP.NET or generic-host code, `AddCratisNeo4j()` on `IServiceCollection` or `IHostBuilder` is the alternative entry point; do not register both paths. Each takes an optional callback for `Neo4jOptions`, an optional callback for the builder (to replace the database name or namespace resolver) and an optional configuration section path, `Cratis:Neo4j` by default.

## What gets configured

- `INeo4jDriverFactory`: one pooled Neo4j driver per resolved server and identity, shared for the life of the application and disposed with the host. Never dispose a driver yourself.
- `IGraphDatabaseNameResolver`: names the database for the current tenant. See [tenancy](./tenancy.md).
- Scoped `IGraphSession`: a session on that database, for reading.
- Scoped `IGraphSessionFactory`: opens a session in a mode you choose.

## Run a first query

This model-bound query lists the members of a group:

```csharp
using Cratis.Arc.Neo4j;
using Cratis.Arc.Queries.ModelBound;

[ReadModel]
public record GroupMember(PersonId PersonId, PersonName Name)
{
    public static Task<IReadOnlyList<GroupMember>> MembersOf(GroupId groupId, IGraphSession graph) =>
        graph.Query<GroupMember>(
            "MATCH (p:Person)-[:MEMBER_OF]->(g:Group { groupId: $groupId }) RETURN p.personId AS personId, p.name AS name ORDER BY p.name",
            new { groupId });
}
```

The full sample is in `Samples/Neo4j`. Resolving the session checks dependency injection; running a query checks connectivity and permissions too. Starting the host does not connect.
