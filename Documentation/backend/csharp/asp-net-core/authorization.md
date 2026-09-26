---
title: Authorization in ASP.NET Core
description: Evaluate named policies and authentication schemes through the ASP.NET Core Arc host.
---

When your application uses `Cratis.Arc`, two layers can reject a request: ASP.NET Core middleware at the HTTP boundary and Arc filters in the command/query pipelines. Keep their configuration separate so a policy tested through HTTP is not mistaken for an in-process guarantee.

## The two boundaries

| Boundary | Checks | Applies to |
| --- | --- | --- |
| ASP.NET Core authorization | Microsoft endpoint/controller metadata, registered policies, schemes, default/fallback policy | Requests that traverse the configured middleware and matching endpoint metadata |
| Arc authorization filters | Authentication, roles, named Arc or ASP.NET Core policies, and selected authentication schemes | Commands and queries executed through Arc pipelines |

`Cratis.Arc.Authorization.RolesAttribute` derives from **Arc's** `AuthorizeAttribute`, not Microsoft's. Every authorization attribute on a declaration applies, so stacked attributes require all of them, as they do in ASP.NET Core. A policy name must resolve to exactly one registered Arc or ASP.NET Core policy; a missing or ambiguous policy fails startup.

With `Cratis.Arc`, Arc's pipeline also enforces **Microsoft's** `[Authorize]` and `[AllowAnonymous]` on model-bound commands and read models - authentication, roles, policies, and schemes, as it does for its own attributes. This check also runs for mapped HTTP commands/queries and new hub subscriptions, which ASP.NET Core middleware cannot protect on its own. Direct calls with no identity transition retain their existing scope; a scheme-selected direct call must use a safe Arc-owned execution scope, as described below. Both attribute families can be mixed; a method's declaration replaces its type's whichever family each comes from, and a declaration that is both anonymous and restricted is rejected as ambiguous. Arc's own attributes remain the portable choice: they are enforced on every Arc host, including Arc Core's own HTTP host, where nothing reads the Microsoft ones.

:::caution[Arc v18.2.0 through v22.20.0 did not enforce the Microsoft attributes]
When Arc Core gained its own attributes in v18.2.0, the ASP.NET Core evaluators meant to keep honoring Microsoft's resolved Arc's same-named attributes instead, so a model-bound command protected only with Microsoft's `[Authorize]` was open to every caller. Upgrade, or replace those attributes with `Cratis.Arc.Authorization`'s.
:::

## Setup

Configure a real authentication scheme for your deployment before adding the middleware. The following **configuration fragment** belongs in an existing `WebApplication` program after authentication service registration:

```csharp
using Cratis.Arc;
using Microsoft.AspNetCore.Authorization;

builder.AddCratisArc();
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseCratisArc();
app.Run();
```

See [Microsoft Identity integration](microsoft-identity.md) for one authentication option and its trusted-ingress prerequisites. A fallback policy is not a substitute for authentication service configuration.

## Protecting all endpoints by default

ASP.NET's fallback policy applies to endpoints without applicable authorization metadata. Its default policy applies when Microsoft `[Authorize]` supplies no named policy. Explicit anonymous metadata bypasses these policies.

> [!WARNING]
> Normal Arc activation maps development-user/tenant discovery, introspection, and identity-schema endpoints with anonymous metadata, including in Production. A fallback policy does **not** protect them. Review [production discovery exposure](../introspection/index.md) and restrict access at trusted ingress where necessary. Do not assume the word “development” is an environment check.

## Role-based authorization

For model-bound commands and queries, use explicit Arc imports. These are complete type fragments for an existing Arc host; neither needs a database or Chronicle:

```csharp
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

[Roles("Admin", "Manager")]
[Command]
public record ReviewRequest(Guid RequestId)
{
    public Guid Handle() => RequestId;
}
```

Arc requires authentication and at least one listed role. The returned `Guid` remains ordinary response data.

```csharp
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

[ReadModel]
public record ServiceStatus(string State)
{
    [Roles("Admin", "Auditor")]
    public static ServiceStatus Internal() => new("Running");
}
```

Arc query method attributes take precedence over read-model type attributes. For the full Arc rules, see [Core authorization](../core/authorization.md).

## Authorization in controllers

For ASP.NET controller policy enforcement, use **Microsoft** attributes explicitly. This complete controller fragment assumes MVC is registered and controllers are mapped in the host:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Manager")]
public class ReportsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { State = "Available" });
}
```

Microsoft controller/action requirements compose according to ASP.NET Core rules; do not transfer Arc's method-overrides-type rule to them. See [ASP.NET Core authorization](https://learn.microsoft.com/aspnet/core/security/authorization/introduction).

## Policy-based authorization

Register ASP.NET Core policies before building the host. Arc also evaluates these policies on model-bound artifacts reached through mapped HTTP endpoints and hub subscriptions, independently of middleware. This is a **configuration fragment** in an existing ASP.NET host:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ReportsReader", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("permission", "reports:read"));
```

After building the app and enabling its authentication/authorization middleware:

```csharp
app.MapGet("/reports/availability", () => new { Available = true })
    .RequireAuthorization("ReportsReader");
```

This protects that HTTP endpoint. To protect a model-bound command or query through **every** Arc route instead, annotate its type or query method with `[Cratis.Arc.Authorization.Authorize(Policy = "ReportsReader")]` or Microsoft's `[Authorize(Policy = "ReportsReader")]`. Arc evaluates the named policy inside its pipeline; it never assumes middleware has run. ASP.NET Core policy handlers receive the Arc `CommandContext` or `QueryContext` as their resource, not an MVC action resource.

Arc requires authentication before evaluating ASP.NET Core policies by default. For a policy that intentionally permits guests, register it with ASP.NET Core and then opt it in for Arc pipeline evaluation with `builder.Services.AddArcAnonymousAspNetAuthorizationPolicy("PublicOrMember")`. The policy must not call `RequireAuthenticatedUser()` (or include `DenyAnonymousAuthorizationRequirement`). Arc checks the opt-in name against registered ASP.NET Core policies at startup, ignoring case, and rejects missing, ambiguous, native Arc, or authentication-required policies. The standalone Core host cannot use this ASP.NET Core opt-in. Only declarations made entirely of opted-in policies without roles or authentication schemes can evaluate guests; the policies still decide whether to grant access. The opt-in changes Arc pipeline behavior, not MVC authorization or ASP.NET Core middleware.

An attribute's `AuthenticationSchemes` and any schemes contributed by the policy select the identities used for authentication, role checks, and policy evaluation. Arc calls ASP.NET Core's authentication service for each named scheme and combines the authenticated identities from successful schemes. A failed scheme does not cancel another scheme's success; if none authenticates, Arc denies access rather than falling back to the default principal. For a selected-scheme request, Arc runs the command or query in a fresh execution service scope; both `ICurrentPrincipalAccessor` and `IHttpContextAccessor.HttpContext.User` expose the selected identity while it executes. The HTTP accessor presents an operation-local native context: its selected `User` and `RequestServices` do not replace those on the shared connection object, and unrelated request features still come from the live request. Existing HTTP accessor registrations made **before** Arc is added are decorated, retaining their ordinary no-scheme behavior; replacing that registration **after** Arc disables safe scheme-selected execution and Arc rejects it rather than mutating a shared request. The built-in Arc ASP.NET request adapter also keeps selected `User` and `RequestServices` operation-local; a custom `IHttpRequestContext` that shares a native request should implement `IAuthorizationRequestContext` to avoid changing that shared object during identity selection. The prior request services, principal, and tenant cache are restored afterward. Arc rejects a scheme-selected invocation through an arbitrary caller-supplied `IServiceProvider`: an already-resolved tenant-bound service cannot be safely rebound. Use mapped HTTP/hub entry points, or the command pipeline's scope-owning `Execute(command)` overload from a live HTTP request. Queries with explicit schemes must enter through mapped HTTP or the hub, not a direct `IQueryPipeline.Perform(..., serviceProvider)` call. A call without a live HTTP context cannot satisfy a named scheme. A nested server-side execution using `BeginScope` and no named scheme remains supported; changing to a different scheme-selected identity after an outer command has bound a unit of work is rejected before joining that work. Configure and register every scheme before startup. On SSE, the live subscribe POST supplies the identity, not the older stream GET. Direct observable SSE/WebSocket emissions retain the live request and its selected identity, tenant, and execution provider until the stream ends; an ordinary no-scheme direct stream keeps the original request context and principal subtype. Hub emissions after subscription admission use the captured Arc principal and tenant, but deliberately expose **no native `HttpContext`**: the subscribe POST may already be disposed, and the emitting thread may belong to another user. Use `ICurrentPrincipalAccessor`, the Arc request context, and the emission context's provider in hub guards/interceptors instead of `IHttpContextAccessor` for those later emissions. Custom host runtimes can implement `IAuthorizationEmissionRuntime` to capture a live direct request and suppress unavailable native HTTP in hub emissions. Policy checks gate admission, not revocation. See [observable emission guards](../queries/observable-query-emission-guards.md) for revocation after admission.

To register a portable scoped policy instead, use [Core named policies](../core/authorization.md#register-a-named-policy). Register a name in either the native Arc registry or ASP.NET Core, not both.

## Authorization results for commands and queries

Arc authorization failures return results with `IsAuthorized = false` and skip the handler/query logic. Mapped Arc results use HTTP 403; ASP.NET middleware can challenge or forbid before Arc executes and may return a different response shape.

An ordinary call to `ServiceStatus.Internal()` bypasses the query pipeline and **does not evaluate attributes**. Calling a command's `Handle()` directly likewise bypasses authorization, validation, filters, and result handling. Use the [query pipeline](../queries/query-pipeline.md), [command pipeline](../commands/command-pipeline.md), or mapped HTTP endpoint when those guarantees matter.

## Custom authorization

A command filter denying permission returns `CommandResult.Unauthorized(context.CorrelationId)`; a query filter uses `QueryResult.Unauthorized(context.CorrelationId)`. `CommandResult.Error(...)` describes an exception result, not an authorization rejection. See [command filters](../commands/command-filters.md) for filter contracts.

Read the trusted principal through `ICurrentPrincipalAccessor` (`Cratis.Arc.Authorization`), or `IHttpContextAccessor.HttpContext.User` when intentionally writing ASP.NET-specific code. `IProvideIdentityDetails` composes frontend details; returning roles or `IsUserAuthorized` in its payload does not add claims or authorize every command/query. The [identity cookie](../identity/identity-provider-service.md) is client-controlled and must not be used as authorization evidence.

## Verification checklist

Test anonymous, authenticated-without-role, and permitted-role callers through both HTTP and direct pipelines where the latter are supported. Where HTTP policies or schemes matter, test wrong-policy/wrong-scheme principals against the actual endpoint and verify scoped collaborators use the selected tenant. Include discovery endpoints with Production settings and a fallback policy enabled. Test custom ownership and tenant membership checks independently of UI visibility.

## See also

- [Core authorization](../core/authorization.md) — host-independent attribute and principal contracts.
- [Identity](../identity/index.md) — identity enrichment and its trust boundary.
- [Microsoft Identity](microsoft-identity.md) — authentication setup.
- [Tenancy](../tenancy/index.md) — selection is not membership authorization.
- [Proxy generation](../proxy-generation/index.md) — clients transport results; they do not enforce server permissions.
