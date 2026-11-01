---
title: Get started with MongoDB
description: Add optional MongoDB persistence to standalone Arc with explicit connection settings.
---

When an Arc command or query needs document storage, add `Cratis.Arc.MongoDB`. The integration supplies collection injection, BSON serializers, naming conventions, and observation. **Chronicle and event sourcing are not prerequisites.**

## Install the optional provider

Start from a working [ASP.NET Core Arc host](../asp-net-core/index.md) or [lightweight Arc host](../core/getting-started.md), then install:

```bash
dotnet add package Cratis.Arc.MongoDB
```

The MongoDB package currently references the ASP.NET shared framework as well as Arc Core, even though its persistence features do not require Chronicle.

## Configure connection settings

In your host's `appsettings.json`, add:

```json
{
  "Cratis": {
    "MongoDB": {
      "Server": "mongodb://localhost:27017",
      "Database": "library"
    }
  }
}
```

`Server` and `Database` are required. Keep production credentials in your host's secret configuration, not source control. `DirectConnection` is optional: when unset, the connection string's setting is preserved. Direct connection does not turn a standalone MongoDB server into a replica set.

## Enable the integration

This is a complete ASP.NET Core `Program.cs` for a project using `Microsoft.NET.Sdk.Web` with `Cratis.Arc` and `Cratis.Arc.MongoDB` installed. Use the settings above and a reachable MongoDB deployment.

```csharp
using Cratis.Arc;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc(configureBuilder: arc => arc.WithMongoDB());

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

`WithMongoDB()` works through `IArcBuilder`, including the lightweight host's `AddCratisArc` configuration callback. Retain that host's normal build, `UseCratisArc()`, and run sequence. For existing ASP.NET/generic-host code, `UseCratisMongoDB()` remains an alternative registration entry point; do not register both paths.

A registration checkpoint is that a request/service scope can resolve `IMongoCollection<T>`. A successful database read additionally requires connectivity, permissions, and any application data you expect; starting the host does not populate collections.

## What gets configured

- Scoped `IMongoClient`, `IMongoDatabase`, and `IMongoCollection<T>` access.
- Default server/database resolvers and unchanged type/member naming.
- Guid serialization using `GuidRepresentation.Standard`.
- Concept, date/time, geometry, and other [BSON serializers](./serializers.md).
- Discovered [class maps](./class-mapping.md), [convention packs](./convention-packs.md), and filters.

`DateTimeOffset` uses BSON UTC milliseconds by default: the original offset and submillisecond precision are not preserved. Naming and serializers are process-wide initialization, not per-request customization.

## Customize defaults

Alternative startup fragment; custom resolver types must implement the corresponding interfaces:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithMongoDB(
    configureMongoDB: mongo => mongo.WithCamelCaseNamingPolicy()));
```

Import `Cratis.Arc.MongoDB` for MongoDB builder customization methods. Use `WithServerResolver<T>()` and `WithDatabaseResolver<T>()` only when the default configured server and [tenant database naming](./tenancy.md) do not meet your requirements.

## Collection access resilience

`EnableResilience` defaults to `true`. Leave it enabled for Cosmos DB and when opening change streams before a collection exists. It handles "Collection not found" errors on async (`Task`-returning) collection calls, returning success, default values or empty cursors as appropriate; change streams return a cursor that waits for the collection. Synchronous collection calls bypass this behavior. Async calls also have a per-method concurrency limit of half the driver's connection pool, with a one-second acquire timeout that raises `TimeoutException`. This is not a retry policy or an AOT switch.

For workloads that need plain driver behavior, add this setting alongside your required `Server` and `Database` values:

```json
{
  "Cratis": {
    "MongoDB": {
      "EnableResilience": false
    }
  }
}
```

Alternatively, configure the option when adding MongoDB to Arc (with the normal connection configuration already available). The callback takes precedence over configuration:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithMongoDB(
    configureOptions: options => options.EnableResilience = false));
```

With resilience disabled, you receive native exceptions and cancellation, the driver owns retries, and Arc adds no concurrency limit. Database and collection objects come directly from the driver. In either mode, disposing the root client does not dispose the process-shared client; disposing a request scope cannot invalidate later scopes. Derived clients returned by `WithReadConcern`, `WithReadPreference` or `WithWriteConcern` retain native driver behavior and ownership.

Next, verify [concept serialization](./concepts.md) and [class mapping](./class-mapping.md). Add [collection observation](./observing-collections.md) only after ordinary reads work; change streams require a replica set or sharded cluster and appropriate permissions.
