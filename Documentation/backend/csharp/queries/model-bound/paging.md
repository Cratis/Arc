---
title: Model-bound query paging
description: Page model-bound queries in Cratis Arc with IQueryable, read totals from result metadata, and use provider-aware observation for live pages.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Model-bound query paging in Cratis Arc lets a client request a portion of a result and read the total matching count. For a one-shot read, return `IQueryable<T>` so Arc can count, sort, and select rows before the provider materializes them. Live pages need a producer that applies paging as the data changes.

## Return the query, not all its rows

Replace the earlier `AllAccounts` declaration with this `IQueryable<DebitAccount>` version. It uses the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names) and the configured Arc MongoDB provider. Calling `ToList()` inside the method would execute the database read too early, leaving Arc a collection it cannot page through the queryable renderer:

```csharp
using System.Linq;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Banking.Accounts;

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Path("/api/accounts")]
    public static IQueryable<DebitAccount> AllAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.AsQueryable().OrderBy(account => account.Id);
}
```

```http
GET /api/accounts?page=0&pageSize=25
GET /api/accounts?page=1&pageSize=10&sortBy=name&sortDirection=asc
```

The first request asks for the first 25 accounts in the method's stable ID order. The second asks for the second page, with ten accounts per page, ordered by name. Page indices start at zero.

A client-requested sort replaces the method's primary ordering. Account names can repeat, so name order alone does not guarantee stable page boundaries. Decide how the application will keep ordering stable before exposing arbitrary sort fields for production paging.

## How it works

The built-in `QueryableQueryRenderer` counts all rows matching the query, applies requested sorting, and then appends `Skip(page * pageSize)` and `Take(pageSize)`. Returning the MongoDB query preserves the provider's opportunity to execute those operations in the database. Converting an already loaded list with `AsQueryable()` only queries that in-memory list; it cannot reduce the earlier database read.

| Request key     | Meaning                                                        |
| --------------- | -------------------------------------------------------------- |
| `page`          | Zero-based index; defaults to zero when `pageSize` is supplied   |
| `pageSize`      | Enables GET paging when parsed as an integer                    |
| `sortBy`        | Read-model field to sort by                                     |
| `sortDirection` | `asc` or `desc`; provide it with `sortBy`                         |

GET matches these four reserved keys case-insensitively, including `sortby` and `SORTBY`. They belong to the query context, not to the static method's argument list. Declaring `int page` or `int pageSize` will not bind them as ordinary caller arguments; see [reserved keys](query-arguments.md#reserved-keys).

Use nonnegative page indices and positive, bounded sizes. A client can omit paging and receive the full matching result, so paging is not a server-enforced result cap. If the read must always have a maximum size, enforce that separately.

## Ahead-of-time compilation

For eligible read models with collection-returning queries, the Arc source generator registers the model and its supported public-property types for statically typed counting, paging, and sorting. Under the JIT, unregistered element and sort-key types use reflection. When dynamic code is unavailable, those types fail with `MissingQueryableElementType` or `MissingQueryableSortKey`, naming the missing type. These registrations alone do not make the whole application NativeAOT-compatible.

## Result metadata

Check the [query result verdict](../query-pipeline.md#query-result-metadata) first, then read the selected accounts from `data`. The requested page is in `paging.page` and `paging.size`; `paging.totalItems` counts matching accounts before slicing, and `paging.totalPages` is computed from that count and size. The response has no `pageSize`, `hasNext`, or `hasPrevious` fields. An empty successful page is different from a denied or failed read.

Materialized lists, arrays, and plain enumerable results do not receive built-in slicing or sorting. Keep the read-model method's supported return shape: returning a manually constructed `QueryResult` to control metadata fails ordinary model-bound discovery.

## Observable queries with paging

A stream return type does not implement paging. The streaming transport attaches paging metadata but does not slice arbitrary `ISubject<IEnumerable<T>>` emissions. A plain subject that produces 100 items still produces 100 when the request asks for ten, unless its producer implements paging.

Arc's MongoDB `collection.Observe()` handles that work in the provider. It captures `IQueryContextManager.Current`, applies sorting and paging to the database query, and updates the total count as changes arrive. Use the [observable example](observable-queries.md) through an Arc query context for live pages, with MongoDB change-stream support and a subscription transport configured. Single-document `ObserveSingle()` and `ObserveById()` do not apply query-context paging, sorting, or total-count updates.

Neither these MongoDB reads nor `Observe()` requires Chronicle; a Chronicle projection is a separate choice about how data reaches the collection.

## Frontend integration

Use the generated proxy's paging API to request another page rather than manually slicing its data array. Continue with [React paging](../../../../frontend/react/queries/paging.md) for one-shot reads, or [observable queries](../../../../frontend/react/queries/observable-queries.md) for hooks that keep a live page subscribed.
