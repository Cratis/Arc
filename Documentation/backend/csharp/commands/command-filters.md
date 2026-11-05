---
title: Cratis Arc command filters for shared checks
description: Command filters apply shared checks before Cratis Arc model-bound handlers run. Authorization filters run first; blocking failures stop execution.
---

A command filter applies a shared check before a Cratis Arc model-bound command reaches `Provide()` or `Handle()`. Use one for rules that belong across commands rather than inside a single handler. Filters run through `ICommandPipeline`; controller-based commands use ASP.NET Core MVC filters and authorization instead.

## How it works

Arc discovers `ICommandFilter` implementations and resolves them from the command's service scope. `OnExecution(CommandContext)` runs before `Provide()` and `Handle()`.

Filters implementing `IAuthorizationCommandFilter` run first, followed by ordinary `ICommandFilter` implementations. Each group retains discovery order, but that order is not a configurable priority mechanism.

Arc merges each filter's result into the command result. Authorization denials, exceptions, and validation failures that block under the effective severity policy stop the chain. Permitted validation failures do not prevent later filters from running. If a filter throws, its failure is merged without discarding earlier verdicts.

The pipeline then removes permitted validation failures from the result. Use [severity filtering](./validation-severity-filtering.md) to choose caller thresholds and declare failures that must remain blocking.

## Implementing a custom command filter

The filter below logs the command type without logging its possibly sensitive payload. Keep it in an assembly Arc discovers, and configure a logging provider with a resolvable `ILogger<CommandLoggingFilter>` to see its messages.

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

For access control, implement `IAuthorizationCommandFilter`, not just `ICommandFilter`. Return `CommandResult.Unauthorized` so a permissive validation threshold cannot turn a denial into permission.

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

Use [command validation](./validation.md) for complete annotation and validator examples. For supported authorization declarations and their attribute namespaces, read [model-bound authorization](./model-bound/authorization.md).

## Context availability

At filter time `CommandContext.Command`, `Type`, `CorrelationId`, `Values`, `ServiceProvider`, and `CancellationToken` describe the current invocation. `Dependencies` is still empty and `Response` has not been selected. Resolve any required collaborator through constructor injection rather than expecting handler arguments to be available already.

Exercise the filter through both execution and pre-flight validation, and review rejection messages for sensitive details. If your logic needs handler dependencies or the selected response, [command context](./command-context.md) explains when those values become available.
