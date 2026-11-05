---
title: Inject dependencies into Cratis Arc query controllers
description: Use ASP.NET Core constructor injection for storage and options in Cratis Arc MVC query controllers, while keeping caller input on the action.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

MVC query controllers use ASP.NET Core dependency injection to receive storage and application services. Put those dependencies in the constructor and keep caller-supplied arguments on the action so the request contract stays separate from the services that perform the read.

## Constructor injection

The controller below receives a MongoDB collection and an options object that caps the number of results. It uses the [shared domain concepts](../model-bound/index.md#model-account-identities-and-names) and an ASP.NET Core Arc host with the MongoDB provider configured. Use this `DebitAccount` declaration instead of the model-bound alternative:

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

`PositiveAccounts()` reads the configured cap, clamps it to the allowed range, and applies `Limit()` before `ToListAsync()` executes the query. Keep fluent operations such as `Limit()` and `Skip()` before execution: `FindAsync()` returns a cursor that no longer exposes them. This fixed cap does not provide pages or total-count metadata. Return an `IQueryable<T>` for [renderer-based paging](paging.md) when clients need to navigate the full result set.

## Other providers and lifetimes

You can inject a registered EF Core `DbContext` or application service in the same way. Database reads do not require Chronicle. Resolve scoped services from the request scope instead of capturing them in a singleton. If you cache caller- or tenant-specific results, the cache key and invalidation must preserve those boundaries.

Let storage failures surface as failures so clients can distinguish an unavailable database from a successful query with no matches. A streaming action also needs [subscription-owned teardown](../model-bound/observable-queries.md#subscription-lifetime): injecting the producer's dependencies does not dispose its upstream reactive subscriptions.

Once the controller has its dependencies, [bind caller arguments](query-arguments.md) on the action. Then choose a [return type](return-types.md) that gives the client the response contract it needs.
