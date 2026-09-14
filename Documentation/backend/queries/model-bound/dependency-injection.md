---
title: Query dependency injection
description: Distinguish injected services from caller arguments in static queries.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## How parameters are classified

Model-bound queries inject services as method parameters. A registered **reference type** is classified as a dependency; value types remain caller arguments. The performer resolves dependencies from the service provider supplied to the query pipeline.

Do not register a query-input DTO as a service and expect HTTP to populate it. A registered reference type can become an injected dependency instead. Use [scalar arguments](query-arguments.md) for the built-in HTTP readers and keep service interfaces distinct from input types.

## Inject only what the read needs

This alternative `DebitAccount` declaration uses the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names). Register its collection through the Arc MongoDB provider. `ILogger<T>` and `IOptions<T>` use ordinary .NET DI. This database read is standalone Arc, not a Chronicle projection.

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Banking.Accounts;

public class AccountQueryOptions
{
    public int MaxResults { get; set; } = 100;
}

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Path("/api/accounts/positive")]
    public static async Task<IEnumerable<DebitAccount>> PositiveAccounts(
        IMongoCollection<DebitAccount> collection,
        IOptions<AccountQueryOptions> options)
    {
        return await collection.Find(account => account.Balance > 0)
            .SortBy(account => account.Name)
            .Limit(System.Math.Clamp(options.Value.MaxResults, 1, 1000))
            .ToListAsync();
    }
}
```

The limit is applied before database execution. It is a fixed result cap, not automatic paging. Use [an `IQueryable` result](paging.md) when the client needs pages and total counts.

The same injection pattern works with an application's registered EF Core `DbContext` or service interface. A summary computed by such a service still needs its query method on the returned summary read model; DI does not relax [discovery rules](return-types.md).

## Lifetimes and failures

Use the lifetime appropriate to the service. Do not capture scoped services in singleton caches or cache user-specific results under a process-wide key. If you cache results, include every relevant tenant, caller/permission boundary, and argument in the key and define invalidation explicitly.

For long-lived streams, the disposal of the subscription—not merely the return of the method—must release upstream resources. See [observable query lifetime](observable-queries.md#subscription-lifetime).

A missing dependency or database failure is a failure, not an empty successful result. Test dependency resolution through the real host as well as calling the static method directly; a direct method call alone does not verify discovery or DI classification.
