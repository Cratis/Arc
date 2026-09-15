---
title: Command pipeline
description: Execute and await standalone Arc commands from application code.
---

When a background job or application service needs the same command behavior as an HTTP caller, inject `ICommandPipeline`. It runs the model-bound authorization, validation, provisioning, handler, response handlers, command operations, and execution scopes without an HTTP round trip.

:::tip[Declare work instead of writing a recovery stack]
For immediate side effects chosen by a command, return [command operations](./operations/index.md). The pipeline awaits their execution and eligible compensation. Backend code and [CommandScenario](../testing/command-operations.md) can inspect recovery observations without moving recovery handling to the frontend.
:::

## Basic usage

These complete caller types use `AddItemToCart`, `ICartService`, and the shared cart concepts from the [service-backed example](./model-bound/index.md#a-service-backed-command). Configure Arc and the example's service registration before resolving them.

### Without a service provider

```csharp
using System.Threading.Tasks;
using Cratis.Arc.Commands;

public class CartApplicationService(ICommandPipeline pipeline)
{
    public Task<CommandResult<CartLineId>> Add(Sku sku, Quantity quantity) =>
        pipeline.Execute<CartLineId>(new AddItemToCart(sku, quantity));
}
```

Each scope-free call creates and disposes its own DI scope. The returned task represents completed execution, including response handlers and scope completion. The caller must await it before using the result; returning the task from an asynchronous API, as above, preserves that contract.

### With a service provider

```csharp
using System;
using System.Threading.Tasks;
using Cratis.Arc.Commands;

public class ScopedCartApplicationService(ICommandPipeline pipeline, IServiceProvider services)
{
    public Task<CommandResult<CartLineId>> Add(Sku sku, Quantity quantity) =>
        pipeline.Execute<CartLineId>(new AddItemToCart(sku, quantity), services);
}
```

Resolve this caller inside the existing scope you intend to share. Passing the root provider does not create a command scope. Scope-explicit calls use the supplied provider and leave its lifetime to the caller.

## Cancellation

HTTP command endpoints pass the request-aborted token. `Provide()` and `Handle()` may accept a `CancellationToken`, which Arc supplies directly.

The following complete command waits asynchronously; it is a timing demonstration, not a background-work scheduler:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands.ModelBound;

[Command]
public record WaitForInterval(int Milliseconds)
{
    public Task Handle(CancellationToken cancellationToken) =>
        Task.Delay(Milliseconds, cancellationToken);
}
```

Caller fragments, with `Cratis.Arc.Commands` and `Cratis.Arc.Validation` imported:

```csharp
var result = await pipeline.Execute(new WaitForInterval(100), cancellationToken);
```

```csharp
var result = await pipeline.Execute(
    new WaitForInterval(100),
    serviceProvider,
    allowedSeverity: ValidationResultSeverity.Warning,
    cancellationToken: cancellationToken);
```

Use these cancellation extension overloads with Arc's cancellation-aware pipeline. A custom implementation of only `ICommandPipeline`, rather than `ICommandPipelineWithCancellation`, receives the compatibility call without a token. Awaiting the delayed command waits for its task; cancellation is handled through the pipeline's result/error path, so inspect the result rather than assuming every cancellation throws to your caller.

## Command results

| Property | Meaning |
| --- | --- |
| `IsSuccess` | Authorized, no remaining validation results, and no exceptions. |
| `IsAuthorized` | The authorization verdict. |
| `IsValid` | Whether `ValidationResults` is empty; this alone does not establish success. |
| `HasExceptions` | Whether exception messages are present. |
| `ValidationResults` | Individual Arc validation failures remaining after applicable filtering. |
| `ExceptionMessages`, `ExceptionStackTrace` | Exception details; handle as potentially sensitive diagnostic information. |
| `AuthorizationFailureReason` | A supplied reason for denial, when present. |
| `CorrelationId` | Identifier for correlating this execution. |
| `Recovery` | Optional backend-only command operation recovery summary; excluded from HTTP JSON. |
| `OperationOutcomes` | Backend-only observations of started operation invocations; excluded from HTTP JSON. |

## Typed command results

Caller fragment using the cart example:

```csharp
var result = await pipeline.Execute<CartLineId>(new AddItemToCart("BOOK-1", 2));
if (result.IsSuccess)
{
    var lineId = result.Response;
    Console.WriteLine(lineId);
}
```

`Execute<TResult>` returns `CommandResult<TResult>` with a `Response` property. Request the response type, not the raw tuple or `Result` wrapper: [response processing](./response-value-handlers.md) determines the value exposed to callers. A response assignable to `TResult`, including an interface or base type, is supported. A genuinely incompatible requested type causes an `InvalidCastException` from the typed overload.

Without a response, the typed overload supplies **`default(TResult)`**: null for a reference type, `Guid.Empty` for `Guid`, and zero for `int`. A `void` or `Task` handler has no response, but still runs to completion before the result is returned. Use non-generic `Execute` when you do not need a typed value.

Scope completion can fail after the handler produced a value. After scopes complete, failed execution clears an already selected response to the default of its **concrete response type**. When requesting that same type, this yields `default(TResult)`, including `Guid.Empty`. Currently, adapting a cleared value-type response to `object` or a compatible interface can preserve its boxed default instead of null. For example, `Execute<object>` can return a boxed `Guid.Empty` after scope completion fails. Always check `IsSuccess` before using the response; neither null nor a default-valued response is itself an authorization or success verdict.

## Exception handling

The pipeline catches handler, filter, response-handler, and scope-completion exceptions and represents failures in `CommandResult`. Exceptions implementing `IValidationFailure` can be translated into validation outcomes; ordinary exceptions produce exception details. A wrong requested generic response type is a separate caller error, described above.

Arc does not undo arbitrary direct application-service writes on failure. Prefer [command operations](./operations/index.md) for inline work you want the framework to execute and, when safe, compensate. Their commit-aware recovery does not imply atomic rollback, automatic retries, or crash recovery. Specialized persistence coordination still belongs in a deliberately implemented [execution scope](./command-execution-scopes.md). The [Chronicle transaction integration](./transactional-commands.md) is optional and has its own boundaries.

## Validation without execution

Caller fragment:

```csharp
var command = new AddItemToCart("BOOK-1", 2);
var check = await pipeline.Validate(command);
if (check.IsSuccess)
{
    var execution = await pipeline.Execute<CartLineId>(command);
    Console.WriteLine(execution.IsSuccess);
}
```

`Validate` also has a scope-explicit overload and [severity thresholds](./validation-severity-filtering.md). It skips `Provide()`, `Handle()`, response processing, and execution scopes, but still runs context providers and command filters. It is not a guarantee that collaborators are side-effect free or that the later execution will succeed. See [pre-flight validation](./command-validation.md).

## Context and authentication

The current correlation context is used when available. Authorization uses the HTTP request principal when a request exists; otherwise it uses an explicitly established server-side principal. For trusted background execution, see [server-side authorization scopes](./model-bound/authorization.md#executing-commands-from-server-side-code). Do not assume a background command is automatically authorized or that a fresh DI scope creates a tenant or user identity.
