---
title: Command context
description: Access invocation metadata and understand when dependencies and responses become available.
---

When several pipeline extensions need the same invocation metadata, carry it in `CommandContext` rather than adding transport details to the command's input. The context is a record whose contents depend on the execution phase.

## Properties

| Property | Meaning |
| --- | --- |
| `CorrelationId` | The invocation's correlation identifier. |
| `Type`, `Command` | The command type and instance. |
| `Dependencies` | Resolved handler arguments, initially empty. |
| `Values` | A case-insensitive `CommandContextValues` dictionary. |
| `AllowedSeverity` | Optional threshold for the stages described in [severity filtering](./validation-severity-filtering.md). |
| `Response` | Selected caller response, if any; not the raw handler return. |
| `ServiceProvider` | The provider for this invocation's service scope, when supplied. |
| `CancellationToken` | The execution cancellation token. |

Context values are application/extension data, not automatically trusted identity claims. Use `ICurrentPrincipalAccessor` for the [authorization principal](./model-bound/authorization.md).

## Command context values

Implement `ICommandContextValuesProvider.Provide(object command)` to contribute values. Arc's builder merges providers in discovery order; a later value with the same case-insensitive key overwrites the earlier one. Use namespaced keys you own.

This complete provider adds tracing metadata without reading request-supplied identity:

```csharp
using System;
using System.Diagnostics;
using Cratis.Arc.Commands;

public class TraceContextValuesProvider : ICommandContextValuesProvider
{
    public CommandContextValues Provide(object command) => new()
    {
        ["Example.ObservedAt"] = DateTimeOffset.UtcNow,
        ["Example.CommandType"] = command.GetType().FullName ?? command.GetType().Name,
        ["Example.TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty
    };
}
```

Under normal Arc type discovery, the provider is included automatically. It runs during context construction for execution **and validation**, before command authorization filters. Keep it lightweight and avoid sensitive output or state changes; pre-flight validation cannot make a custom provider pure.

Arc may also populate reserved context values such as a resolved command key. Do not overwrite values owned by the framework or another integration.

## Accessing command context

Filters, response handlers, and execution scopes receive a context argument directly. Components that do not receive one can inject `ICommandContextAccessor` for the established ambient context. This complete command uses a provided tracing value and returns it as ordinary response data:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

[Command]
public record ReadInvocationTrace()
{
    public string Handle(ICommandContextAccessor accessor) =>
        accessor.Current.Values.TryGetValue("Example.TraceId", out var traceId)
            ? traceId.ToString() ?? string.Empty
            : string.Empty;
}
```

The empty response means no trace identifier was supplied. This is a metadata demonstration, not a permission check or a business-state query.

## Phase availability

During scope `Begin` and command filters, `Dependencies` is empty and `Response` is null. After filters, Arc resolves `Provide()` values and handler arguments and uses an updated context for handler invocation. During response processing it may create another context record with a selected response.

Use the context **passed to a lifecycle callback** for its phase's dependencies and response. The ambient accessor is established earlier; do not assume a retained accessor value is replaced whenever the pipeline creates a later record copy. `Values` is shared by those copies and remains mutable.

At scope `Complete`, a successfully processed simple response is available just as a tuple-selected response is. The result can still fail during scope completion. See [response object availability](./response-value-handlers.md#response-object-availability) before using a response inside an extension.

MVC action filters establish some ambient command context separately, but controller actions do not pass through this model-bound lifecycle. Continue to [execution scopes](./command-execution-scopes.md) or [command filters](./command-filters.md) for extension examples.
