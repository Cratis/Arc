---
title: Response value handlers
description: How standalone Arc classifies scalar, tuple, and discriminated-union command results.
---

A command may need both a response for its caller and server-side processing of another value. Response value handlers separate those responsibilities without requiring event sourcing.

:::tip[Prefer command operations for application side effects]
Use [command operations](./operations/index.md) to declare inline service work with scoped dependencies and optional compensation. You do not need to implement the two response-handler interfaces or manage error handling yourself. Response value handlers remain the advanced extension point for framework integrations and specialized return-value interpretation; they are not deprecated.
:::

## Automatic response handling

After awaiting `Handle()`, the model-bound pipeline classifies its return value. `ICommandOperation` values and `CommandOperations` batches are reserved for the [operation phase](./operations/reference.md#execution-and-failure-ordering); ordinary response-value handlers do not execute them a second time. For other values:

1. A simple value matching `ICommandResponseValueHandler.CanHandle` is processed by every matching handler. Their `CommandResult` outcomes are merged.
2. A simple value with no handler becomes the response, including an ordinary `Guid`.
3. A `Result` or `OneOf` wrapper is unwrapped and its active value is processed. Nested wrappers are supported.
4. Tuple elements are classified individually. Exactly one unhandled value may become the response; all remaining non-null values need handlers. If all values are handled, there is no response.

Multiple unhandled tuple values cause `MultipleUnhandledTupleValues` internally. `ICommandPipeline.Execute` catches this and normally returns an exception-bearing result, rather than throwing that exception to the caller. A null return supplies no response.

The built-in handler consumes **one `Cratis.Arc.Validation.ValidationResult`**. It does not consume `ValidationResult[]` or `IEnumerable<ValidationResult>` returned by `Handle()`. An unhandled collection becomes ordinary response data, or conflicts with another unhandled tuple value. `Provide()` has a [different control-result contract](../../scenarios/provide-data-to-a-command.md) that does recognize validation collections.

## Result with tuple alternatives

A `Result` can use a tuple as an alternative. In `Result<(Guid, AllocationLog), ValidationResult>`, the active success alternative supplies both the caller's `Guid` response and an `AllocationLog` for server-side handling. If the active alternative is instead a `ValidationResult` error, Arc's built-in handler records a validation failure; the success tuple is not processed.

The [complete example below](#creating-custom-value-handlers) defines the command and its custom handler. See [caller examples](./response-examples.md#result-and-tuple-response) for consuming the result.

## Creating custom value handlers

Here is a complete standalone set of application types. The command allocates an identifier; the custom handled value writes a diagnostic message. This sample does **not** create a durable business record, and diagnostic logging is not a durable audit trail.

```csharp
using System;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

public record AllocationLog(string Label);

public class AllocationLogHandler(ILogger<AllocationLogHandler> logger) :
    ICommandResponseValueHandler,
    ICommandResponseValueHandler<AllocationLog>
{
    static readonly Action<ILogger, string, Exception?> _allocated = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(1, "IdentifierAllocated"), "Allocated identifier for {Label}");

    public bool CanHandle(CommandContext context, object value) => value is AllocationLog;

    public Task<CommandResult> Handle(CommandContext context, object value)
    {
        var entry = (AllocationLog)value;
        _allocated(logger, entry.Label, null);
        return Task.FromResult(CommandResult.Success(context.CorrelationId));
    }
}

[Command]
public record AllocateIdentifier(string Label)
{
    public Result<(Guid, AllocationLog), ValidationResult> Handle()
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            return ValidationResult.Error("A label is required", [nameof(Label)]);
        }
        return (Guid.NewGuid(), new AllocationLog(Label));
    }
}
```

Prerequisites in the host:

- Configure Arc's normal application type discovery and DI. Place the command and handler in a discovered application assembly referencing `Cratis.Arc.Core`.
- Enable .NET logging and configure a logging provider to observe the message. `ILogger<AllocationLogHandler>` must resolve.
- Implement **both** handler interfaces as above. The non-generic interface is the runtime contract; the generic marker tells build-time tools that `AllocationLog` is handled server-side and is not the client response.
- Do not install a handler for `Guid` if you want it to remain the caller's response.

Execution fragment in a caller with an injected `ICommandPipeline`:

```csharp
var result = await pipeline.Execute<Guid>(new AllocateIdentifier("invoice"));
```

On success, the log handler runs and `result.Response` contains the allocated `Guid`. On an empty label, Arc's singular validation handler adds the rejection; `result.IsValid` is false and the typed response is `default(Guid)`.

Implementing only `ICommandResponseValueHandler<TValue>` does not supply a runtime handler. For dynamically accepted types, implement the runtime interface without an overly broad marker such as `object`, which would misdescribe the client contract. Typed declarations are discovered in referenced assemblies that reference `Cratis.Arc.Core`; the interfaces must have the actual Arc assembly identity, not just matching names.

## Response object availability

`CommandContext.Response` is phase-dependent, not a copy of the raw handler return:

| Phase | What is available |
| --- | --- |
| Filters, scope `Begin`, and before handler execution | No response yet. |
| Simple-value `CanHandle` check | The candidate has not yet been assigned as a response. |
| Tuple classification | Earlier elements may already have supplied a response; do not assume it exists. `CanHandle` may run repeatedly. |
| Tuple value handler `Handle` | The single unhandled response has been selected, if any. |
| Scope `Complete` after successful response processing | The selected response, including an unhandled simple value. |

Keep `CanHandle` side-effect free and prefer type checks that do not depend on a response being present. Use the context passed to the callback; do not assume a previously captured ambient context has been replaced with the later record instance.

After all execution scopes complete, the pipeline clears the **result's** response when execution is unsuccessful. A response observed inside a scope is not proof of final success; always check the returned `CommandResult.IsSuccess` before acting on it.

## Side effects and failures

A custom handler performs real work after `Handle()` returns. Arc does not automatically compensate arbitrary work performed by these custom handlers or earlier direct service writes. For per-command application side effects, prefer [operations with optional compensation](./operations/implementing.md). Their recovery contract covers declared, started operations—not every write a custom handler or service might perform.

For event-sourced applications only, [Chronicle transactional commands](./transactional-commands.md) describes the optional integration's commit behavior. Continue with [response examples](./response-examples.md) for success, validation, and failure cases.
