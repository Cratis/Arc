---
title: Cratis Arc tenant membership checks
description: Register an application-owned ITenantMembership check in Cratis Arc to verify that callers may use the selected tenant before accessing its data.
---

Cratis Arc tenant membership checks verify that a caller may use the tenant selected from a request. If a caller sends `x-cratis-tenant-id: acme`, the header resolver selects `acme`; an application-owned `ITenantMembership` hook can allow or deny that selection before tenant data is accessed. Alternatively, enforce caller-to-tenant membership at a trusted ingress.

A tenant's existence does not establish caller membership. Cratis AuthProxy's optional tenant-existence verification checks that the tenant exists, not that the caller belongs to it. That check alone does not establish membership enforcement for an Arc backend.

Base the decision on validated authentication and authoritative membership data. A claim is useful only when its trusted issuer establishes the caller's permission to use that tenant. Membership then answers which tenant the caller may use; authorization for the requested operation remains a separate decision.

## Check membership in the application

Register the hook before building an ASP.NET Core host or an `ArcApplicationBuilder` host. This fragment assumes authentication has already validated the identity and loaded its trusted `tenant` claims before Arc handles the request:

```csharp
using System.Security.Claims;
using Cratis.Arc;
using Cratis.Arc.Tenancy;

builder.AddCratisArc(options => options.UseHeaderTenancy());
builder.Services.AddScoped<ITenantMembership, ClaimsTenantMembership>();

public sealed class ClaimsTenantMembership : ITenantMembership
{
    public bool IsMember(ClaimsPrincipal principal, TenantId tenantId) =>
        principal.Identity?.IsAuthenticated == true &&
        tenantId != TenantId.NotSet &&
        principal.HasClaim("tenant", tenantId.Value);
}
```

Arc passes the normalized candidate tenant and the effective caller's `ClaimsPrincipal` to the hook. In this example, an authenticated caller with a trusted `tenant=acme` claim can use `acme`. A caller without that claim receives HTTP 403 when an Arc command or query tries to use the tenant. Requests without the header resolve to `TenantId.NotSet`, which this implementation also denies. A denial uses the normal unauthorized result without exception details, even when exception detail exposure is enabled.

Register the hook explicitly; convention-based registration does not supply one. Without a registration, resolver behavior stays unchanged and Arc does not automatically enforce membership. If you register several hooks, all must allow the candidate. Returning `false`, throwing, or failing to resolve a registered hook denies access.

When no principal is available, Arc passes an unauthenticated `ClaimsPrincipal`; the hook decides whether to allow it. Arc also checks `TenantId.NotSet`, so decide explicitly whether an unset tenant is permitted. Check `TenantId.Default` too if your rule must reject both default aliases.

### Keep the hook synchronous

`ITenantIdAccessor.Current` is synchronous and may be read while constructing a service. Its membership hook must therefore answer synchronously too. Use trusted claims or in-memory membership data loaded before entering Arc. If membership requires a database lookup, preload that data in authentication or request middleware before tenant resolution. Do not perform I/O, block on an async task, or read `ITenantIdAccessor.Current` recursively from the hook.

Arc resolves hooks from the current request's service provider, so scoped registrations work. It resolves the instances once per request or observable-query subscription and reuses them only there. A transient registration consequently behaves like a scoped one within a request.

Reusing a hook instance does not cache its decision. Each verified read checks the candidate and effective principal again, so changed membership data does not reuse a previous successful decision. During an authorized operation, Arc keeps the selected principal and tenant snapshot visible for that operation.

Explicit `ITenantScope.Begin` scopes bypass request membership checks because they represent a tenant chosen by trusted application code. Make the required authorization decision before beginning one.

## Recognize the startup warning

Arc logs one warning at host startup when the active resolver is the built-in header, query or subdomain resolver and there is no membership hook. The warning names the resolver and links to this page. It is a diagnostic, not a startup failure. Claim, fixed, development and custom tenancy resolvers do not produce it. The check does not construct hooks in the normal Microsoft DI container, including scoped hooks whose dependencies are available only during a request.

If your ingress actually enforces caller-to-tenant membership, explicitly acknowledge that boundary to suppress the warning. AuthProxy's tenant-existence verification alone does not meet this requirement:

```json
{
  "Cratis": {
    "Arc": {
      "Tenancy": {
        "IngressValidatesTenant": true
      }
    }
  }
}
```

The default is `false`. `IngressValidatesTenant` is a trusted operator assertion, not topology autodetection. Setting it to `true` **only suppresses the warning**: it does not validate requests, skip a registered hook, or change tenant resolution. Neither the warning nor its suppression provides automatic protection.

Enable it only when the ingress enforces caller-to-tenant membership and direct backend bypass is prevented. Isolate backend listeners so callers cannot reach them except through that ingress, and trust forwarded identity and tenant information only from that boundary. Apply the same boundary to HTTP, WebSocket and SSE traffic; Arc cannot infer or enforce this deployment isolation from the setting.

## Membership denials in validators

By default, a `TenantMembershipDenied` thrown by a validator, for example through an injected tenant accessor, becomes a `ValidatorFailed` validation error. Raising the allowed validation severity can cause that error to be dropped. Set `Tenancy:StrictValidatorMembership` to `true` so a membership denial rejects the operation as unauthorized instead. Enable it when validator code can read the tenant.

Reading the tenant through the validation context (`GetTenant()`) is always strict, whatever this setting is. See [reading the tenant and caller](../commands/validation.md#reading-the-tenant-and-caller).

## Boundary of the check

Registering `ITenantMembership` opts in to server-side enforcement when request code resolves a tenant. That includes Arc HTTP command/query endpoints, ASP.NET Core controllers and native HTTP hosting, but it does not authorize every endpoint automatically. An application that replaces Arc's tenant accessor must provide its own membership enforcement.

For observable queries, Arc checks WebSocket and SSE connections before upgrading or committing streaming headers when a hook is registered. It also checks each subscription's effective principal and tenant when the subscription starts, not on every emission. An explicitly selected application tenant remains trusted.

Background work without an HTTP request does not invoke the hook. Make the authorization decision before starting that work, then use an explicit tenant scope. Membership checks do not infer permission from arbitrary claims, add tenant filters to queries, or change database naming. See [MongoDB database isolation](database-resolvers.md) for destination selection, or continue with [tenant context access](tenant-context.md) to use the tenant in application services.
