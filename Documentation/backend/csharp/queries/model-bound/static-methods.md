---
title: Static query methods
description: Declaration, async execution, and discovery rules for model-bound queries.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Method requirements

Declare queries as non-generic **public or internal static** methods on a `[ReadModel]` type. Give each query a descriptive name and return the declaring model or a [supported wrapper around it](return-types.md). Private static helpers, local functions, compiler-generated methods, and property accessors are not query endpoints. Ordinary helper methods returning another type are not query endpoints. Avoid overloaded query names: the method name is part of the query's identity.

Dependencies are resolved by type, not by parameter position. C# still requires optional parameters to follow required parameters, including required service parameters.

## Not a query

Mark a static helper with `[NotAQuery]` to keep it out of HTTP query discovery, generated metadata and TypeScript proxies, query diagnostics, Screenplay query extraction, and `QueryScenario`:

```csharp
using Cratis.Arc.Queries.ModelBound;

namespace Banking.Accounts;

[ReadModel]
public record AccountSummary(string Name)
{
    [NotAQuery]
    public static AccountSummary Create(string name) => new(name);

    public static AccountSummary ByName(string name) => new(name);
}
```

Only `ByName` is a query. The opt-out works for public and internal methods and does not affect calling a helper directly from C#.

Arc also excludes a recognizable Chronicle fold creator automatically: a static method named `Create`, returning its declaring read model or `Task<T>`/`ValueTask<T>`, with one or two parameters, where either a parameter is `Cratis.Chronicle.Events.EventContext` or the first parameter's type (including its base types) carries `[EventType]`. Nullable parameter types are unwrapped for this check. A method with another name, a collection return type, or more than two parameters is not excluded by this rule. A `Create` query taking a concept remains a query.

An unmarked, explicitly registered event or an interface event parameter cannot be recognized from that signature alone. Add `[NotAQuery]` or an `EventContext` parameter to a creator in that situation. The optional Chronicle integration analyzer [ARCCHR0019](../../chronicle/code-analysis/ARCCHR0019.md) warns about these possible creators and offers the attribute as a quick fix. If the method is intentionally a query taking a concrete service, suppress that heuristic with a justification rather than opting the query out.

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
