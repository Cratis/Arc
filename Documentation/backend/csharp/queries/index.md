---
title: Queries
description: Read application state once or observe updates with Cratis Arc queries, without requiring event sourcing.
---
<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

To show accounts on a screen, declare a query that returns the account data the caller may read. Arc exposes the read through HTTP and generates a typed TypeScript proxy, so the frontend can consume the result without maintaining a separate request contract.

A read model is the shape a query serves. Its data can come from MongoDB, EF Core, a service, or an in-memory model. Arc queries do not require Chronicle or event sourcing. A database query or MongoDB `Observe()` call is not a Chronicle projection. If your application uses Chronicle, queries can also serve its projected read models; see [Chronicle integration](../chronicle/index.md).

## Choose a declaration style

| Style | What you write | Choose it when |
| --- | --- | --- |
| [Model-bound](model-bound/index.md) | Static query method on the `[ReadModel]` type it returns | You want the query next to its data shape with minimal endpoint boilerplate |
| [Controller-based](controller-based/index.md) | MVC GET action | You need MVC route binding, data transfer objects, filters, or HTTP response control |

The two paths share result rendering but not all binding, validation, or authorization behavior. Start with the model-bound example, then use the controller path where you need its HTTP controls.

For example, this method on `[ReadModel] DebitAccount` reads the collection once:

```csharp
[Path("/api/accounts")]
public static IEnumerable<DebitAccount> AllAccounts(IMongoCollection<DebitAccount> collection) =>
    collection.Find(_ => true).ToList();
```

This is a method excerpt from the [model-bound example](model-bound/index.md#start-with-one-read), which supplies the model, imports, and MongoDB setup. `GET /api/accounts` returns the accounts in the query result's `data`. An empty list is different from an unauthorized or failed read, so check the result before rendering an empty state. Add [authorization](model-bound/authorization.md) before exposing private account data.

## Request once or observe updates

```mermaid
flowchart LR
    UI[Client] -->|GET / QUERY / subscribe| Arc[Arc endpoint]
    Arc --> Query[Query method]
    Query --> State[(Application state)]
    State --> Producer[Optional observable producer]
    Query -->|one result| UI
    Producer -->|streamed results| UI
```

| Mode | Behavior | Use it for |
| --- | --- | --- |
| Request/response | Read once through GET or [generated HTTP QUERY](using-the-http-query-method.md) | One-off reads and reports |
| [Observable](model-bound/observable-queries.md) | Subscribe to a producer over Server-Sent Events (SSE) or WebSocket | Lists or dashboards that should stay current |

Choose a one-off read when the caller needs a result at that moment. Choose an observable query when a list or dashboard should update as its source changes. The subscription carries results over SSE or WebSocket, and its resources need to be released when the caller disconnects.

An observable query needs a producer that detects changes; a return type alone does not watch your database. Arc's MongoDB integration supplies `Observe()`. Other providers need their own observation mechanism. An update may follow a [command](../commands/index.md) changing database state directly, or an optional Chronicle projection updating it.

## Learn the contract in order

1. [Declare a model-bound read](model-bound/index.md), then add [arguments](model-bound/query-arguments.md).
2. Apply [validation](validation.md) and [authorization](model-bound/authorization.md), including their current limitations.
3. Read the [QueryResult contract](query-pipeline.md#query-result-metadata) and add [paging](model-bound/paging.md).
4. Expose [observable updates](model-bound/observable-queries.md) and understand subscription disposal.
5. For advanced delivery, use the [hub protocol](observable-query-demultiplexer.md), [change streams](change-stream.md), and [emission guards](observable-query-emission-guards.md).

[Read-model interception](read-model-interception.md) transforms supported results, including observable HTTP snapshots, but does not replace authorization or caller-scoped data selection. [Query health](query-health.md) helps diagnose hub subscriptions but exposes sensitive telemetry unless you restrict it. Use [cURL workflows](using-observable-queries-with-curl.md) to distinguish a snapshot from a live stream.

## Consume the read in React

A successful backend proxy-generation build supplies typed client declarations. Use the generated query's hook and result state instead of maintaining a separate transport model. Continue with [queries in React](../../../frontend/react/queries/index.md) and [proxy generation](../proxy-generation/index.md).
