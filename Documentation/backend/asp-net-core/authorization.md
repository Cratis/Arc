---
title: Authorization in ASP.NET Core
description: Separate ASP.NET endpoint policies from Arc command and query authorization.
---

When your application uses `Cratis.Arc`, two layers can reject a request: ASP.NET Core middleware at the HTTP boundary and Arc filters in the command/query pipelines. Keep their configuration separate so a policy tested through HTTP is not mistaken for an in-process guarantee.

## The two boundaries

| Boundary | Checks | Applies to |
| --- | --- | --- |
| ASP.NET Core authorization | Microsoft endpoint/controller metadata, registered policies, schemes, default/fallback policy | Requests that traverse the configured middleware and matching endpoint metadata |
| Arc authorization filters | Authenticated principal and OR-role membership from Arc attributes | Commands and queries executed through Arc pipelines |

`Cratis.Arc.Authorization.RolesAttribute` derives from **Arc's** `AuthorizeAttribute`, not Microsoft's. Arc's evaluator does not evaluate `Policy` or `AuthenticationSchemes`, and it takes the first applicable authorization attribute instead of combining multiple attributes. Do not stack Arc attributes to express AND requirements.

Current limitation: the ASP.NET adapter's authorization evaluator resolves Arc's attribute type too. Do not rely on a Microsoft `[Authorize]` placed only on a model-bound command record to protect direct pipeline calls. This does **not** mean Microsoft authorization cannot work: middleware can enforce Microsoft metadata on controllers or explicitly configured endpoints. Verify the actual HTTP route and the pipeline separately.

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

Register policies with ASP.NET Core and apply them to an endpoint that participates in its middleware. These are **configuration fragments** in an existing ASP.NET host:

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

This protects that HTTP endpoint. It does not attach the policy to unrelated generated Arc routes or to direct pipeline calls. For permissions that must hold through every transport, implement a server-side permission check in the Arc pipeline using trusted claims and authoritative application data.

## Authorization results for commands and queries

Arc authorization failures return results with `IsAuthorized = false` and skip the handler/query logic. Mapped Arc results use HTTP 403; ASP.NET middleware can challenge or forbid before Arc executes and may return a different response shape.

An ordinary call to `ServiceStatus.Internal()` bypasses the query pipeline and **does not evaluate attributes**. Calling a command's `Handle()` directly likewise bypasses authorization, validation, filters, and result handling. Use the [query pipeline](../queries/query-pipeline.md), [command pipeline](../commands/command-pipeline.md), or mapped HTTP endpoint when those guarantees matter.

## Custom authorization

A command filter denying permission returns `CommandResult.Unauthorized(context.CorrelationId)`; a query filter uses `QueryResult.Unauthorized(context.CorrelationId)`. `CommandResult.Error(...)` describes an exception result, not an authorization rejection. See [command filters](../commands/command-filters.md) for filter contracts.

Read the trusted principal through `ICurrentPrincipalAccessor` (`Cratis.Arc.Authorization`), or `IHttpContextAccessor.HttpContext.User` when intentionally writing ASP.NET-specific code. `IProvideIdentityDetails` composes frontend details; returning roles or `IsUserAuthorized` in its payload does not add claims or authorize every command/query. The [identity cookie](../identity/identity-provider-service.md) is client-controlled and must not be used as authorization evidence.

## Verification checklist

Test anonymous, authenticated-without-role, and permitted-role callers through both HTTP and direct pipelines. Where HTTP policies or schemes matter, test wrong-policy/wrong-scheme principals against the actual endpoint. Include discovery endpoints with Production settings and a fallback policy enabled. Test custom ownership and tenant membership checks independently of UI visibility.

## See also

- [Core authorization](../core/authorization.md) — host-independent attribute and principal contracts.
- [Identity](../identity/index.md) — identity enrichment and its trust boundary.
- [Microsoft Identity](microsoft-identity.md) — authentication setup.
- [Tenancy](../tenancy/index.md) — selection is not membership authorization.
- [Proxy generation](../proxy-generation/index.md) — clients transport results; they do not enforce server permissions.
