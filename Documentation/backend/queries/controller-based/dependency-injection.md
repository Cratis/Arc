---
title: Controller query dependencies
description: Inject storage, options, and services into MVC query controllers.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Constructor injection

Controllers use ASP.NET Core dependency injection. Inject services through the constructor and keep caller arguments on the action. This alternative banking declaration uses the [shared domain concepts](../model-bound/index.md#model-account-identities-and-names) and an ASP.NET Core Arc host with the MongoDB provider configured:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Banking.Accounts;

public class AccountQueryOptions
{
    public int MaxResults { get; set; } = 100;
}

public record DebitAccount(AccountId Id, AccountName Name, decimal Balance);

[Authorize(Roles = "AccountReader")]
[Route("api/accounts")]
public class AccountsController(
    IMongoCollection<DebitAccount> collection,
    IOptions<AccountQueryOptions> options) : ControllerBase
{
    [HttpGet("positive")]
    public async Task<IEnumerable<DebitAccount>> PositiveAccounts() =>
        await collection.Find(account => account.Balance > 0)
            .SortBy(account => account.Name)
            .Limit(Math.Clamp(options.Value.MaxResults, 1, 1000))
            .ToListAsync();
}
```

The query is limited before execution. `FindAsync()` returns a cursor; it is too late to call fluent `Limit()` or `Skip()` on that cursor. A fixed cap is not automatic paging and does not provide total-count metadata; use [IQueryable paging](paging.md) when you need it.

## Other providers and lifetimes

A registered EF Core `DbContext` or application service works the same way. Database reads do not require Chronicle. Resolve scoped services from the request scope; do not put them in a singleton. Avoid shared cache keys for caller- or tenant-specific results unless the key and invalidation preserve those boundaries.

Let genuine storage failures surface as failures instead of returning an empty successful result. For streaming actions, constructor injection alone does not manage an upstream Rx subscription: preserve [subscription-owned teardown](../model-bound/observable-queries.md#subscription-lifetime).

Continue with [query arguments](query-arguments.md) for MVC binding and [return types](return-types.md) for the wrapper contract.
