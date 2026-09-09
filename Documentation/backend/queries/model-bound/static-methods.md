---
title: Static query methods
description: Declaration, async execution, and discovery rules for model-bound queries.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Method requirements

Declare queries as non-generic static methods on an `[ReadModel]` type. Prefer public methods, give each query a descriptive name, and return the declaring model or a [supported wrapper around it](return-types.md). Ordinary helper methods returning another type are not query endpoints. Avoid overloaded query names: the method name is part of the query's identity.

Dependencies are resolved by type, not by parameter position. C# still requires optional parameters to follow required parameters, including required service parameters.

## Async methods

This alternative declaration uses the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names) and an Arc host with the MongoDB provider configured:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

namespace Banking.Accounts;

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Path("/api/accounts/search")]
    public static async Task<IEnumerable<DebitAccount>> Search(
        IMongoCollection<DebitAccount> collection,
        AccountName name) =>
        await collection.Find(account => account.Name == name)
            .SortBy(account => account.Name)
            .Limit(100)
            .ToListAsync();

    [Path("/api/accounts/by-id")]
    public static async Task<DebitAccount?> ById(
        AccountId id,
        IMongoCollection<DebitAccount> collection) =>
        await collection.Find(account => account.Id == id).FirstOrDefaultAsync();
}
```

`Search` composes the MongoDB fluent query **before** materializing it. `FindAsync()` returns a cursor, not a fluent query; do not call `Limit()` or `Skip()` on that cursor. The fixed 100-row cap here is not Arc paging and supplies no total-count metadata.

Arc awaits the task. `ById` can return null successfully in the model-bound pipeline; that is not an automatic HTTP 404. Do not catch database failures and return null or an empty list: that hides a failure as a successful read.

## Observable methods

`Task<ISubject<DebitAccount>>` and `Task<ISubject<IEnumerable<DebitAccount>>>` are accepted after the `Task` is unwrapped. A task prepares the stream; the subscription then delivers its values. Returning a subject does not itself open a WebSocket: the client/request selects the transport.

Use the [observable-query guide](observable-queries.md) for producer lifetime and transport behavior, then [query arguments](query-arguments.md) for binding rules.
