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

## Body-binding failures

MVC authorization runs before body binding. Arc converts formatter ModelState paths such as `$.Items[2].From` to validation members such as `items[2].from`, preserving MVC's messages. `[property: ExplicitOffset]` also works on controller body properties; see [command input binding](./input-binding.md) for supported date-time forms and normalization.

`[ApiController]` automatic ProblemDetails responses can short-circuit before Arc's action filter and retain ASP.NET's response shape. This behavior is unchanged; do not assume every binding error uses the command-result envelope.

## Bypassing command result wrappers

By default Arc wraps controller POST results in a `CommandResult`. Use Arc's `[AspNetResult]` when you intentionally need ASP.NET's result behavior instead. See [without wrappers](../asp-net-core/without-wrappers.md) for its namespace and usage.

POST controller commands run [execution scopes](./command-execution-scopes.md) before publishing their command result. Scopes do not run for `[AspNetResult]` actions, validation-only requests, or requests rejected by model-state validation. With Chronicle, a refused commit returns HTTP 400 or 500 and clears the response instead of reporting success. A controller that captures `unitOfWorkManager.Current` in its constructor still holds the request unit; resolve the current unit during action execution to use the command's unit of work.

Model-bound tuple response handlers and command filters do not process controller results. Return the actual response DTO or value from an MVC action and use MVC's own extension points.

:::note[Operation execution requires the command pipeline]
MVC action return values do not trigger [command operations](./operations/index.md). Keep returning the action's actual response. To use operation execution and compensation, explicitly execute a model-bound command through `ICommandPipeline` and handle its result. That command owns its own transaction: it commits before `Execute` returns and does not join the controller's unit of work.
:::

## Automatic validation endpoints

Arc's MVC convention adds a validation selector for the `[HttpPost]` action above. Actions or controllers marked `[AspNetResult]` are excluded from this convention:

- Execute: `POST /api/carts/add`
- Validate: `POST /api/carts/add/validate`

The validation route accepts the same body and runs through ASP.NET Core binding and filters, but Arc skips the action itself. It therefore does not call `ICartService.AddItem`. Preconditions checked only inside that service are not pre-flight validation rules.

See [pre-flight command validation](./command-validation.md#controller-based-commands) for route prerequisites, side-effect limits, and differences from model-bound validation. Use Microsoft ASP.NET Core authorization attributes on controllers/actions; [Arc model-bound authorization](./model-bound/authorization.md) documents its separate attribute contract.
