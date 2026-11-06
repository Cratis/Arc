---
title: Configure Cratis Arc authorization in ASP.NET Core
description: Protect Cratis Arc commands and queries with roles, policies, and authentication schemes, while keeping HTTP and pipeline authorization distinct.
---

Cratis Arc authorization protects commands and queries inside their execution pipelines; ASP.NET Core authorization protects requests at the HTTP boundary. Configure both when operations can be reached through HTTP and server-side pipeline calls. A successful HTTP policy test alone does not establish that an in-process call is protected.

## The two boundaries

| Boundary | Checks | Applies to |
| --- | --- | --- |
| ASP.NET Core authorization | Microsoft endpoint/controller metadata, registered policies, schemes, default/fallback policy | Requests that traverse the configured middleware and matching endpoint metadata |
| Arc authorization filters | Authentication, roles, named Arc or ASP.NET Core policies, and selected authentication schemes | Commands and queries executed through Arc pipelines |

`Cratis.Arc.Authorization.RolesAttribute` derives from Arc's `AuthorizeAttribute`, not Microsoft's. Every authorization attribute on a declaration applies, so stacked attributes must all be satisfied. A policy name must resolve to exactly one registered Arc or ASP.NET Core policy; a missing or ambiguous policy fails startup.

In the ASP.NET Core host, Arc's pipeline also enforces Microsoft's `[Authorize]` and `[AllowAnonymous]` on model-bound commands and read models. It checks authentication, roles, policies, and schemes for mapped HTTP operations and new hub subscriptions too. Middleware alone cannot protect all of those paths.

You can mix the two attribute families. A method's declaration replaces its type's declaration, regardless of which family each uses. A declaration that is both anonymous and restricted is rejected as ambiguous. Use Arc's own attributes for portable contracts: the standalone Arc Core host enforces them but does not read Microsoft's attributes.

Direct pipeline calls that do not change identity keep their existing service scope. Calls that select an authentication scheme need an Arc-owned execution scope; see [authentication scheme selection](#authentication-scheme-selection) before making those calls from server code.

:::caution[Arc v18.2.0 through v22.20.0 did not enforce the Microsoft attributes]
When Arc Core gained its own attributes in v18.2.0, the ASP.NET Core evaluators meant to keep honoring Microsoft's resolved Arc's same-named attributes instead, so a model-bound command protected only with Microsoft's `[Authorize]` was open to every caller. Upgrade, or replace those attributes with `Cratis.Arc.Authorization`'s.
:::

## Setup

Configure a real authentication scheme for your deployment before adding the middleware. The following **configuration fragment** belongs in an existing `WebApplication` program after authentication service registration:

```csharp
using Cratis.Arc;
using Microsoft.AspNetCore.Authorization;

builder.AddCratisArc();
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
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

ASP.NET's `AuthorizationOptions.FallbackPolicy`, configured above with `builder.Services.AddAuthorization(...)`, applies to HTTP endpoints without applicable authorization metadata, including MVC controller actions. Its default policy applies when Microsoft `[Authorize]` supplies no named policy. Explicit anonymous metadata bypasses these policies.

Observable query hub routes remain anonymous. Before executing a non-anonymous query subscription, the hub evaluates `FallbackPolicy` separately. It authenticates schemes named by that policy against the live WebSocket upgrade or server-sent events (SSE) subscribe POST, then evaluates the policy with that `HttpContext` as its resource. A denial or policy failure produces an `Unauthorized` frame; SSE subscribe also returns 401.

Queries allowing anonymous access or [opted-in guest policy evaluation](#policies-that-can-evaluate-guests) bypass the host fallback, matching their mapped direct endpoints. This host check does not add a fallback policy to in-process pipeline calls.

Arc's [`IFallbackAuthorizationEvaluator`](../core/authorization.md#set-baseline-requirements-without-overriding-anonymous-access) is separate: it supplies baseline requirements for model-bound commands and queries on both Arc hosts, and for controller-based queries reached through Arc's observable hub path in the ASP.NET Core host. It also covers server-side pipeline invocations that never pass through ASP.NET middleware. Direct HTTP GET and WebSocket requests to controller-based queries are MVC actions, not covered by `IFallbackAuthorizationEvaluator`. Use ASP.NET Core's `FallbackPolicy` to require authorization for those actions and controller-based commands without explicit authorization metadata.

> [!WARNING]
> Normal Arc activation maps the discovery endpoints (command/query introspection, development-user/tenant discovery and the identity schema) anonymously only in Development; elsewhere they require an authenticated caller unless `ArcOptions.Introspection.RequireAuthentication` is `false`. A fallback policy does **not** protect endpoints that are explicitly anonymous. Review [production discovery exposure](../introspection/index.md#production-access).

## Verified capability callers and scopes

On a capability listener, the [verified caller](verified-callers.md) becomes the principal used by Arc authorization, `ICurrentPrincipalAccessor`, and Chronicle identity/causation. Product roles, policies, and live tenant membership still apply to that subject. A signed tenant is only a candidate, not proof of membership. A different authenticated subject is refused instead of merged.

Use `[RequiresScope]` to narrow an operation against the verified credential's scope ceiling. This complete type fragment belongs in an installed capability host:

```csharp
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

[Command, Roles("editor"), RequiresScope("content:write")]
public record PublishContent
{
    public string Handle() => "published";
}
```

Both requirements apply: the caller needs the product's editor role and the credential's exact `content:write` scope. An absent scope ceiling is empty. A role, policy, or same-subject product scheme cannot add scopes to the credential ceiling. Scopes never authorize a person who fails product authorization. Role enrichment must preserve the verified subject on each authenticated identity it adds.

See the acceptance sample in the repository's `Samples/VerifiedCallerAcceptance` folder for real HTTP checks of roles, tenant membership, scope narrowing, and subject conflicts. PAT, OAuth-grant, service-account, and on-behalf installation are not supplied by the browser-session installer.

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

`RequireAuthorization` protects that HTTP endpoint. To protect a model-bound command or query through every Arc route, annotate its type or query method with `[Cratis.Arc.Authorization.Authorize(Policy = "ReportsReader")]` or Microsoft's `[Authorize(Policy = "ReportsReader")]`. Arc evaluates the named policy inside its pipeline without assuming middleware has run.

For a pipeline invocation, ASP.NET Core policy handlers receive the Arc `CommandContext` or `QueryContext` as their resource, not an MVC action resource. Its `ReceivedAt` is fixed at Arc dispatch, before binding and scheme authentication, and survives a scheme-selected service scope. It is not network arrival time or a timestamp from before application middleware. MVC authorization remains a separate boundary and does not use this Arc resource.

### Policies that can evaluate guests

Arc normally requires authentication before evaluating ASP.NET Core policies. If a policy intentionally permits guests, register it with ASP.NET Core, then opt it in for Arc pipeline evaluation with `builder.Services.AddArcAnonymousAspNetAuthorizationPolicy("PublicOrMember")`. The standalone Core host cannot use this ASP.NET Core opt-in.

The policy must not call `RequireAuthenticatedUser()`, include `DenyAnonymousAuthorizationRequirement`, or specify `AuthenticationSchemes`. Arc validates the opt-in through the ASP.NET Core policy provider at startup and rejects missing, ambiguous, native Arc, authentication-required, or scheme-selected policies.

Match the opt-in name to the command or query's policy declaration exactly, including case. An opt-in for `Guest` does not opt in `guest`, even if the provider resolves names case-insensitively. Only declarations composed entirely of opted-in policies, without roles or authentication schemes, can evaluate guests. Those policies still decide whether to grant access.

For mapped model-bound command and query endpoints, Arc emits anonymous HTTP metadata only when the entire effective declaration qualifies for guest evaluation. This lets guests reach the Arc policy even with an authentication-requiring `FallbackPolicy`; command validation and query GET/QUERY endpoints use the same rule. The Arc pipeline still evaluates every policy and returns 403 on denial without running command/query logic. Do not combine the policy declaration with `[AllowAnonymous]`. Ordinary policies, mixed guest/ordinary requirements, roles, and authentication schemes retain their existing HTTP authentication behavior. The opt-in does not change MVC authorization or unrelated endpoints.

For a portable scoped policy, use [Core named policies](../core/authorization.md#register-a-named-policy). Register a name in either the native Arc registry or ASP.NET Core, not both.

### Authentication scheme selection

An attribute's `AuthenticationSchemes`, together with schemes contributed by its policy, selects the identities used for authentication, role checks, and policy evaluation. Register every scheme before startup. Arc calls ASP.NET Core's authentication service for each named scheme and combines the authenticated identities from successful schemes. One failed scheme does not cancel another's success. If none authenticates, Arc denies access without falling back to the default principal.

Arc runs a scheme-selected command or query in a fresh execution service scope. During execution, `ICurrentPrincipalAccessor` and `IHttpContextAccessor.HttpContext.User` expose the selected identity. The HTTP accessor supplies an operation-local native context: selected `User` and `RequestServices` do not replace those on the shared connection, while unrelated request features still come from the live request. Arc restores the prior request services, principal, and tenant cache afterward.

If you register an HTTP accessor before adding Arc, Arc decorates it and retains its ordinary no-scheme behavior. Replacing the registration after adding Arc prevents safe scheme-selected execution, so Arc rejects that execution instead of mutating a shared request. The built-in ASP.NET request adapter also keeps `User` and `RequestServices` operation-local. A custom `IHttpRequestContext` that shares a native request should implement `IAuthorizationRequestContext` for the same isolation.

### Calling scheme-selected operations from server code

Arc rejects scheme-selected execution through an arbitrary caller-supplied `IServiceProvider`, because it cannot safely rebind an already-resolved tenant-bound service. Use mapped HTTP or hub entry points. For commands, you can also use the pipeline's scope-owning `Execute(command)` overload from a live HTTP request. Queries with explicit schemes must enter through mapped HTTP or the hub, not a direct `IQueryPipeline.Perform(..., serviceProvider)` call. A call without a live HTTP context cannot satisfy a named scheme.

Nested server-side execution using `BeginScope` without a named scheme remains supported. Changing to a different scheme-selected identity after an outer command has bound a unit of work is rejected before joining that work.

### Identity during observable query emissions

For SSE, the live subscribe POST supplies the subscription identity, not the older stream GET. Direct observable SSE/WebSocket emissions retain the live request and its selected identity, tenant, and execution provider until the stream ends. An ordinary no-scheme direct stream keeps the original request context and principal subtype.

Hub emissions after subscription admission use the captured Arc principal and tenant, but expose no native `HttpContext`. The subscribe POST may already be disposed, and the emitting thread may belong to another user. In hub guards and interceptors, use `ICurrentPrincipalAccessor`, the Arc request context, and the emission context's provider instead of `IHttpContextAccessor`.

Custom host runtimes can implement `IAuthorizationEmissionRuntime` to capture a live direct request and suppress unavailable native HTTP in hub emissions. Policy checks control admission, not later revocation. Use [observable emission guards](../queries/observable-query-emission-guards.md) when permissions must be checked again after admission.

## Authorization results for commands and queries

Arc authorization failures return results with `IsAuthorized = false` and skip the handler/query logic. Mapped Arc results use HTTP 403; ASP.NET middleware can challenge or forbid before Arc executes and may return a different response shape.

An ordinary call to `ServiceStatus.Internal()` bypasses the query pipeline and **does not evaluate attributes**. Calling a command's `Handle()` directly likewise bypasses authorization, validation, filters, and result handling. Use the [query pipeline](../queries/query-pipeline.md), [command pipeline](../commands/command-pipeline.md), or mapped HTTP endpoint when those guarantees matter.

## Custom authorization

A command filter denying permission returns `CommandResult.Unauthorized(context.CorrelationId)`; a query filter uses `QueryResult.Unauthorized(context.CorrelationId)`. `CommandResult.Error(...)` describes an exception result, not an authorization rejection. See [command filters](../commands/command-filters.md) for filter contracts.

Read the trusted principal through `ICurrentPrincipalAccessor` (`Cratis.Arc.Authorization`), or `IHttpContextAccessor.HttpContext.User` when intentionally writing ASP.NET-specific code. `IProvideIdentityDetails` composes frontend details; returning roles or `IsUserAuthorized` in its payload does not add claims or authorize every command/query. The [identity details](../identity/identity-provider-service.md) are presentation data and must not be used as authorization evidence.

## Verification checklist

Test anonymous, authenticated-without-role, and permitted-role callers through both HTTP and direct pipelines where the latter are supported. Where HTTP policies or schemes matter, test wrong-policy/wrong-scheme principals against the actual endpoint and verify scoped collaborators use the selected tenant. Include discovery endpoints with Production settings and a fallback policy enabled. Test custom ownership and tenant membership checks independently of UI visibility.

## See also

- [Core authorization](../core/authorization.md) — host-independent attribute and principal contracts.
- [Identity](../identity/index.md) — identity enrichment and its trust boundary.
- [Microsoft Identity](microsoft-identity.md) — authentication setup.
- [Tenancy](../tenancy/index.md) — selection is not membership authorization.
- [Proxy generation](../proxy-generation/index.md) — clients transport results; they do not enforce server permissions.
