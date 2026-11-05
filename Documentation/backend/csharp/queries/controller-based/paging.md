---
title: Page Cratis Arc controller query results
description: Return an unmaterialized IQueryable from an MVC GET action so Cratis Arc can apply paging and sorting and include total counts in the response.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Return an `IQueryable<T>` from an Arc-wrapped MVC GET action when clients need pages of results with a total count. Leave the database query unmaterialized: Arc's renderer applies the requested sorting and paging before the result is enumerated.

## Return an IQueryable

These actions return database queries without calling `ToList()`. Both start with a stable ordering by account ID, and `PositiveAccounts()` filters out accounts whose balance is zero or negative. The example uses the [shared domain concepts](../model-bound/index.md#model-account-identities-and-names), an ASP.NET Core Arc host, configured authorization, and the MongoDB provider. Use this `DebitAccount` declaration instead of the model-bound alternative:

```csharp
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Banking.Accounts;

public record DebitAccount(AccountId Id, AccountName Name, decimal Balance);

[Authorize(Roles = "AccountReader")]
[Route("api/accounts")]
public class AccountsController(IMongoCollection<DebitAccount> collection) : ControllerBase
{
    [HttpGet]
    public IQueryable<DebitAccount> AllAccounts() =>
        collection.AsQueryable().OrderBy(account => account.Id);

    [HttpGet("positive")]
    public IQueryable<DebitAccount> PositiveAccounts() =>
        collection.AsQueryable().Where(account => account.Balance > 0)
            .OrderBy(account => account.Id);
}
```

```http
GET /api/accounts?page=0&pageSize=25
GET /api/accounts/positive?page=1&pageSize=10&sortBy=name&sortDirection=asc
```

The first request asks for the first 25 accounts. The second asks for the next page of 10 positive-balance accounts, ordered by name. Arc's action filter reads those reserved query-string values into the query context. `QueryableQueryRenderer` counts the filtered query, applies the requested sorting, then applies `Skip` and `Take`.

A database-backed LINQ provider can perform that work server-side. Calling `ToList()` first loads the data before Arc can page it; turning that list back into a queryable does not move the work back to the database.

## Paging contract

`page` is zero-based and defaults to zero when a valid `pageSize` is supplied without a page number. `pageSize` enables paging and sets the requested size. Use positive, bounded sizes in your application. Without a paging request, Arc does not automatically cap the query.

Supply both `sortBy` and `sortDirection` to request sorting. GET matches these reserved keys case-insensitively, so `sortby` and `SORTBY` also work. Client-requested ordering replaces the method's primary ordering. Keep ordering stable, accounting for duplicate sort values when designing the client's paging behavior.

The response's `paging` object contains `page`, `size`, `totalItems`, and `totalPages`, as described in [query result metadata](../query-pipeline.md#query-result-metadata). `totalItems` counts matching items before paging.

Lists, arrays, and plain enumerable results are not automatically sliced. If the action pages a list itself, Arc does not automatically recover the database's total count. Returning a `QueryResult` from a wrapped action does not set the outer metadata either: Arc wraps that object as nested data.

## Manual control

An integration can read `Paging` and `Sorting` from `IQueryContextManager.Current`. With MongoDB's fluent API, compose `Skip`, `Limit`, and `Sort` before execution. Use an expression or a `SortDefinition` for sorting; there is no `SortBy(string)` overload.

If you need to own the full response and its metadata, use `[AspNetResult]` and supply the MVC response yourself. This opts out of Arc wrapping, so the client must understand your custom contract. Returning an unmaterialized queryable keeps paging and metadata in Arc's renderer.

## Observable paging

A stream needs a producer that applies paging to each emitted collection. Arc's MongoDB `Observe()` supplies [provider-aware paging](../model-bound/paging.md#observable-queries-with-paging), while arbitrary subjects must implement it themselves. Continue with [controller observable queries](observable-queries.md) to return an observed collection through an MVC GET action.
