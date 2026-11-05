---
title: Cratis Arc command pipeline from application code
description: Execute Cratis Arc model-bound commands from application services or background jobs, choose their DI scope, and check the final result before using a response.
---

Use Cratis Arc's `ICommandPipeline` to run a model-bound command from a background job or application service without an HTTP round trip. It applies the command's authorization and validation rules and awaits execution, so the caller can check the final result before using a response.

Choose whether Arc should create a dependency injection (DI) scope or reuse one you own. The pipeline runs `Provide()` to obtain handler dependencies, invokes the handler, processes responses and declared operations, and completes execution scopes. Calling `Handle()` directly bypasses that work.

:::tip[Declare work instead of writing a recovery stack]
For immediate side effects chosen by a command, return [command operations](./operations/index.md). The pipeline awaits their execution and eligible compensation. Backend code and [CommandScenario](../testing/command-operations.md) can inspect recovery observations without moving recovery handling to the frontend.
:::

Direct calls supply a command instance, so HTTP body binding is not part of this procedure. Mapped endpoints have a separate refusal path for an unbound body: they check resource-free authorization before returning binding validation, without creating command context or execution scopes. That refusal still emits command/validation spans. See [input binding](./input-binding.md#authorization-before-reporting-input-errors) for the named-policy limits.

## Basic usage

These caller types use `AddItemToCart`, `ICartService`, and the shared cart concepts from the [service-backed example](./model-bound/index.md#a-service-backed-command). Configure Arc and register that example's service before resolving a caller. Use the first form unless you have an existing command scope to share.

Before using a response, [check the final command result](#command-results).

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

This scope-free call creates and disposes its own DI scope. Its task completes after response handlers and execution-scope completion, so awaiting it includes those stages. Returning the task from the caller above lets the next caller await the same work. Do not discard the task or read a response before completion.

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

Resolve this caller inside the scope you intend to share. The pipeline uses the supplied provider and leaves disposal to you. Passing the root provider does not create a command scope and can give scoped dependencies the wrong lifetime.

## Execute for a specific tenant

An off-request worker has no HTTP tenant to inherit. Inject `ITenantScope` and begin a scope before calling the pipeline:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Tenancy;

public class TenantCartJob(ITenantScope tenants, ICommandPipeline pipeline)
{
    public async Task<CommandResult<CartLineId>> Add(TenantId tenant, Sku sku, Quantity quantity, CancellationToken stoppingToken)
    {
        using (tenants.Begin(tenant))
        {
            return await pipeline.Execute<CartLineId>(new AddItemToCart(sku, quantity), stoppingToken);
        }
    }
}
```

`ITenantScope.Begin` selects a nonempty tenant for the current async flow. That explicit selection wins over the configured resolver, including during an HTTP request and when an authentication scheme selects a different principal. Nested scopes restore the outer tenant; disposal restores the previously resolved tenant and is idempotent. If you dispose an outer scope before an inner one, the inner tenant stays selected. Ending the inner scope then skips the disposed outer tenant.

The same selection works with `Validate`, queries, reads through `IReadModels`, and typed or untyped execution, including cancellation-aware calls. It does not grant permission to use that tenant. Enforce tenant access and establish a trusted principal separately when required.

Keep captured async work within the intended tenant lifetime. Tasks or timers started inside a scope capture its tenant even after the parent disposes the scope; do not reuse that captured context for unrelated work. Independent HTTP requests reset tenant selection on entry to Arc's request pipeline. Observable emissions restore the subscriber's tenant instead of inheriting the producer's scope.

On ASP.NET Core, Arc's startup filter installs that reset for all application middleware, including middleware registered before `UseCratisArc()`. It cannot cover middleware added by another `IStartupFilter` registered before Arc's; do not resolve tenant-scoped services there.

`ITenantScope` requires Arc's `TenantIdAccessor` as the effective `ITenantIdAccessor` registration. If a custom accessor is registered before or after Arc, resolving `ITenantScope` throws `ExplicitTenantScopeRequiresArcAccessor`. This prevents a selection that other services cannot see.

Create a new DI scope inside the tenant scope before resolving tenant-scoped services such as Chronicle's event store or a `DbContext`. Changing the current tenant does not rebind a service already resolved in an existing DI scope.

Scope-free `Execute` and `Validate` create that inner DI scope for you. If you pass an `IServiceProvider`, create its scope after `Begin` and dispose it when the work finishes. Apply the same lifetime rule to other tenant-scoped reads.

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

These cancellation extension overloads forward the token to Arc's cancellation-aware pipeline. A custom implementation of only `ICommandPipeline`, without `ICommandPipelineWithCancellation`, receives the compatibility call without a token. Awaiting the delayed command waits for its task. Cancellation follows the pipeline's result/error path, so inspect the result; do not rely on every cancellation throwing to the caller.

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

## How parameters are bound

The pipeline resolves `Handle()` and `Provide()` parameters in order, awaiting each resolution. Await `Execute` rather than blocking on it.

If you write a parameter-resolution plugin, an existing synchronous `ICanResolveParameterDependency` implementation still works. Add `IAsyncParameterDependencyResolver` when acquiring the dependency requires asynchronous work. Its `Resolve` method returns a `ParameterDependencyResolution`: `NotResolved` when the plugin does not own the parameter, or `Resolved(dependency)` when it does, even if the dependency is null.

Resolvers are consulted in discovery order. The first one that owns the parameter wins; if none does, the container resolves the type.

This covers parameter binding only. It does not make general dependency injection, constructor injection or custom synchronous factories nonblocking. Chronicle read-model parameters use this contract, see [injecting read models](../chronicle/read-models/injecting-into-commands.md#reads-are-awaited).

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

Use this two-call flow when the caller needs early feedback. Execution already validates the command, so a separate `Validate` call is not a prerequisite. It has a scope-explicit overload and [severity thresholds](./validation-severity-filtering.md).

`Validate` skips `Provide()`, `Handle()`, response processing, and execution scopes. Context providers and command filters still run application code, which can have side effects. State can also change before execution; the fragment checks the execution result for that reason. Follow [pre-flight validation](./command-validation.md) to expose the check to a form.

## Commands without a request

Commands triggered by a Chronicle occurrence — a schedule, startup, shutdown or clock automation — are not run through `ICommandPipeline` directly. Use `IOccurrenceCommandExecutor` from `Cratis.Arc.Chronicle`, which keys the execution by occurrence and effect name and makes it retry-safe. See [Commands for occurrences](../chronicle/commands/occurrence-commands.md).

## Context and authentication

A fresh DI scope supplies service lifetimes, not a user or tenant identity. The pipeline uses the current correlation context when available. For authorization, it uses the HTTP request principal when one exists; otherwise it uses an explicitly established server-side principal. Before running protected work in a background process, follow [server-side authorization scopes](./model-bound/authorization.md#executing-commands-from-server-side-code). Background execution is not automatically authorized.
