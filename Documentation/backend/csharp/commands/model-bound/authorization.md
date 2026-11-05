---
title: Authorize model-bound commands in Cratis Arc
description: Protect Cratis Arc model-bound commands with authentication, roles, or policies, and check authorization results before using a response.
---

Cratis Arc authorizes model-bound commands before ordinary command filters run. Put authorization attributes on the command type to require an authenticated caller or a role; a denied caller never reaches `Provide()` or `Handle()`.

Use an existing [Arc host](../../getting-started/index.md) with authentication configured to establish the caller's principal, the identity and claims used for access checks. Authorization attributes check that principal; they do not sign a caller in.

## Using the Authorize attribute

Apply `[Authorize]` from `Cratis.Arc.Authorization` to require authentication. This complete command returns a new identifier for an authenticated caller without persisting a business record:

```csharp
using System;
using Cratis.Arc.Commands.ModelBound;

[Command]
[Cratis.Arc.Authorization.Authorize]
public record AllocatePrivateIdentifier()
{
    public Guid Handle() => Guid.NewGuid();
}
```

Execute the command through `ICommandPipeline` or a mapped HTTP endpoint so Arc evaluates the attribute. Calling `Handle()` directly skips the pipeline and its authorization checks.

## Using the Roles attribute

Use `[Roles]` when authentication alone is not enough. This complete command allows an authenticated caller in either the `Admin` or `Manager` role:

```csharp
using System;
using Cratis.Arc.Commands.ModelBound;

public enum ApplicationRole
{
    Admin,
    Manager
}

[Command]
[Cratis.Arc.Authorization.Roles(nameof(ApplicationRole.Admin), nameof(ApplicationRole.Manager))]
public record AllocateManagedIdentifier()
{
    public Guid Handle() => Guid.NewGuid();
}
```

`[Cratis.Arc.Authorization.Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]` expresses the same requirement: a comma-separated list accepts any one of the roles. `nameof` emits the strings `Admin` and `Manager`, so match these names exactly to the roles supplied by your identity provider.

Separate authorization attributes all apply. If your application also defines an `ApplicationRole.Auditor` member, `[Roles(nameof(ApplicationRole.Admin))]` together with `[Authorize(Roles = nameof(ApplicationRole.Auditor))]` requires both roles.

## Requiring approval

Mark a command with `[RequiresApproval]` from `Cratis.Arc.Identity.Approvals` when it needs approval before its handler can execute:

```csharp
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Identity.Approvals;

[Command, RequiresApproval]
public record AllocateReviewedIdentifier
{
    public Guid Handle() => Guid.NewGuid();
}
```

The requirement is inherited by derived command types. Until an approval package registers an approval flow, execution and validation are refused with HTTP 403 (`IsAuthorized` is `false`); neither `Provide()` nor `Handle()` runs. Existing authorization and validation rules still apply before approval begins. Custom pipelines and controller actions do not support approval handoff and refuse marked commands even when a flow is registered.

## Authorization results

In an async caller with `ICommandPipeline pipeline` injected from `Cratis.Arc.Commands` and `System` imported, execute the managed command above:

```csharp
var result = await pipeline.Execute<Guid>(new AllocateManagedIdentifier());
if (!result.IsAuthorized)
{
    Console.WriteLine("Access denied");
}
```

An anonymous caller, or one without either required role, receives a denied result: `IsAuthorized` and `IsSuccess` are both `false`, and the handler has not executed. An authenticated `Admin` or `Manager` can reach the handler. Check `IsSuccess`, not `IsValid` alone, before using the response. [Validation severity](../validation-severity-filtering.md) does not override an unauthorized verdict.

## Anonymous access with AllowAnonymous

Use `[Cratis.Arc.Authorization.AllowAnonymous]` for an intentionally public command. It bypasses Arc's built-in attribute authorization check for that type. The built-in evaluator also allows access when no authorization requirement exists, so review which commands need protection rather than relying on the absence of an attribute.

Custom authorization filters and external middleware can still restrict access. An attribute on an unrelated controller does not establish a global requirement for model-bound command types. Public commands also need their own abuse controls.

## Policy-based authorization

Use a named policy for an access rule that goes beyond authentication and roles. Register a scoped `IAuthorizationPolicy` as shown in [Core named policies](../../core/authorization.md#register-a-named-policy), then apply `[Cratis.Arc.Authorization.Authorize(Policy = "ActiveSubscription")]` to the command. Arc awaits the policy before running `Provide()` or `Handle()`. Stacked policy attributes all apply; an unknown or duplicate policy fails host startup.

The policy receives the executing `CommandContext` through `AuthorizationPolicyContext.Resource` and the selected caller through `Principal`. Both contexts carry the same `ReceivedAt`, the time Arc received the operation. Delaying or repeating policy evaluation does not change it. See [command context](../command-context.md#properties) when a rule needs receipt time or validators need access to it.

### ASP.NET Core policies and authentication schemes

On an ASP.NET Core host, you can also use a named ASP.NET policy and `AuthenticationSchemes`, including schemes contributed by a policy. A scheme selects the authentication handler used to establish the caller. Arc authenticates the requested schemes rather than relying only on the default principal. Register every named scheme; Arc denies a request when none of the selected schemes authenticates it. The standalone Core host rejects `AuthenticationSchemes` at startup (see [ARC0021](../../code-analysis/index.md#arc0021-unevaluated-authorization-settings)).

When scheme selection changes the identity, Arc creates a fresh service scope so command values, execution-scope `Begin`, and handler dependencies bind to the selected tenant. Use mapped HTTP or the pipeline's scope-owning `Execute(command)` overload from a live request. An arbitrary explicit `IServiceProvider` is rejected for a scheme-selected command because it may already hold dependencies for the default identity.

Nested server-side commands remain supported when they use the same caller without selecting an authentication scheme. If a nested scheme changes identity after the outer command has started its unit of work, Arc rejects it before it can join that work.

With `Cratis.Arc`, you can also use `Microsoft.AspNetCore.Authorization.AuthorizeAttribute` and `AllowAnonymousAttribute` on a model-bound command. Arc enforces their authentication, roles, policies, and schemes. Prefer Arc's attributes when the command must also work on Arc Core's own HTTP host, which does not read the Microsoft attributes. Analyzer [ARC0020](../../code-analysis/index.md#arc0020-aspnet-core-authorization-attributes) reports a Microsoft attribute in a project without `Cratis.Arc`.

Microsoft authorization on MVC controllers or actions, and explicitly configured external middleware, enforce access separately from the model-bound pipeline.

### Domain-specific access rules

For access rules shared across commands, implement an [authorization command filter](../command-filters.md#cross-cutting-authorization-by-namespace) using `IAuthorizationCommandFilter` and return an unauthorized verdict when access is denied. The linked example checks authentication and roles. To protect a record by ownership, look up the record and compare its owner to the caller before making changes.

Test protection through every entry point your application exposes, not just its HTTP endpoint.

## Unbindable command bodies

A malformed body prevents Arc from creating a command instance. For execute and `/validate`, Arc still checks authentication, roles, schemes, and fallback requirements before reporting that input error.

Named ASP.NET policies also run with a null resource when every resolved requirement is resource-free, meaning it can be evaluated without a command instance. This includes the built-in authentication, role, claim, and name requirements, plus custom requirements implementing `Cratis.Arc.Authorization.IResourceFreeRequirement`. Use that marker only when the handler reads the user and never the resource. A command-property comparison must not opt in, and `AssertionRequirement` is not classified as resource-free.

If any policy contains a resource-dependent requirement, Arc skips the entire policy set on this path, not just that handler. Callers who meet the remaining authentication and role checks receive the binding failure as a Validation result. Native Core policies and `IAuthorizationCommandFilter` implementations also need an instance and are not evaluated here. Successfully bound commands follow normal policy and filter evaluation, including resource-dependent checks. See [command input binding](../input-binding.md) to handle messages, member paths, and the explicit-offset marker.

## Executing commands from server-side code

A background job has no HTTP caller, so it needs an explicitly trusted server-side actor to execute a protected command. Arc's `ICurrentPrincipalAccessor` uses the HTTP request principal when a request exists. Outside a request, it uses a principal established through `ISystemExecution`, or null if none exists.

This caller reuses `AllocateManagedIdentifier` and `ApplicationRole` above. Inject `ISystemExecution` and `ICommandPipeline` from Arc's dependency injection container:

```csharp
using System;
using System.Threading.Tasks;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;

public class ManagedIdentifierJob(ISystemExecution systemExecution, ICommandPipeline pipeline)
{
    public async Task<CommandResult<Guid>> Run()
    {
        using (systemExecution.AsSystem(nameof(ApplicationRole.Manager)))
        {
            return await pipeline.Execute<Guid>(new AllocateManagedIdentifier());
        }
    }
}
```

`AsSystem(params string[] roles)` creates an authenticated system actor carrying exactly the supplied roles. Calling it with no roles satisfies authentication alone, not a role requirement. Use `As(ClaimsPrincipal)` when you already have an explicitly supplied principal. Await execution inside the scope and dispose it to restore the previous actor, as the `using` block does above.

Keep system roles under trusted server-side control; never derive them from request input. An HTTP request principal remains authoritative, so a server-side scope cannot elevate that request.

If a Chronicle reactor needs to execute a command, continue with [reactor command side effects](../../chronicle/reactors/command-side-effects.md). See [Chronicle command integration](../../chronicle/commands/index.md) only when you want the command to participate in an event-sourced workflow.
