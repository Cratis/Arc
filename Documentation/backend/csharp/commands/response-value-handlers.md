---
title: Response value handlers
description: Cratis Arc response value handlers process model-bound command results server-side while selecting a separate response value for the caller.
---

A command may need both a response for its caller and server-side processing of another value. Response value handlers separate those responsibilities without requiring event sourcing.

:::tip[Prefer command operations for application side effects]
Use [command operations](./operations/index.md) to declare inline service work with scoped dependencies and optional compensation. You do not need to implement the two response-handler interfaces or manage error handling yourself. Response value handlers remain the advanced extension point for framework integrations and specialized return-value interpretation; they are not deprecated.
:::

## Automatic response handling

Response value handlers apply to model-bound commands. Controller actions bypass this classification: Arc places the action's returned value directly in `CommandResult.Response`. Generated controller proxies therefore do not suppress values declared as server-handled by typed or attributed handler markers, or by command operation contracts.

After awaiting `Handle()`, the model-bound pipeline classifies its return value. `ICommandOperation` values and `CommandOperations` batches are reserved for the [operation phase](./operations/reference.md#execution-and-failure-ordering); ordinary response-value handlers do not execute them a second time. For other values:

1. A simple value matching `ICommandResponseValueHandler.CanHandle` is processed by every matching handler. Their `CommandResult` outcomes are merged.
2. A simple value with no handler becomes the response, including an ordinary `Guid`.
3. A `Result` or `OneOf` wrapper is unwrapped and its active value is processed. Nested wrappers are supported.
4. Tuple elements are classified individually. Exactly one unhandled value may become the response; all remaining non-null values need handlers. If all values are handled, there is no response.

Multiple unhandled tuple values cause `MultipleUnhandledTupleValues` internally. `ICommandPipeline.Execute` catches this and normally returns an exception-bearing result, rather than throwing that exception to the caller. A null return supplies no response.

The built-in handler consumes **one `Cratis.Arc.Validation.ValidationResult`**. It does not consume `ValidationResult[]` or `IEnumerable<ValidationResult>` returned by `Handle()`. On this ordinary response path, an unhandled collection becomes response data, or conflicts with another unhandled tuple value. The distinction applies only there: when the returned values include an `ICommandOperation` or `CommandOperations`, the pipeline classifies the values separately, treats a validation collection as a control value rather than a response candidate, and never returns it as response data. A declared union alone does not select that path; it applies only when the active runtime value contains an operation. Neither path makes `Handle()` collection validation a supported contract, so return a single `ValidationResult` (or a `Result` with a `ValidationResult` error) to report a failure. `Provide()` has a [different control-result contract](../../../scenarios/provide-data-to-a-command.mdx) that does recognize validation collections.

## Native C# union returns (.NET 11)

On .NET 11, `Handle()` can return a native C# union directly, through `Task<T>`, or through `ValueTask<T>`. For example, declare `public readonly union AllocationOutcome(Guid, AllocationLog, ValidationResult);` using the handled value defined below. Arc processes only the active case, just as it does for `Result` and `OneOf`. Native unions can also contain tuple alternatives or be nested inside the existing wrappers and tuples. Operations in the active case follow the existing command-operation rules; a bare operation collection still requires `CommandOperations`.

The Arc source generator registers declaration metadata and a typed active-value accessor for each nameable closed native union in a command's declared return graph. This includes inherited `Handle()` methods, nested unions, tuples, `Result`/`OneOf`, nullable and asynchronous wrappers, and collection element types. Private or generic commands returning a nameable closed union also receive metadata. Before executing `Handle()`, the operation boundary uses these declarations to detect operation alternatives, including inactive cases; active-value dispatch uses the registered accessor. This removes constructor reflection from the generated union path, not all trim or NativeAOT limitations in Arc.

Untrimmed JIT applications retain constructor reflection for unions that generated code cannot name or assemblies built without the generator. The `Cratis.Arc.Commands.UnionReflection.IsSupported` runtime switch controls this compatibility path; `CratisArcUnionReflectionSupport` sets it through MSBuild. The Arc.Core package forces it off for trimmed and NativeAOT publishes. Without generated declaration metadata in that mode, Arc throws `MissingCommandUnionMetadata` before `Handle()` runs rather than overlooking a possible operation. Use a nameable closed union in the declared return graph and include the Arc generator. Values returned through `object` or an opaque factory are outside that graph; declaring the union explicitly lets Arc inspect its alternatives before business execution.

A default union has no active non-null case. Arc rejects it before executing operations or response-value handlers, even when it occurs in a later tuple element. Do not use a default union to mean a successful command with nothing to record; return a `ValidationResult` rejection when a rule does not hold.

The JSON response is the active client-visible value, not a union wrapper, `Value` property, or discriminator. This support is for command **return values only**: it does not add union request deserialization, query unions, or `Provide()` unions. Object-case unions are not automatically round-trippable without a System.Text.Json classifier.

Generated TypeScript proxies retain the existing best-response selection, not an `A | B` contract. For model-bound commands they exclude server-handled cases, recursively inspect wrappers and tuples, prefer a concept, then a primitive, then the first remaining candidate. If no client candidate remains, the command has no typed response. Multiple client-visible cases therefore are not fully represented by the generated proxy. Controller outputs retain their existing classification without model-bound handler exclusions.

Analyzers enforcing CA1815 flag union declarations because the compiler-generated struct has no value equality; `readonly union` avoids IDE0250. Chronicle analyzer recognition of native union alternatives is separate from this runtime support.

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

## Declaring attributed values

For an integration that consumes model-bound return values identified by an attribute, declare the supported shapes alongside `ICommandResponseValueHandler`. Both markers require `TAttribute` to derive from `Attribute`:

- `ICommandResponseValueHandlerForAttributed<TAttribute>` excludes values whose own type carries the attribute. It does not imply collection handling.
- `ICommandResponseValueHandlerForAttributedEnumerable<TAttribute>` excludes enumerables compatible with `IEnumerable<object>` whose declared reference element type carries the attribute. It does not imply scalar handling. Rectangular arrays and value-type element collections remain responses.

Direct attributes always qualify. Attributes declared on base types qualify only when their effective `AttributeUsage.Inherited` permits inheritance (the default is `true`).

For example, the Chronicle single-event handler declares this marker in its class declaration (excerpt; the handler also implements the runtime `CanHandle` and `Handle` methods):

```csharp
public class SingleEventCommandResponseValueHandler(
    IEventLog eventLog,
    IEventTypes eventTypes,
    IConcurrencyScopeStrategies concurrencyScopeStrategies) :
    ICommandResponseValueHandler,
    ICommandResponseValueHandlerForAttributed<EventTypeAttribute>
```

These types come from `Cratis.Arc.Commands`, `Cratis.Chronicle.Events`, `Cratis.Chronicle.EventSequences`, and `Cratis.Chronicle.EventSequences.Concurrency`. Chronicle's multiple-event handler declares only `ICommandResponseValueHandlerForAttributedEnumerable<EventTypeAttribute>`. Neither handler declares `object` or `IEnumerable<object>` as universally consumed: build-time tooling cannot infer event elements from an object collection.

These markers describe supported static shapes; they do not register a runtime handler or change `CanHandle`. Keep declarations consistent with the values your integration actually consumes. Chronicle still needs registered event types and event-source routing, and its enumerable handler requires non-null registered elements. The static declaration cannot prove those value- and context-dependent conditions. As with the typed-value marker, tooling requires the genuine Arc.Core contracts and matches the attribute's full name and defining assembly identity; a same-named attribute in another assembly does not qualify.

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
