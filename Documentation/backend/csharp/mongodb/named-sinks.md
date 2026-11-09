---
title: Read MongoDB read models from named sinks
description: Configure Cratis Arc to read read models a Chronicle named sink stores on another MongoDB server, with server or file-based connection settings.
---

A Chronicle Kernel can store some read models on a different MongoDB server than the rest, through a [named sink](/chronicle/sinks/#named-sinks). Cratis Arc reads such a read model from that sink's server once you name the sink in Arc's MongoDB configuration. Queries that inject `IMongoCollection<T>` then reach the right database, and observable queries watch it, without changing their code.

Without this configuration, Arc reads every read model through the default `Cratis:MongoDB` connection. A read model the Kernel stores in a named sink would then come back empty, because the default server holds no such collection.

## Name the sink on the read model

Chronicle applications already name the sink on the read model with Chronicle's `[Sink]` attribute. Arc reads the same declaration:

```csharp
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Sinks;
using MongoDB.Driver;

[ReadModel]
[Sink("reporting")]
public record MonthlyRevenue(Guid Id, decimal Total)
{
    public static IEnumerable<MonthlyRevenue> AllMonths(IMongoCollection<MonthlyRevenue> collection) =>
        collection.Find(FilterDefinition<MonthlyRevenue>.Empty).ToList();
}
```

When the Chronicle client sets `DefaultSinkName`, Arc applies it to the read models the client registers and that name no sink themselves. Passive read models are never read from a sink, since the Kernel does not store them.

Without Chronicle, use Arc's own `[ReadModelSink("reporting")]` from `Cratis.Arc.Sinks`. When a type carries both attributes, Arc's wins.

## Configure the sink's server

Add the sink under `Cratis:MongoDB:Sinks`, by the same name the Kernel's `sinks.configurations` uses. Point `Server` at the same server as the Kernel's `connectionDetails` for that sink:

```json
{
  "Cratis": {
    "MongoDB": {
      "Server": "mongodb://mongo:27017",
      "Database": "my-event-store",
      "Sinks": {
        "reporting": { "Server": "mongodb://reporting-mongo:27017" }
      }
    }
  }
}
```

In Kubernetes, a connection string is a Secret, and the safe way to hand one to a pod is a mounted file. Name the file with `ServerFile` instead of `Server`:

```json
{
  "Cratis": {
    "MongoDB": {
      "Sinks": {
        "reporting": { "ServerFile": "/var/run/secrets/sinks/reporting" }
      }
    }
  }
}
```

The same through environment variables:

```bash
Cratis__MongoDB__Sinks__reporting__ServerFile=/var/run/secrets/sinks/reporting
```

Arc reads the file when the sink is first used and trims surrounding whitespace. Sink names are matched without regard to case.

You can also add a sink in code:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithMongoDB(
    configureOptions: mongo => mongo.Sinks["reporting"] = new() { Server = "mongodb://reporting-mongo:27017" }));
```

## Which database a sink read model comes from

The Kernel names a named MongoDB sink's databases exactly as it names its default read model databases: the event store name, with `+<namespace>` for a namespace other than the default. Arc therefore reads a sink read model from the database the default connection resolves for the current tenant, on the sink's server. With `Database` set to `my-event-store` and the tenant `north`, `MonthlyRevenue` is read from `my-event-store+north` on `reporting-mongo`.

A [custom database name resolver](./tenancy.md#custom-database-resolvers) or a database returned by [connection resolution](./connection-resolution.md) applies to sink read models too, so keep it matching the Kernel's naming.

The sink connects with its `Server` directly. Like the Kernel's named MongoDB sinks, it does not use connection resolution. It shares the default connection's `DirectConnection`, resilience and metrics, and a sink on the same server as another shares its client.

## When the configuration is wrong

Arc refuses to start, with a message naming the sink, when a sink:

- sets both `Server` and `ServerFile`, or neither;
- names a `ServerFile` that does not exist, cannot be read, or is empty (the message names the path, never what the file holds);
- has a `Server` that is not a MongoDB connection string.

A read model naming a sink that has no MongoDB configuration fails when its collection is first resolved, with `UnknownMongoDBSink` naming the sink and the configured sinks. A read model in a SQL or Neo4j sink is not read through MongoDB; read it with the matching integration instead.

## What the sink does not change

- Read models injected into a command's validator, `Provide()` or `Handle()` are loaded through the Chronicle client, which already knows the sink. See [read models in commands](../chronicle/read-models/injecting-into-commands.md).
- An `IMongoDatabase` you inject directly is always the default database.
- A join in an [observed query](./observing-collections.md) reads the joined type from the database of the collection it starts from.
- The generated TypeScript proxies carry no storage information, so the frontend is unaffected.
