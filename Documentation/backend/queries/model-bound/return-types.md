---
title: Model-bound query return types
description: Which return types qualify for discovery and how Arc wraps their results.
---
<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Discovery contract

For an `[ReadModel]` type `T`, a query method must return one of these shapes. One outer `Task<TResult>` is also supported, including around a subject.

| Declared return type | Behavior |
| --- | --- |
| `T` or nullable `T?` for a reference model | Single model |
| `IEnumerable<T>`, `List<T>`, `T[]`, or another collection assignable to `IEnumerable<T>` | Collection of the declaring model |
| `IQueryable<T>` | Collection with renderer-based paging/sorting |
| `ISubject<T>` | Observable single model |
| `ISubject<IEnumerable<T>>` or subject of a collection of `T` | Observable collection |
| `IAsyncEnumerable<T>` | Asynchronous stream of the declaring model |

`Task` without a value, `ValueTask<T>`, arbitrary `IObservable<T>`, unrelated scalar values, unrelated models, and custom result envelopes are not in this discovery contract. A helper returning `int` on `DebitAccount` may compile, but it does not become a count endpoint. Runtime streaming detection and generated-proxy support are separate from C# compilation; test the actual endpoint for any less common shape.

## Collections and absence

With MongoDB, materialize a fluent query using `ToList()` or `ToListAsync()` before returning a collection. `collection.Find(...)` itself is an `IFindFluent`, not `IEnumerable<T>`. For [automatic paging](paging.md), return `collection.AsQueryable()` instead of materializing early.

A null model-bound result is a successful query with null data, not an automatic 404. An empty list is a successful empty collection. An exception is a failed result; do not convert storage failures into either successful absence shape. [Controller null handling](../controller-based/return-types.md#nullable-return-types) differs.

## Custom return types

Put the query on the type it returns. This example reuses the [shared `AccountId` concept](index.md#model-account-identities-and-names) and computes a summary from ordinary MongoDB documents; it is not a Chronicle projection.

```csharp
using System.Linq;
using System.Threading.Tasks;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

namespace Banking.Accounts;

public record AccountDocument(AccountId Id, decimal Balance);

[ReadModel]
public record AccountSummary(int Count, decimal TotalBalance)
{
    [Path("/api/accounts/summary")]
    public static async Task<AccountSummary> Summary(IMongoCollection<AccountDocument> collection)
    {
        var accounts = await collection.Find(_ => true).ToListAsync();
        return new AccountSummary(accounts.Count, accounts.Sum(account => account.Balance));
    }
}
```

This deliberately small example loads all matching documents. Use a database aggregation for large collections, still returning `AccountSummary` from `AccountSummary`. MongoDB `.Project(...)` or LINQ `.Select(...)` are valid ways to shape database results; the result method must still satisfy same-read-model discovery.

## Paged results

Return `IQueryable<T>` and let Arc produce the metadata. Do not put a method returning `PagedResult<T>` or `QueryResult` on `T` and expect it to be discovered. If your API genuinely needs a custom envelope, model that envelope as its own read model with its own method or use a controller.

Arc's backend [QueryResult](../query-pipeline.md#query-result-metadata) is nongeneric. It is an infrastructure envelope produced around the method's data, not the model-bound return type you normally declare.

## Observable return types

Returning a subject makes streaming possible; it does not choose a transport. Clients request a snapshot, SSE, or WebSocket. A task around a subject is awaited before streaming begins. Plain subjects do not automatically gain database watching, paging, or teardown of unrelated subscriptions.

Continue with [observable queries](observable-queries.md) for complete producer examples and lifetime rules.
