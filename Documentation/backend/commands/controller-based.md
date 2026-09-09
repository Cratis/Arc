---
title: Controller-based commands
description: Use Arc command result wrapping with ASP.NET Core MVC actions.
---

When an existing HTTP API needs MVC action control, a controller action can represent the command. This is a different execution path from a model-bound `[Command]` record: ASP.NET Core handles routing, binding, authorization, and action invocation.

## Define the action

This complete controller declaration reuses the shared cart concepts and fully implemented `ICartService` from the [service-backed example](./model-bound/index.md#a-service-backed-command). Configure Arc's MVC integration and register that service in your host. `AddCartLine` is a body DTO, not a model-bound command.

```csharp
using Microsoft.AspNetCore.Mvc;

public record AddCartLine(Sku Sku, Quantity Quantity);

[Route("api/carts")]
public class CartsController(ICartService carts) : ControllerBase
{
    [HttpPost("add")]
    public CartLineId AddItemToCart([FromBody] AddCartLine command) =>
        carts.AddItem(command.Sku, command.Quantity);
}
```

Posting valid input to `/api/carts/add` calls the service and returns its `CartLineId` in Arc's command-result wrapper. The example's store is in-memory, not durable or multi-user. Add real MVC validation and authorization before using this pattern for protected data.

The [proxy generator](../proxy-generation/index.md) uses the action method name for the generated TypeScript command. A returned task is awaited by MVC; an action returning `Task` must return actual asynchronous work, not an empty method body.

## Bypassing command result wrappers

By default Arc wraps controller POST results in a `CommandResult`. Use Arc's `[AspNetResult]` when you intentionally need ASP.NET's result behavior instead. See [without wrappers](../asp-net-core/without-wrappers.md) for its namespace and usage.

Do not assume model-bound tuple response handlers, command filters, or execution scopes also process controller results. Return the actual response DTO or value from an MVC action and use MVC's own extension points.

## Automatic validation endpoints

Arc's MVC convention adds a validation selector for the `[HttpPost]` action above. Actions or controllers marked `[AspNetResult]` are excluded from this convention:

- Execute: `POST /api/carts/add`
- Validate: `POST /api/carts/add/validate`

The validation route accepts the same body and runs through ASP.NET Core binding and filters, but Arc skips the action itself. It therefore does not call `ICartService.AddItem`. Preconditions checked only inside that service are not pre-flight validation rules.

See [pre-flight command validation](./command-validation.md#controller-based-commands) for route prerequisites, side-effect limits, and differences from model-bound validation. Use Microsoft ASP.NET Core authorization attributes on controllers/actions; [Arc model-bound authorization](./model-bound/authorization.md) documents its separate attribute contract.
