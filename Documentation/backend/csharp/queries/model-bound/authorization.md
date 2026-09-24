---
title: Model-bound query authorization
description: Protect reads with authentication, roles, named policies, and selected schemes.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Choose the right authorization surface

For model-bound queries, use attributes from **`Cratis.Arc.Authorization`**. The default evaluator checks whether the current principal is authenticated and, when roles are specified, belongs to at least one of those roles.

Register a native scoped policy using [Core named policies](../../core/authorization.md#register-a-named-policy), then put `[Authorize(Policy = "ActiveSubscription")]` on the read model or a query method. Arc awaits every named policy before invoking the method, whether the call comes from HTTP, the query pipeline, or a new hub subscription. Unknown and duplicate policies fail host startup. Method-level declarations replace type-level declarations, while stacked attributes on one declaration all apply. The selected principal must be authenticated, even when the policy itself would allow anonymous access.

On ASP.NET Core, both Arc and Microsoft's authorization attributes can name ASP.NET policies or select registered authentication schemes. Arc authenticates an explicitly requested or policy-contributed scheme instead of trusting the default request identity; on SSE it uses the live subscribe POST. Scheme-selected queries construct authorization filters and later query dependencies under the same selected principal and tenant in a fresh Arc-owned service scope. A direct `IQueryPipeline.Perform(..., serviceProvider)` call with an arbitrary provider cannot safely rebind already-resolved identity-dependent services and fails closed if it selects a different identity; use mapped HTTP or a hub subscription. The standalone Core host rejects `AuthenticationSchemes` at startup (see [ARC0021](../../code-analysis/index.md#arc0021-unevaluated-authorization-settings)). ASP.NET Core middleware/MVC authorization remains a separate HTTP boundary. Do not substitute input validation for an authorization verdict.

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

Do not combine `[AllowAnonymous]` with `[Authorize]`/`[Roles]` on the same target, from either attribute family: the evaluator rejects that combination as ambiguous, and analyzer [ARC0019](../../code-analysis/index.md#arc0019-conflicting-authorization) reports it at build time.

Use `[Authorize]` for authentication alone and `[Roles("Admin", "Auditor")]` for authentication plus any listed role. A denied model-bound query does not invoke its method and produces `isAuthorized: false`. Direct HTTP normally maps that verdict to 403; hub denial is an `Unauthorized` message.

## Ownership is a separate rule

A role check does not establish record ownership. Neither a balance predicate nor a non-null user ID is an ownership check. If a read is owner-scoped, derive the owner from the authenticated principal's trusted identity mapping and constrain the database predicate to that owner **and** the requested record ID. Handle absent/unmapped identities by denying access, never by dropping the owner predicate.

There is no universal claim-to-owner mapping Arc can supply for your application. This page therefore does not provide a success-shaped ownership-policy stub. Test at least two owners, a missing identity, and an unauthorized caller through every exposed route/transport before using such a query for private data. Generated proxies provide type safety, not client-side security enforcement.

## Observable authorization

Authorization gates each new subscription. Direct SSE/WebSocket streams keep the live request's selected principal, tenant, and service provider through each emission. Emissions from another producer's execution flow restore the subscriber's identity (including on controller-based streams without policy metadata). On disconnect, direct streams stop accepting emissions and wait for in-flight interception and guards before releasing their request resources; a normally completed stream delivers already accepted emissions before ending. The hub snapshots the selected principal, typed arguments, and tenant for subsequent emissions, so guards run with the admitting identity even after the subscribe POST ends. Hub emissions have no native ASP.NET Core `HttpContext` after admission: a subscribe POST has ended and an emission can originate under another request's identity. Resolve identity through `ICurrentPrincipalAccessor` or the Arc emission context rather than native HTTP inside a hub emission guard. It does not automatically revoke a running stream when roles change or credentials expire. Use an [emission guard](../observable-query-emission-guards.md) backed by your current session/permission state when revocation must affect ongoing delivery.

Do not use [interceptor masking](../read-model-interception.md) as your only data-access control: observable HTTP snapshots currently bypass that interception path. Ensure the producer itself never yields fields or rows the caller must not receive.
