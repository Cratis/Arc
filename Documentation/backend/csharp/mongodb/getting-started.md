---
title: Set up MongoDB persistence in Cratis Arc
description: Connect a Cratis Arc host to MongoDB, register injectable collections, and choose BSON mapping defaults before adding live queries.
---

Add `Cratis.Arc.MongoDB` when an Arc command or query needs to read or write MongoDB documents. This setup connects your host to a database and makes typed collections available through dependency injection. It does not require Chronicle or event sourcing.

## Install the optional provider

Start from a working [ASP.NET Core Arc host](../asp-net-core/index.md) or [lightweight Arc host](../core/getting-started.md), then install:

```bash
dotnet add package Cratis.Arc.MongoDB
```

The package references the ASP.NET shared framework as well as Arc Core, so that framework must be available to your host.

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

### Required settings, and the error when they are missing

`Server` and `Database` have no defaults and are both required. They are read from the `Cratis:MongoDB` section. As environment variables they are `Cratis__MongoDB__Server` and `Cratis__MongoDB__Database`. The section name is fixed unless you pass a different `mongoDBConfigSectionPath` to `WithMongoDB()`.

When either is missing or empty, the host fails at startup with an options validation error, for example `The Server field is required.`. The message names the field, not the configuration key, so check that `Cratis:MongoDB:Server` and `Cratis:MongoDB:Database` reach the host, including from environment variables or user secrets.

Keep production credentials in your host's secret configuration, not source control. `DirectConnection` is optional: when unset, the connection string's setting is preserved. Direct connection does not turn a standalone MongoDB server into a replica set.

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

To check registration, resolve `IMongoCollection<T>` from a request or service scope, with `T` replaced by your document type. Then read a collection you can access. Resolving the service checks dependency injection; a read checks connectivity and permissions too. Starting the host does not populate collections.

## What gets configured

Registration supplies these defaults for collection access and BSON, MongoDB's binary document format:

- Scoped `IMongoClient`, `IMongoDatabase`, and `IMongoCollection<T>` access.
- Default server/database resolvers and unchanged type/member naming.
- Guid serialization using `GuidRepresentation.Standard`.
- Concept, date/time, geometry, and other [BSON serializers](./serializers.md).
- Discovered [class maps](./class-mapping.md), [convention packs](./convention-packs.md), and filters.

Choose naming and serialization rules before writing documents. They are initialized once for the process, not customized per request. For example, `DateTimeOffset` uses BSON UTC milliseconds by default, so stored values lose the original offset and submillisecond precision.

## Customize defaults

To store collection and member names in camel case, use this alternative registration:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithMongoDB(
    configureMongoDB: mongo => mongo.WithCamelCaseNamingPolicy()));
```

Import `Cratis.Arc.MongoDB` for MongoDB builder customization methods. Use `WithServerResolver<T>()` and `WithDatabaseResolver<T>()` only when the default configured server and [tenant database naming](./tenancy.md) do not meet your requirements.

## Collection access resilience

`EnableResilience` defaults to `true`. Leave it enabled for Cosmos DB and when opening change streams before a collection exists. On async (`Task`-returning) collection calls, it handles "Collection not found" errors by returning success, default values, or empty cursors as appropriate. A change stream instead receives a cursor that waits for the collection.

Synchronous calls bypass this behavior. Async calls have a per-method concurrency limit of half the driver's connection pool; waiting longer than one second for a slot raises `TimeoutException`. This option controls collection access, not retries or ahead-of-time (AOT) compilation.

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

## Command logging

When Trace logging is enabled for `MongoDBClientFactory` at startup, Arc logs command start and completion metadata: request id, command name, database, collection, duration, and, for failures, the exception type and server error code. It does not log document, update or filter values, nor the driver's failure message, because duplicate-key messages echo the key values.

To see full command bodies and failure messages while diagnosing a problem, opt in:

```json
{
  "Cratis": {
    "MongoDB": {
      "LogCommandBodies": true
    }
  }
}
```

> [!WARNING]
> `LogCommandBodies` writes document and filter values to your logs. They may include personal data. Enable it only temporarily and never in production.

Once ordinary reads work, check your document representations with [concept serialization](./concepts.md) and [class mapping](./class-mapping.md). To keep query results live, continue with [collection observation](./observing-collections.md). Change streams require a replica set or sharded cluster with database-level watch and read permissions; a standalone server is not sufficient.
