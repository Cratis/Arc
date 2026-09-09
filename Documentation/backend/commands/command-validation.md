---
title: Pre-flight command validation
description: Check authorization and input without invoking a command's Provide or Handle methods.
---

A form can ask whether a command is currently acceptable before attempting the change. Arc's `Validate` operation returns a `CommandResult` from pre-flight checks, without running the command handler. It is early feedback, not a reservation or a guarantee that later execution will succeed.

## Backend support

For model-bound commands, inject `ICommandPipeline` and call `Validate`. Both a scope-free form and a form accepting an existing `IServiceProvider` are available, with optional severity and cancellation overloads.

This complete caller uses the [service-backed `AddItemToCart`](./model-bound/index.md#a-service-backed-command) definition and shared cart concepts. Configure its validators and authorization requirements separately:

```csharp
using System.Threading.Tasks;
using Cratis.Arc.Commands;

public class CartPreflight(ICommandPipeline pipeline)
{
    public Task<CommandResult> Check(Sku sku, Quantity quantity) =>
        pipeline.Validate(new AddItemToCart(sku, quantity));
}
```

Await the returned task and inspect `IsSuccess` for an overall verdict. `IsValid` alone ignores authorization and exception failures. With no validators, pre-flight does not discover arbitrary checks inside the cart service.

## What runs

The model-bound validation path finds the handler, builds context values, establishes a context, and runs command filters. Authorization filters run before ordinary filters, with short-circuiting on unsuccessful results. Applicable [severity filtering](./validation-severity-filtering.md) then determines the remaining validation results.

It skips:

- `Provide()` and handler argument resolution;
- `Handle()`;
- response value handlers;
- execution scopes' `Begin` and `Complete` callbacks.

Validators can still resolve dependencies and read services. Context providers, custom filters, validators, and their constructors are executable application code: Arc cannot guarantee that they are side-effect free or reveal no sensitive information. Design them for repeated pre-flight calls.

## Model-bound endpoints

Arc adds `/validate` alongside a model-bound command's execute route. For example, **if the mapped execute route is** `/api/carts/add-item-to-cart`, validation is `POST /api/carts/add-item-to-cart/validate` with the same command payload. The base route depends on namespace/path configuration; it is not inferred from this example URL.

Both endpoints use model-bound command filters. Validation does not run business decisions that exist only in `Provide()`, `Handle()`, or a service invoked by them.

## Controller-based commands

With Arc's MVC integration, `[HttpPost]` actions with attribute route selectors receive corresponding `/validate` selectors unless `[AspNetResult]` opts the action or controller out of command handling. Use a `[FromBody]` parameter for the command payload, as in the complete [controller example](./controller-based.md).

`CommandValidationRouteConvention` adds the selector at startup. Requests still go through ASP.NET Core authorization, binding, and the action-filter pipeline; Arc's `CommandActionFilter` recognizes the `/validate` path and skips action execution. These are **MVC filters**, not the model-bound `ICommandFilter` chain. Other application middleware and filters may still run.

For the shown controller route:

- Execute: `POST /api/carts/add`
- Validate: `POST /api/carts/add/validate`

If a route is missing, confirm that Arc MVC integration is configured and that the action is recognized as a POST command with an attribute route. Do not assume every arbitrary MVC action receives validation endpoints.

## Security and concurrency

> [!WARNING]
> Skipping a handler is not a guarantee of no data exposure. Validation messages and custom pre-flight collaborators can reveal sensitive information. Protect validation and execution entry points, and review their output under denied principals.

Use [authorization verdicts](./command-filters.md#cross-cutting-authorization-by-namespace), not overridable validation errors, for access control. Model-bound authorization failures and MVC authorization responses belong to their respective pipelines; do not assume identical HTTP challenge behavior.

Always inspect the result of the eventual `Execute` call too. State may change between requests, handler-only checks may reject the operation, or a service or execution scope may fail. Enforce concurrency-sensitive invariants in the operation that performs the change.

## Frontend usage

Use [core command validation](../../frontend/core/commands/validation.md) or [React command validation](../../frontend/react/commands/validation.md) for pre-flight feedback. For warning confirmation, use an explicit warning-blocking first threshold as described in [severity filtering](./validation-severity-filtering.md#confirm-a-warning).
