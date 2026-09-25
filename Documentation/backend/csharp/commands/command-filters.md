---
title: Command filters
description: Intercept model-bound commands, with authorization evaluated before ordinary filters.
---

Use a command filter when a rule applies before multiple commands run. Filters belong to the **model-bound `ICommandPipeline`**, not the MVC action pipeline. For controller-based commands, use ASP.NET Core filters and authorization.

## How it works

Arc discovers `ICommandFilter` implementations and resolves them from the command's service scope. `OnExecution(CommandContext)` runs before `Provide()` and `Handle()`.

1. Filters implementing **`IAuthorizationCommandFilter`** run first.
2. Ordinary `ICommandFilter` implementations run afterward.
3. Within each group, filters retain discovery order; do not use that as a configurable priority mechanism.
4. Each result is merged. The chain stops at the first unsuccessful result; a throwing filter contributes a failure rather than discarding earlier verdicts.

The pipeline applies [severity filtering](./validation-severity-filtering.md) after this chain. That page describes the current limitation when a warning stops the chain but is later removed.

## Implementing a custom command filter

This complete filter logs the command type without logging its possibly sensitive payload. It requires normal Arc discovery and a resolvable `ILogger<CommandLoggingFilter>` with a configured provider if you want to observe messages.

```csharp
using System;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Microsoft.Extensions.Logging;

public class CommandLoggingFilter(ILogger<CommandLoggingFilter> logger) : ICommandFilter
{
    static readonly Action<ILogger, string, Exception?> _checking = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(1, "CheckingCommand"), "Checking command {CommandType}");

    public Task<CommandResult> OnExecution(CommandContext context)
    {
        _checking(logger, context.Type.FullName ?? context.Type.Name, null);
        return Task.FromResult(CommandResult.Success(context.CorrelationId));
    }
}
```

This logs a pre-execution check, not successful completion. It also runs during pre-flight validation. For final outcomes use [execution scopes](./command-execution-scopes.md).

## Cross-cutting authorization by namespace

For access control, implement **`IAuthorizationCommandFilter`**, not just `ICommandFilter`. Return `CommandResult.Unauthorized`, not a severity-overridable validation error.

This complete filter requires an authenticated principal with the `Payments` role for commands in exactly `MyApp.Payments` or one of its child namespaces. It uses Arc's transport-independent principal accessor, which also supports trusted server-side execution scopes.

```csharp
using System;
using System.Threading.Tasks;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;

public class PaymentsAuthorizationFilter(ICurrentPrincipalAccessor principals) : IAuthorizationCommandFilter
{
    const string ProtectedNamespace = "MyApp.Payments";

    public Task<CommandResult> OnExecution(CommandContext context)
    {
        var commandNamespace = context.Type.Namespace;
        var protectedCommand = commandNamespace == ProtectedNamespace ||
            (commandNamespace?.StartsWith(ProtectedNamespace + ".", StringComparison.Ordinal) ?? false);
        if (!protectedCommand)
        {
            return Task.FromResult(CommandResult.Success(context.CorrelationId));
        }

        var principal = principals.Current;
        var authorized = principal?.Identity?.IsAuthenticated == true && principal.IsInRole("Payments");
        return Task.FromResult(authorized
            ? CommandResult.Success(context.CorrelationId)
            : CommandResult.Unauthorized(context.CorrelationId, "Payments access is required."));
    }
}
```

Keep this class in a discovered assembly and configure authentication to establish the trusted principal. The namespace boundary check deliberately excludes names such as `MyApp.PaymentsPublic`. Renaming a protected command outside the namespace changes its protection, so test your command inventory as well as allowed and denied principals. This role gate does not implement record ownership; add an actual ownership decision if your operation needs one.

## Built-in filters

| Filter in `Cratis.Arc.Commands.Filters` | Behavior |
| --- | --- |
| `AuthorizationFilter` | Implements `IAuthorizationCommandFilter`; evaluates Arc authentication/role requirements and returns an unauthorized verdict when denied. |
| `DataAnnotationValidationFilter` | Validates command properties. Positional records require targets such as `[property: Required]`. |
| `FluentValidationFilter` | Validates the command graph with discovered validators. |

Use [command validation](./validation.md) for complete annotation and validator examples. For the exact supported attribute namespaces and the current policy/scheme limitation, read [model-bound authorization](./model-bound/authorization.md).

## Context availability

At filter time `CommandContext.Command`, `Type`, `CorrelationId`, `Values`, `ServiceProvider`, and `CancellationToken` describe the current invocation. `Dependencies` is still empty and `Response` has not been selected. Resolve any required collaborator through constructor injection rather than expecting handler arguments to be available already.

Keep filters focused, avoid exposing sensitive details in rejection messages, and test them through both execution and pre-flight validation. See [command context](./command-context.md) for later phases.
