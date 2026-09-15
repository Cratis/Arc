---
title: Model-bound queries
description: Keep a query with the read model it returns, without writing a controller.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

When a screen needs a list of accounts, you should not need a controller just to forward a database call. Put a static method on the returned model and mark the model with `[ReadModel]`. Arc discovers the query and generates its HTTP endpoint and TypeScript proxy.

A read model is the shape you serve. It can come from MongoDB, EF Core, a service, or in-memory data. **Neither `[ReadModel]` nor a database query requires Chronicle or event sourcing.**

## Model account identities and names

Cratis applications conventionally give domain values their own types. `AccountId` says which kind of identity a method accepts; `AccountName` is not interchangeable with an address or another string. The C# compiler can catch accidental swaps before the application runs. Define shared concepts once in the feature and reuse them in its commands and read models:

```csharp
using System;
using Cratis.Concepts;

namespace Banking.Accounts;

public record AccountId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly AccountId NotSet = new(Guid.Empty);
    public static AccountId New() => new(Guid.NewGuid());
    public static implicit operator AccountId(Guid value) => new(value);
}

public record AccountName(string Value) : ConceptAs<string>(Value)
{
    public static readonly AccountName NotSet = new(string.Empty);
    public static implicit operator AccountName(string value) => new(value);
}
```

These are standalone domain concepts, not Chronicle event-source identities. Arc and its MongoDB integration handle their underlying wire/storage values, and the proxy generator maps them to the corresponding TypeScript value types. The C# domain distinctions are not automatically branded TypeScript types.

This is a modeling convention, not a requirement imposed by `[ReadModel]`. Add a `ConceptValidator<T>` when an invariant should follow a value wherever Arc validates it. See [concepts and serialization](../../mongodb/concepts.md) and the [validation tutorial](/arc/tutorial/validation/).

## Start with one read

Use the concepts above in an Arc host with [the MongoDB provider configured](../../mongodb/getting-started.md) and an `IMongoCollection<DebitAccount>` available from DI. The following is the complete read-model declaration, not a complete host. Later pages show alternative declarations of this same model; do not add all of them as duplicate types.

```csharp
using System.Collections.Generic;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

namespace Banking.Accounts;

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Path("/api/accounts")]
    public static IEnumerable<DebitAccount> AllAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();
}
```

`GET /api/accounts` now returns a [query result envelope](../query-pipeline.md#query-result-metadata) whose `data` is the account list. The explicit path makes this example independent of namespace-routing configuration. `AllAccounts` becomes the generated query class name; its fully qualified query name is `Banking.Accounts.DebitAccount.AllAccounts`.

This unprotected teaching example is suitable only for public data or a local sandbox. Before exposing account data, add [authorization](authorization.md).

## What Arc discovers

Use a non-generic static method returning **the declaring read-model type**, a collection of that type, or one of its [supported wrappers](return-types.md). Public methods are the recommended declaration style.

For example, a method on `DebitAccount` returning `IEnumerable<DebitAccount>` qualifies. A method there returning `AccountSummary`, `int`, or `PagedResult<DebitAccount>` does not. Put a summary query on an `[ReadModel] AccountSummary` instead, or choose a [controller](../controller-based/index.md). Compiling a method is not proof that Arc discovers an endpoint for it.

`Task<T>` can wrap a supported result, including `ISubject<T>`. Query dependencies are method parameters resolved by type; caller arguments are the other parameters. See [static methods](static-methods.md) and [dependency injection](dependency-injection.md).

## Grow the query in order

1. Add [scalar arguments](query-arguments.md) and [validation](../validation.md).
2. Protect the query with [Arc authentication and role checks](authorization.md); do not assume ASP.NET policies run in the model-bound evaluator.
3. Return `IQueryable<DebitAccount>` for [paging and sorting](paging.md).
4. Return a supported stream for [observable updates](observable-queries.md).
5. Consume the generated proxy in [React queries](../../../frontend/react/queries/index.md).
