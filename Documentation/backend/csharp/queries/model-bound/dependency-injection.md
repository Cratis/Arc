---
title: Model-bound query dependency injection
description: Inject services into model-bound queries in Cratis Arc while keeping caller arguments, cancellation, and subscription lifetimes distinct.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Model-bound queries in Cratis Arc receive registered services through their static method parameters. Inject the collection, database context, or service that performs the read alongside any caller arguments; Arc resolves dependencies from the service provider used by the query pipeline.

## How parameters are classified

A registered reference type can become an injected dependency. Value types and concepts (`ConceptAs<T>`) remain caller arguments, even when registered. Collections of query-argument values, such as `IEnumerable<int>`, are also caller inputs rather than services; collections of service interfaces can still be injected.

A `CancellationToken` parameter is neither a dependency nor a caller argument. For HTTP requests, Arc binds it to the request's abort token so the query can stop when the client disconnects. It does not appear in the generated TypeScript proxy.

Keep service interfaces distinct from input types. If you register an input object as a service, Arc can inject it instead of reading caller input. Use the supported [query arguments](query-arguments.md) for values supplied over HTTP.

## Inject only what the read needs

The following read uses a collection to select accounts and `IOptions<AccountQueryOptions>` to cap how many it returns. Register the collection through the [Arc MongoDB provider](../../mongodb/getting-started.md) and configure the options through ordinary .NET dependency injection (DI).

This is an alternative `DebitAccount` declaration using the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names), not an additional type to add beside the earlier example. It reads MongoDB directly and does not require Chronicle.

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

Both parameters come from DI, so the caller supplies neither the collection nor the options in the request. `MaxResults` is clamped between 1 and 1000 and applied before database execution. This caps the result; it does not give the client pages or a total count. Return [an `IQueryable` result](paging.md) when the client needs those.

The same injection pattern works with an application's registered EF Core `DbContext` or service interface. A summary computed by such a service still needs its query method on the returned summary read model; DI does not relax [discovery rules](return-types.md).

## Lifetimes and failures

Keep scoped services within the scope that owns the query; do not capture them in singleton caches. Cached results also need keys that distinguish tenants, callers, permissions, and arguments wherever those affect the read. Define how entries are invalidated so cached data cannot outlive the access decision it depends on.

A streaming query returns before its work is finished. Disposing its subscription must release upstream resources, not just the method's local variables. Follow [observable query lifetime](observable-queries.md#subscription-lifetime) when the read stays open.

A missing dependency or database failure is not a successful empty result. Test resolution through the real host as well as calling the static method directly: a direct call supplies the parameters itself and cannot verify Arc's discovery or DI classification.
