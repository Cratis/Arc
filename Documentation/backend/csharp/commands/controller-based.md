---
title: Cratis Arc commands with ASP.NET Core controllers
description: Use ASP.NET Core MVC actions as Cratis Arc commands, returning command results while keeping MVC routing, binding, authorization, and filters.
---

Controller-based commands let an ASP.NET Core MVC action return a Cratis Arc command result while MVC owns routing, binding, authorization, and action invocation. Choose this path when extending an existing controller API or when you need MVC's action-level control.

Use a [model-bound `[Command]` record](./model-bound/index.md) instead when you want the same command to run through `ICommandPipeline` from HTTP or application code. Controller actions do not pass through that pipeline automatically.

## Define the action

The controller below reuses the shared cart concepts and `ICartService` implementation from the [service-backed example](./model-bound/index.md#a-service-backed-command). Configure Arc's MVC integration and register that service in your host. `AddCartLine` is a data transfer object (DTO) for the request body, not a model-bound command.

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

By default Arc wraps controller POST results in a `CommandResult`. If an action needs ASP.NET's own result behavior, mark it with Arc's `[AspNetResult]`. See [without wrappers](../asp-net-core/without-wrappers.md) for its namespace and usage.

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

Follow [pre-flight command validation](./command-validation.md#controller-based-commands) to check route prerequisites and understand which code still runs during validation. Protect controllers and actions with Microsoft ASP.NET Core authorization attributes; [Arc model-bound authorization](./model-bound/authorization.md) uses a separate attribute contract.
