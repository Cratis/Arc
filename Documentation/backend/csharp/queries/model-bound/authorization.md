---
title: Model-bound query authorization
description: Secure model-bound queries in Cratis Arc with read-model and method-level authorization, then restrict account data to the caller's scope.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Model-bound query authorization in Cratis Arc decides who may run a read before its static method is invoked. Apply a declaration to the read model to protect its queries together, or to a method when that read needs different access. Separately, constrain the database query to the rows the caller may receive.

## Choose the right authorization surface

Use attributes from `Cratis.Arc.Authorization` for declarations shared by standalone Arc and ASP.NET Core hosts. `[Authorize]` requires authentication; `[Roles]` also checks role membership. The [role example below](#require-a-role) protects access to the query, while [ownership](#ownership-is-a-separate-rule) needs a rule that restricts the data itself.

When roles cannot express your rule, register a scoped policy using [Core named policies](../../core/authorization.md#register-a-named-policy), then apply `[Authorize(Policy = "ActiveSubscription")]`. Arc awaits every named policy before invoking the method through HTTP, the query pipeline, or a new hub subscription. Unknown and duplicate policies fail host startup.

Policies normally require authentication before their logic runs. To let a policy decide for guests, explicitly register it with `evaluatesAnonymous: true`; every stacked requirement must then be an opted-in policy with no roles or authentication schemes. Follow the [guest-policy registration rules](../../core/authorization.md#let-a-policy-evaluate-guests) rather than combining a policy with `[AllowAnonymous]`.

## Require a role

Require `AccountReader` to call `AllAccounts`, and a different role for the overdrawn-account read. Use the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names) with an authenticated host and the configured MongoDB provider. This replaces the earlier `DebitAccount` declaration:

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
4. With no explicit declaration on either method or type, Arc applies any requirements supplied by `IFallbackAuthorizationEvaluator`.
5. With neither explicit nor baseline requirements, the core evaluator permits access. Host admission checks can still apply.

Do not combine `[AllowAnonymous]` with `[Authorize]`/`[Roles]` on the same target, from either attribute family: the evaluator rejects that combination as ambiguous, and analyzer [ARC0019](../../code-analysis/index.md#arc0019-conflicting-authorization) reports it at build time.

A single `[Roles("Admin", "Auditor")]` accepts either role. Stacking `[Roles("Admin")]` and `[Roles("Auditor")]` on the same member requires both; all stacked requirements must pass. For requirements on otherwise unprotected queries, see [baseline authorization](../../core/authorization.md#set-baseline-requirements-without-overriding-anonymous-access).

A denied model-bound query does not invoke its method and produces `isAuthorized: false`. Direct HTTP normally maps that verdict to 403; hub denial is an `Unauthorized` message. Check the selected identity and the query's role or policy requirement before treating missing data as an account that was not found.

## Ownership is a separate rule

A role check does not establish record ownership. For an owner-scoped read, derive the owner from the authenticated principal's trusted identity mapping and constrain the database predicate to both that owner and the requested record ID. A balance predicate or non-null user ID alone cannot establish ownership. Deny access when an identity is absent or unmapped; never recover by dropping the owner predicate.

Your application defines the mapping from trusted claims to account ownership. Test it with at least two owners, a missing identity, and an unauthorized caller through every exposed route and transport before serving private data. Generated proxies provide type safety, not client-side security enforcement.

## Select an authentication scheme

On ASP.NET Core, both Arc and Microsoft's authorization attributes can name ASP.NET Core policies or select registered authentication schemes. Arc authenticates an explicitly requested or policy-contributed scheme instead of trusting the default request identity; for hub Server-Sent Events (SSE) subscriptions, it uses the live subscribe POST. Authorization filters and later query dependencies are then constructed under the selected principal and tenant in a fresh Arc-owned service scope.

A direct `IQueryPipeline.Perform(..., serviceProvider)` call with an arbitrary provider cannot safely rebind already-resolved, identity-dependent services. It fails closed if scheme selection changes the identity; use mapped HTTP or a hub subscription for that case. The standalone Core host rejects `AuthenticationSchemes` at startup (see [ARC0021](../../code-analysis/index.md#arc0021-unevaluated-authorization-settings)). ASP.NET Core middleware and MVC authorization remain a separate HTTP boundary.

`AuthorizationPolicyContext.ReceivedAt` matches the executing `QueryContext.ReceivedAt`, including after scheme selection or repeated authorization. Each hub subscribe operation gets a new receipt, rather than sharing the connection's receipt; see [query context](../query-pipeline.md#query-context-and-renderers).

## Observable authorization

Authorization gates each new subscription; it does not automatically revoke a running stream when roles change or credentials expire. Use an [emission guard](../observable-query-emission-guards.md) backed by current session or permission state when revocation must affect ongoing delivery.

Direct SSE and WebSocket streams retain the live request's selected principal, tenant, and service provider for each emission. Arc restores the subscriber's identity when an emission arrives from another producer's execution flow, including for controller-based streams without policy metadata. On disconnect, direct streams stop accepting emissions and wait for in-flight interception and guards before releasing request resources. Normal completion delivers already accepted emissions before ending.

The hub snapshots the selected principal, typed arguments, and tenant so later guards run with the identity that admitted the subscription. There is no native ASP.NET Core `HttpContext` for hub emissions after admission: the subscribe POST has ended, and an emission may originate under another request's identity. Resolve identity through `ICurrentPrincipalAccessor` or the Arc emission context inside a hub guard.

Observable HTTP snapshots apply [read-model interception](../read-model-interception.md) before emission guards. Masking can shape the result, but it does not replace authorization or owner-scoped data selection. Ensure the producer itself never yields fields or rows the caller must not receive. Continue with [observable queries](observable-queries.md) to configure the producer and choose how clients receive updates.
