---
title: Static query methods
description: Declaration, async execution, and discovery rules for model-bound queries.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Method requirements

Declare queries as non-generic **public or internal static** methods on a `[ReadModel]` type. Give each query a descriptive name and return the declaring model or a [supported wrapper around it](return-types.md). Private static helpers, local functions, compiler-generated methods, and property accessors are not query endpoints. Ordinary helper methods returning another type are not query endpoints. Avoid overloaded query names: the method name is part of the query's identity.

A method with a parameter of its own read-model type is **never a query**. The parameter can be nullable or non-nullable, have any name, and appear in any position. This is an exact type check: a collection of the model, a different read-model type, or a service parameter does not trigger the exclusion. Adding `[Query]` to a self-parameter method is an [ARC0026](../../code-analysis/ARC0026.md) error; remove the marker, not the current-state parameter.

Dependencies are resolved by type, not by parameter position. C# still requires optional parameters to follow required parameters, including required service parameters.

## Choose endpoints explicitly when a model has helpers

`[ReadModel]` uses conventional discovery by default. Adding `[Query]` to one method does **not** hide its siblings or change that mode. Keep convention for a model whose eligible static methods are all queries.

Self-parameter folds are excluded automatically. Set `RequireQueryAttribute = true` when the same type also has factories or other query-shaped helpers **without** a self parameter:

```csharp
using System.Threading.Tasks;
using Cratis.Arc.Queries.ModelBound;

namespace Banking.Accounts;

[ReadModel(RequireQueryAttribute = true)]
public record AccountSummary(string Name)
{
    public static AccountSummary Create(string name) => new(name);
    public static Task Prepare(object service) => Task.CompletedTask;

    [Query]
    public static AccountSummary? ByName(string name) => new(name);
}
```

Only `ByName` is an endpoint. Unmarked helpers can be public, internal, private, static, or instance methods, with any return type or service parameters. They receive no query-signature, generic-query, or concept-query-parameter diagnostics. Direct C# calls are unchanged. A type with no marked methods has no queries.

A marked query must still be an ordinary public/internal static nongeneric method on a `[ReadModel]`, with value parameters and a supported return shape. Nullable model results, collections, `Task<T>` wrapping those shapes, `IAsyncEnumerable<T>`, and `ISubject<T>` of the model or its collection are supported in explicit mode. Plain `Task`, `ValueTask<T>`, private/instance methods, and by-reference parameters or results are not valid marked queries; [ARC0026](../../code-analysis/ARC0026.md) reports them. The Arc marker is `Cratis.Arc.Queries.ModelBound.QueryAttribute`, not Chronicle's gRPC marker.

The nearest `[ReadModel]` declaration supplies the setting. A direct `[ReadModel]` on a derived type resets it to false. All partial declarations share the setting; a nested type does not inherit its enclosing type's setting. Arc does not start enumerating inherited static methods.

In explicit mode a marked `ById(string)` can coexist with an unmarked `ById(int)` helper. Two selected methods with the same name are an error, because query identity contains the name rather than an overload signature. Conventional overload selection is unchanged.

## Preview a migration

1. List the currently exposed query names and routes, including every partial declaration.
2. Set `RequireQueryAttribute = true` and mark every endpoint you intend to retain with `[Query]`.
3. Compare runtime discovery, generated query metadata, TypeScript proxies, and Screenplay extraction with the intended endpoint list. Preserve each endpoint's `[Path]`, HTTP method, argument defaults, and authorization attributes.
4. Exercise retained queries through the pipeline or `QueryScenario`, including observable queries, and confirm helpers are absent. `[Path]`, `[QueryHttpMethod]`, and authorization on an unmarked helper do not make it an endpoint.

No method name is globally reserved. A `Reduce` or `Create` method **with a self parameter** is excluded by its signature, not its name. Helpers without a self parameter remain subject to conventional discovery and diagnostics. The optional Chronicle integration warning [ARCCHR0019](../../chronicle/code-analysis/ARCCHR0019.md) flags some creator shapes without changing discovery. Its whole-type quick fix requires an endpoint preview; it cannot prove which helpers you intended as queries. For a record that combines static reductions with ordinary queries, see [self-reducing read models](../../chronicle/read-models/index.md#self-reducing-read-models).

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
