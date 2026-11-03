---
title: Command context
description: Access invocation metadata and understand when dependencies and responses become available.
---

`CommandContext` carries invocation metadata for pipeline extensions. Its command input stays the same, but resolved dependencies and the selected response become available at later phases. Use this reference to decide which context to read in a filter, handler, or lifecycle callback; a context retained from an earlier phase is not a live view of every later record.

## Properties

| Property | Meaning |
| --- | --- |
| `CorrelationId` | The invocation's correlation identifier. |
| `Type`, `Command` | The command type and instance. |
| `Dependencies` | Resolved handler arguments; empty during filters and scope initialization. |
| `GeneratedValues` | Validated values captured while resolving generated handler parameters; see [validation and captures](./generated-values.md#validation-and-captures). |
| `Values` | A case-insensitive `CommandContextValues` dictionary. |
| `AllowedSeverity` | Optional threshold for the stages described in [severity filtering](./validation-severity-filtering.md). |
| `Response` | Selected caller response, if any; available after response processing, not the raw handler return or proof of final success. |
| `ServiceProvider` | The provider for this invocation's service scope, when supplied. |
| `CancellationToken` | The execution cancellation token. |
| `ReceivedAt` | `DateTimeOffset` captured once when Arc receives this operation. |
| `SliceLocation` | The module, feature and slice of the command, resolved like its proxy's output folder. See [slice location](../proxy-generation/Configuration/slice-location.md). |

`ReceivedAt` means Arc receipt time, not network arrival or a timestamp before application middleware. Model-bound HTTP dispatch captures it before body/argument binding and authorization preparation. Direct pipeline calls capture it on entry. A hub captures a value per subscribe operation, not per connection; MVC establishes command context in an action filter after binding.

The captured time stays fixed through authorization, validation, handler execution, and any Arc-owned replacement service scope. Sibling public pipeline calls within a decorated transport dispatch, such as `Validate` followed by `Execute` or a query followed by a command, share the transport receipt. A call started inside another pipeline entry captures a new receipt and restores the outer value afterward.

Receipt sharing ends with the dispatch. Work that starts a pipeline after dispatch finishes gets a fresh receipt, even if it captured its execution context during that dispatch.

Context values are application/extension data, not automatically trusted identity claims. Use `ICurrentPrincipalAccessor` for the [authorization principal](./model-bound/authorization.md).

## Command context values

Implement `ICommandContextValuesProvider.Provide(object command)` to contribute extension metadata. The builder merges providers in discovery order, and a later value overwrites an earlier value with the same case-insensitive key. Namespace keys you own so a tracing provider cannot accidentally replace another integration's data.

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

Normal Arc type discovery includes this provider automatically. Its tracing values are built during execution **and validation**, before command authorization filters. Do not perform a write or expose sensitive data on the assumption that a filter has already allowed the command. Pre-flight invokes the provider too, so keep it lightweight and safe to repeat.

Arc may also populate reserved context values such as a resolved command key. Do not overwrite values owned by the framework or another integration.

## Accessing command context

Prefer the context argument supplied to a filter, response handler, or execution scope. For a component with no such argument, inject `ICommandContextAccessor` to read the established ambient context. Access outside an established context throws; it is not an optional lookup. This command reads the tracing value from the provider above and returns ordinary response data:

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

At scope `Begin` and during command filters, `Dependencies` is empty and `Response` is null: argument resolution has not happened yet. After filters, Arc resolves `Provide()` values and handler arguments, then uses an updated context for handler invocation. Response processing can create a further record containing the selected caller response.

Read phase-specific dependencies and response from the context **passed to the lifecycle callback**. The ambient accessor was established earlier and is not automatically replaced for each later record copy. `Values` is shared and mutable across those copies; replacing a record and mutating that dictionary are different operations. The ambient context accessor itself does not provide a stack that restores an earlier command context for you.

A validator resolved from the operation's service provider can inject `IOperationContextAccessor` (`Cratis.Arc`) and read its nullable `ReceivedAt`. It is null outside an Arc operation and in MVC-bound validators that run during model binding, before the command action filter starts the receipt. To test time-sensitive rules, register your own `TimeProvider` before or after `AddCratisArcCore()`; Arc uses `TryAddSingleton<TimeProvider>` for `TimeProvider.System`, so an app registration takes precedence regardless of order. Do not call `DateTimeOffset.UtcNow` to reconstruct a receipt timestamp in a validator.

At scope `Complete`, both a successfully processed simple response and a tuple-selected response are available. A later completion failure can still make execution unsuccessful. Do not commit unrelated work merely because this callback sees a response; consult [response object availability](./response-value-handlers.md#response-object-availability) for the extension contract.

MVC action filters establish some ambient command context separately, but controller actions do not pass through this model-bound lifecycle. Continue to [execution scopes](./command-execution-scopes.md) or [command filters](./command-filters.md) for extension examples.
