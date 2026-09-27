---
title: Without wrappers
description: Return native ASP.NET controller results instead of Arc command/query envelopes.
---

Arc's participating controller actions normally return `CommandResult` or `QueryResult` envelopes. For an endpoint whose external contract must remain a native ASP.NET result, use `[AspNetResult]` on the controller or action.

## Opt out on an action

This complete controller fragment assumes an ASP.NET Core Arc host with MVC registration and controller mapping:

```csharp
using Cratis.Arc;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/status")]
public class StatusController : ControllerBase
{
    [HttpGet]
    [AspNetResult]
    public IActionResult Get() => Ok(new { State = "Running" });
}
```

The action's result remains native ASP.NET output rather than an Arc envelope. Move `[AspNetResult]` above `StatusController` to apply it to every action on that controller.

## Validation still applies

Opting out of response wrapping does not disable Arc's model-state validation checks. Invalid model state can still prevent action execution. Likewise, do not treat the attribute as a way to bypass authentication or endpoint authorization. Test error responses as well as successful payloads when defining an external API contract.

## Proxy generator

The proxy generator skips a controller action that carries `[AspNetResult]` itself; no Arc TypeScript proxy is generated for it. It checks the action only, not the controller: with `[AspNetResult]` on the controller class, its `[HttpGet]` and `[HttpPost]` actions still get proxies that expect Arc's envelope and cannot read the native response. Put the attribute on each action when no proxy should exist, and supply a client appropriate to your unwrapped contract instead.

## See also

- [Controller commands](../commands/controller-based.md)
- [Controller queries](../queries/controller-based/index.md)
- [OpenAPI](../open-api/index.md)
