---
title: Tenant Membership
description: Verify that the effective caller may use a tenant selected from a request.
---

A caller sends `x-cratis-tenant-id: acme`. The header resolver selects `acme`, but the header does not prove that the caller belongs to it. Put Cratis AuthProxy in front of Arc, select a tenant from a trusted claim, or register an application-owned `ITenantMembership` check.

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

When Arc resolves the request tenant, it passes the normalized candidate and the effective principal to the hook. A caller with a trusted `tenant=acme` claim can use `acme`; a caller without it receives HTTP 403 when an Arc command or query tries to use that tenant. The sample also denies requests without a tenant header, because those resolve to `TenantId.NotSet`. A denial uses the normal unauthorized result without exception details, even if exception detail exposure is enabled.

The hook is optional, not convention-provided. Without a registration, resolver behavior remains unchanged. Registering multiple hooks requires **all** of them to allow the candidate. Returning `false`, throwing, or failing to resolve a registered hook denies access. An absent principal is represented by an unauthenticated `ClaimsPrincipal`; each hook decides whether to permit it. `TenantId.NotSet` is checked too: explicitly decide whether the application's default tenant is allowed.

### Keep the hook synchronous

`ITenantIdAccessor.Current` is a synchronous property and can be used while constructing services. Consequently `ITenantMembership.IsMember` must be fast and synchronous: use trusted claims or in-memory membership data that was loaded before entering Arc. Do not perform I/O, block on an async task, or resolve `ITenantIdAccessor.Current` recursively from the hook. If membership needs a database lookup, preload it in authentication or request middleware before Arc tenant resolution.

Hooks are resolved from the current request's service provider, so scoped registrations are supported. Verified reads are not cached: changing the effective principal or membership data does not reuse a previous successful check. During an authorized operation, Arc keeps its selected principal and tenant snapshot visible for that operation. Explicit `ITenantScope.Begin` scopes are trusted application code and bypass request membership checks; use them only after making the required authorization decision.

## Recognize the startup warning

Arc logs one warning at host startup when the active resolver is the built-in header, query or subdomain resolver and there is no membership hook. The warning names the resolver and links to this page. It is a diagnostic, not a startup failure. Claim, fixed, development and custom tenancy resolvers do not produce it. The check does not construct hooks in the normal Microsoft DI container, including scoped hooks whose dependencies are available only during a request.

When Cratis AuthProxy (or another trusted ingress) already validates tenant membership, acknowledge that boundary to suppress the warning:

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

The default is `false`. Setting it to `true` **only suppresses the warning**: it does not validate requests, skip a registered hook, or change tenant resolution. Only set it when the ingress really verifies membership and the application cannot be reached around that ingress.

## Boundary of the check

The hook verifies a candidate when request code resolves a tenant; it is not blanket authorization for every endpoint. Arc checks observable WebSocket and SSE connections before upgrading or committing streaming headers when a hook is registered, and still checks each subscription's effective principal and tenant. An explicitly selected application tenant remains trusted. Membership is checked when a subscription starts, not on every emission; an application replacing Arc's tenant accessor is responsible for its own membership enforcement.

Background work without an HTTP request does not invoke the hook. Use an explicit tenant scope and make the authorization decision before starting that work. This feature does not infer memberships from arbitrary claims, add tenant filters to queries, or change database naming. [MongoDB database isolation](database-resolvers.md), tenant resolution and caller membership remain distinct responsibilities. Continue with [tenant context access](tenant-context.md) to use the verified tenant in application services.
