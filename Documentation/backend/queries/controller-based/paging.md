---
title: Controller query paging
description: Let the MVC query renderer apply paging and sorting to IQueryable results.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Return an IQueryable

For a default Arc-wrapped MVC GET, return an unmaterialized database query. This alternative banking declaration uses the [shared domain concepts](../model-bound/index.md#model-account-identities-and-names), an ASP.NET Core Arc host, configured authorization, and the MongoDB provider:

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
GET /api/accounts/positive?page=1&pageSize=10&sortby=name&sortDirection=asc
```

The action filter establishes the paging/sorting context. `QueryableQueryRenderer` counts the filtered query, applies requested sorting, then applies `Skip`/`Take`. A database-backed LINQ provider can perform this work server-side; an in-memory queryable cannot undo earlier materialization.

## Paging contract

`page` is zero-based, `pageSize` is the requested size, and `sortby`/`sortDirection` select the sort. Use positive, bounded sizes and a stable ordering. Client-requested ordering replaces the method's primary ordering; account for duplicate sort values in production.

The response `paging` object has **`page`, `size`, `totalItems`, `totalPages`**. See [query result metadata](../query-pipeline.md#query-result-metadata). Without a paging request, the query is not automatically capped.

Lists, arrays, and plain enumerable results are not automatically sliced. A manually paged list does not automatically carry the database's total count. Do not return a `QueryResult` from a wrapped action to fix that: it becomes nested data, not outer metadata.

## Manual control

`IQueryContextManager.Current` exposes `Paging` and `Sorting` if an integration needs them. When using MongoDB's fluent API, compose `Skip`, `Limit`, and `Sort` **before** execution; use expression sorting or a `SortDefinition`, not `SortBy(string)`.

For a completely custom response/metadata contract, use `[AspNetResult]` and supply the full MVC response yourself. That is an opt-out from Arc wrapping and needs a matching client. Prefer the queryable path unless you need that control.

## Observable paging

A stream does not become paged merely because it emits a collection. Arc's MongoDB `Observe()` supplies [provider-aware paging](../model-bound/paging.md#observable-queries-with-paging); arbitrary subjects must implement it themselves. See [controller observable queries](observable-queries.md).
