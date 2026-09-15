---
title: Model-bound command authorization
description: Protect standalone Arc commands with authentication, roles, and authorization filters.
---

A valid command is not necessarily an allowed command. Arc evaluates authorization before ordinary command filters and does not invoke `Provide()` or `Handle()` when authorization is denied.

## Using the Authorize attribute

Use the attributes in **`Cratis.Arc.Authorization`** for model-bound commands. This complete example requires authentication and returns an identifier without persisting a business record:

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

Configure authentication in your host to establish the request principal. The attribute checks that principal; it does not authenticate a request by itself.

## Using the Roles attribute

This complete command requires an authenticated principal in at least one of the listed roles:

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

`[Cratis.Arc.Authorization.Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]` expresses the same role requirement. These application-owned names still emit the role strings `Admin` and `Manager`; preserve exact names when roles come from an external identity provider. Use a single attribute with a comma-separated list rather than assuming several authorization attributes compose multiple requirements; the current attribute evaluator takes the first matching attribute.

## Anonymous access with AllowAnonymous

`[Cratis.Arc.Authorization.AllowAnonymous]` bypasses Arc's built-in attribute authorization check for that type. With no authorization requirement, the built-in evaluator also allows access. Mark intentionally public commands explicitly, and review their abuse controls separately.

This does not promise to bypass custom authorization filters or external middleware. Likewise, adding an attribute to an unrelated controller does not establish a global authorization requirement for model-bound command types.

## Policy-based authorization

> [!WARNING]
> Current model-bound Arc evaluators check authentication and roles only. Although Arc's `AuthorizeAttribute` exposes `Policy` and `AuthenticationSchemes` properties, those properties are not evaluated here. Do not rely on them to protect a command.

Do not substitute `Microsoft.AspNetCore.Authorization.AuthorizeAttribute` on a model-bound command expecting equivalent protection. The current Arc evaluators do not establish that contract; even the ASP.NET-named evaluator currently resolves the Arc attribute type. Standard Microsoft authorization on **MVC controllers/actions**, or explicitly configured external middleware, is a separate enforcement path.

For domain-specific access control in the model-bound pipeline, implement an [authorization command filter](../command-filters.md#cross-cutting-authorization-by-namespace) using `IAuthorizationCommandFilter` and an actual unauthorized verdict. That example uses real authentication and role checks, not a placeholder policy or an overridable validator. Record ownership needs its own real resource lookup and owner comparison before changes occur.

Test protection through every entry point your application exposes.

## Authorization results

Caller fragment with `ICommandPipeline pipeline` injected:

```csharp
var result = await pipeline.Execute<Guid>(new AllocateManagedIdentifier());
if (!result.IsAuthorized)
{
    Console.WriteLine("Access denied");
}
```

A denied result has `IsSuccess == false` and the handler has not executed. Check `IsSuccess`, not `IsValid` alone, before using a response. [Validation severity](../validation-severity-filtering.md) does not override an unauthorized verdict.

## Executing commands from server-side code

Arc's `ICurrentPrincipalAccessor` uses the HTTP request principal when a request exists. Otherwise it uses a principal established through `ISystemExecution`, or null if none exists. A protected background command therefore needs an explicitly trusted server-side actor.

This complete caller reuses `AllocateManagedIdentifier` above and requires `ISystemExecution` and `ICommandPipeline` from Arc DI:

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

`AsSystem(params string[] roles)` creates an authenticated system actor carrying exactly those roles. No roles satisfies authentication alone, not a role requirement. `As(ClaimsPrincipal)` supports an explicitly supplied principal. Dispose the scope to restore the previous actor, and await execution inside it.

Never derive the system roles from request input. An HTTP request principal remains authoritative; a server-side scope cannot elevate that request. For the separate, optional event-sourced use case, see [Chronicle reactor command side effects](../../chronicle/reactors/command-side-effects.md) and [Chronicle command integration](../../chronicle/commands/index.md).
