---
title: Model-bound query paging
description: Use IQueryable for one-shot paging and provider-aware observation for live pages.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Return the query, not all its rows

To let Arc request one page from your database, return `IQueryable<T>` without materializing it first. This alternative declaration uses the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names) and the configured Arc MongoDB provider:

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
GET /api/accounts?page=1&pageSize=10&sortby=name&sortDirection=asc
```

The first request uses the method's stable ID ordering. A client-requested sort replaces that primary ordering; design a stable ordering for production paging, especially when a sort field has duplicates.

## How it works

The built-in `QueryableQueryRenderer` counts the filtered query, applies requested sorting, and then appends `Skip(page * pageSize)` and `Take(pageSize)`. The provider determines whether these operations execute in the database. An in-memory `AsQueryable()` cannot undo earlier database materialization.

| Request key     | Meaning                                                        |
| --------------- | -------------------------------------------------------------- |
| `page`          | Zero-based index; defaults to zero when `pageSize` is supplied |
| `pageSize`      | Enables GET paging when parsed as an integer                   |
| `sortby`        | Read-model field to sort by                                    |
| `sortDirection` | `asc` or `desc`; provide it with `sortby`                      |

Use valid nonnegative page indices and positive, bounded sizes. With no paging request, Arc returns the full matching result. Paging alone is not a server-enforced result cap.

These are context keys, **not method parameters** in model-bound GET. Do not declare `int page` or `int pageSize` and expect normal binding. See [reserved keys](query-arguments.md#reserved-keys).

## Result metadata

The response `paging` object contains `page`, `size`, `totalItems`, and computed `totalPages`. It does not contain `pageSize`, `hasNext`, or `hasPrevious`. See the complete [query result contract](../query-pipeline.md#query-result-metadata).

Materialized lists, arrays, and plain enumerable results do not receive built-in slicing/sorting. Returning a manually constructed `QueryResult` from the read model is not a metadata-control workaround and fails the ordinary discovery contract.

## Observable queries with paging

`ISubject<IEnumerable<T>>` describes a stream, not a paging implementation. The streaming transport attaches paging metadata but does **not** slice arbitrary emissions.

Arc's MongoDB `collection.Observe()` is provider-aware: it captures `IQueryContextManager.Current`, applies sorting/paging to the database query, and updates the total count as it watches changes. The [observable example](observable-queries.md) therefore supports live pages when called through an Arc query context. A plain `Subject<IEnumerable<T>>` emitting 100 items still emits 100 items when the request asks for 10, unless its producer implements paging.

Keep database observation separate from optional Chronicle projection/subscription behavior; neither MongoDB queries nor `Observe()` requires Chronicle.

## Frontend integration

Use the generated proxy's paging API rather than manually changing its data array. See [React paging](../../../frontend/react/queries/paging.md) and [observable queries](../../../frontend/react/queries/observable-queries.md) for the appropriate one-shot and streaming hooks.
