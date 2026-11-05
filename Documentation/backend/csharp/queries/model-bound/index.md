---
title: Declare model-bound queries in Cratis Arc
description: Declare model-bound queries in Cratis Arc with static read-model methods that expose reads through HTTP endpoints and generated TypeScript proxies.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

A model-bound query in Cratis Arc keeps a read on the model it returns, without a controller that only forwards the call. Mark the model with `[ReadModel]` and add a public static query method. Arc discovers the method for HTTP routing and TypeScript proxy generation; the host and data provider supply the running endpoint.

The read model defines the data a caller receives. Its values can come from MongoDB, EF Core, a service, or in-memory data. Neither `[ReadModel]` nor a database query requires Chronicle or event sourcing. The example below reads accounts directly from MongoDB.

## Model account identities and names

Give account identities and names their own types so the C# compiler can catch accidental swaps. A concept is a domain value wrapped in a type derived from `ConceptAs<T>`: `AccountId` identifies an account, while `AccountName` distinguishes its name from other strings. Define these shared concepts once in the feature and reuse them in commands and read models:

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

These concepts are domain values, not Chronicle event-source identities. Arc and its MongoDB integration use their underlying values for transport and storage. The proxy generator maps them to the corresponding TypeScript value types, so the C# distinctions do not become branded TypeScript types automatically.

Concepts are a modeling convention, not a requirement of `[ReadModel]`. When a rule should follow a value wherever Arc validates it, add a `ConceptValidator<T>`. See [concepts and serialization](../../mongodb/concepts.md) and the [validation tutorial](/arc/tutorial/validation/) for those next steps.

## Start with one read

Configure [the MongoDB provider](../../mongodb/getting-started.md) in an Arc host so dependency injection (DI) can supply `IMongoCollection<DebitAccount>`. Then add this read-model declaration using the concepts above. Later pages replace it with alternative declarations of the same model; do not add them all as duplicate types.

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

Call `GET /api/accounts` to run `AllAccounts`. Check the verdict in the [query result envelope](../query-pipeline.md#query-result-metadata), then read the account list from `data`. A successful empty collection means no accounts matched; a denied or failed query does not establish that the accounts are absent.

The explicit path makes this example independent of namespace-routing configuration. `AllAccounts` becomes the generated query class name; its fully qualified query name is `Banking.Accounts.DebitAccount.AllAccounts`.

This unprotected teaching example is suitable only for public data or a local sandbox. Before exposing account data, add [authorization](authorization.md).

## What Arc discovers

A query must be a non-generic public static method returning the declaring read-model type, a collection of that type, or one of its [supported wrappers](return-types.md). A method accepting that exact read-model type as a parameter is never a query, even if the parameter is nullable. Helpers that take an existing model and return a revised one therefore stay off the endpoint surface without requiring `RequireQueryAttribute`.

For example, a method on `DebitAccount` returning `IEnumerable<DebitAccount>` qualifies. A method there returning `AccountSummary`, `int`, or `PagedResult<DebitAccount>` does not. Put a summary query on an `[ReadModel] AccountSummary` instead, or choose a [controller](../controller-based/index.md). Compiling a method is not proof that Arc discovers an endpoint for it.

`Task<T>` can wrap a supported result, including `ISubject<T>`. Query dependencies are method parameters resolved by type; caller arguments are the other parameters. See [static methods](static-methods.md) and [dependency injection](dependency-injection.md).

## Grow the query in order

Before serving private account data, add [authorization](authorization.md) and constrain the read to the caller's permitted accounts. The policy and host setup on that page applies to model-bound queries as well as their generated endpoints.

Once the read is protected, choose the next step for the screen you are building:

- Accept filters through [query arguments](query-arguments.md) and check them with [validation](../validation.md).
- Return `IQueryable<DebitAccount>` for [paging and sorting](paging.md) before rows are materialized.
- Return a supported stream for [observable updates](observable-queries.md) when the screen must stay current.

Use [dependency injection](dependency-injection.md) to supply the services each read needs, then consume its generated proxy in [React queries](../../../../frontend/react/queries/index.md).
