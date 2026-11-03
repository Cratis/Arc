---
title: Pre-flight command validation
description: Check authorization and input without invoking a command's Provide or Handle methods.
---

Check a form before submission when the user needs feedback without attempting the change. Arc's `Validate` operation returns a `CommandResult` from pre-flight checks and skips the command handler. Use that result to explain what currently blocks the request. Submit the command separately when the user proceeds; the check reserves no state and cannot guarantee that execution will succeed.

## Backend support

For a model-bound command, inject `ICommandPipeline` and call `Validate`. Choose the scope-free form when Arc should own a fresh DI scope, or pass an existing `IServiceProvider` when you intend to share that scope and manage its lifetime. Both forms have optional severity and cancellation overloads.

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

Await the task before displaying feedback. Use `IsSuccess` for the overall verdict; `IsValid` alone ignores authorization and exception failures. If this check succeeds but execution later fails in the cart service, move only the rules suitable for early feedback into validators. Pre-flight cannot discover arbitrary checks inside a service that the handler has not called.

## What runs

The model-bound validation path finds the handler, builds context values, establishes a context, and runs command filters. Authorization preparation can reject a request before that work. Within the filter chain, authorization filters precede ordinary filters and unsuccessful results short-circuit the chain. Applicable [severity filtering](./validation-severity-filtering.md) determines which validation failures remain blocking.

It skips:

- `Provide()` and handler argument resolution;
- `Handle()`;
- response value handlers;
- execution scopes' `Begin` and `Complete` callbacks.

A pre-flight request can still construct a validator and read its dependencies. Context providers and custom filters also execute application code. Keep those collaborators safe to call repeatedly, including before the authorization-filter verdict; skipping the handler does not make them side-effect free or prevent them from revealing sensitive information.

## Model-bound endpoints

Arc adds `/validate` alongside a model-bound command's execute route. For example, **if the mapped execute route is** `/api/carts/add-item-to-cart`, validation is `POST /api/carts/add-item-to-cart/validate` with the same command payload. The base route depends on namespace/path configuration; it is not inferred from this example URL.

Both endpoints send a successfully bound command through model-bound command filters. An unbound body takes a different refusal path: authentication, roles, and explicitly resource-free policies are checked before returning `malformedRequest` with the failing member when available. Resource-dependent policies and authorization command filters need a command instance, so they do not run on that path; see [command input binding](./input-binding.md). Even with a valid body, pre-flight cannot evaluate business decisions that exist only in `Provide()`, `Handle()`, or a service invoked by them.

## Controller-based commands

With Arc's MVC integration, `[HttpPost]` actions with attribute route selectors receive corresponding `/validate` selectors unless `[AspNetResult]` opts the action or controller out of command handling. Use a `[FromBody]` parameter for the command payload, as in the complete [controller example](./controller-based.md).

`CommandValidationRouteConvention` adds the selector at startup. Requests still go through ASP.NET Core authorization, binding, and the action-filter pipeline; Arc's `CommandActionFilter` recognizes the `/validate` path and skips action execution. These are **MVC filters**, not the model-bound `ICommandFilter` chain. Other application middleware and filters may still run.

For the shown controller route:

- Execute: `POST /api/carts/add`
- Validate: `POST /api/carts/add/validate`

If the validation request returns a missing-route response, check Arc MVC configuration, POST command recognition, and the action's attribute route before changing the payload. An arbitrary MVC action does not necessarily receive a validation endpoint.

## Security and concurrency

> [!WARNING]
> Skipping a handler is not a guarantee of no data exposure. Validation messages and custom pre-flight collaborators can reveal sensitive information. Protect validation and execution entry points, and review their output under denied principals.

Use [authorization verdicts](./command-filters.md#cross-cutting-authorization-by-namespace), not overridable validation errors, for access control. Model-bound authorization failures and MVC authorization responses belong to their respective pipelines; do not assume identical HTTP challenge behavior.

After a successful check, make the eventual `Execute` call and inspect its result too. State can change between requests. Handler-only checks, service failures, or scope completion can still reject execution. Keep concurrency-sensitive invariants in the operation that performs the change, where the check and write can be coordinated.

## Frontend usage

Use [core command validation](../../../frontend/core/commands/validation.md) or [React command validation](../../../frontend/react/commands/validation.md) for pre-flight feedback. For warning confirmation, use an explicit warning-blocking first threshold as described in [severity filtering](./validation-severity-filtering.md#confirm-a-warning).
