---
title: Model-bound query authorization
description: Protect reads with Arc authentication and roles, and understand the current policy limitation.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Choose the right authorization surface

For model-bound queries, use attributes from **`Cratis.Arc.Authorization`**. The default evaluator checks whether the current principal is authenticated and, when roles are specified, belongs to at least one of those roles.

> [!WARNING]
> The current model-bound evaluator does **not** evaluate named policies. Do not rely on `[Authorize(Policy = "...")]`, a policy-derived ownership attribute, or `Microsoft.AspNetCore.Authorization.AuthorizeAttribute` to enforce a model-bound policy. Configured ASP.NET middleware/MVC authorization is a separate surface and can enforce its own requirements; it is not automatically the model-bound evaluator.

Treat policy support as a current implementation limitation. For a policy-based HTTP API, use an explicitly protected MVC action and test its middleware configuration. For rules needed across model-bound HTTP, direct pipeline execution, and hub subscriptions, implement a real [authorization query filter](../query-pipeline.md#query-filters). Do not substitute input validation for an authorization verdict.

## Require a role

Use the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names) with an authenticated host and the configured MongoDB provider. This replaces the earlier `DebitAccount` declaration:

```csharp
using System.Collections.Generic;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

namespace Banking.Accounts;

[ReadModel]
[Roles("AccountReader")]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Path("/api/accounts")]
    public static IEnumerable<DebitAccount> AllAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();

    [Roles("Admin", "Auditor")]
    [Path("/api/accounts/overdrawn")]
    public static IEnumerable<DebitAccount> OverdrawnAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(account => account.Balance < 0).ToList();
}
```

`AllAccounts` requires `AccountReader`. `OverdrawnAccounts` requires **Admin or Auditor**, replacing the type-level requirement rather than adding to it. These roles intentionally grant access to all matching accounts; this example does not claim owner-only access.

## Attribute precedence

For Arc's built-in attributes:

1. Method-level `[AllowAnonymous]` permits anonymous access.
2. Method-level `[Authorize]` or `[Roles]` takes precedence over the declaring type's requirements.
3. Otherwise type-level anonymous/authorization requirements apply.
4. Without a requirement, the default evaluator permits access.

Do not combine `[AllowAnonymous]` with `[Authorize]`/`[Roles]` on the same target: the built-in anonymous evaluator treats that combination as ambiguous.

Use `[Authorize]` for authentication alone and `[Roles("Admin", "Auditor")]` for authentication plus any listed role. A denied model-bound query does not invoke its method and produces `isAuthorized: false`. Direct HTTP normally maps that verdict to 403; hub denial is an `Unauthorized` message.

## Ownership is a separate rule

A role check does not establish record ownership. Neither a balance predicate nor a non-null user ID is an ownership check. If a read is owner-scoped, derive the owner from the authenticated principal's trusted identity mapping and constrain the database predicate to that owner **and** the requested record ID. Handle absent/unmapped identities by denying access, never by dropping the owner predicate.

There is no universal claim-to-owner mapping Arc can supply for your application. This page therefore does not provide a success-shaped ownership-policy stub. Test at least two owners, a missing identity, and an unauthorized caller through every exposed route/transport before using such a query for private data. Generated proxies provide type safety, not client-side security enforcement.

## Observable authorization

Authorization gates each new subscription. It does not automatically revoke a running stream when roles change or credentials expire. Use an [emission guard](../observable-query-emission-guards.md) backed by your current session/permission state when revocation must affect ongoing delivery.

Do not use [interceptor masking](../read-model-interception.md) as your only data-access control: observable HTTP snapshots currently bypass that interception path. Ensure the producer itself never yields fields or rows the caller must not receive.
