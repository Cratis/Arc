---
title: Cratis Arc controller responses without wrappers
description: Use Cratis Arc's AspNetResult attribute to return native ASP.NET Core controller responses and control the status and body without command or query envelopes.
---

Use Cratis Arc's `[AspNetResult]` when a controller endpoint must return a native ASP.NET Core response instead of a `CommandResult` or `QueryResult`. This lets you keep an existing API's response contract or return a status endpoint's payload directly, without Arc's envelope, the object that normally contains the data and result metadata.

## Opt out on an action

In an [ASP.NET Core Arc host](index.md) with MVC registration and controller mapping, put `[AspNetResult]` on the action whose response you want to control:

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

A `GET /api/status` request returns HTTP 200 with the object passed to `Ok`. With default JSON naming, the body is:

```json
{"state":"Running"}
```

There is no `data` or `response` field around that object and no Arc result metadata. Move `[AspNetResult]` above `StatusController` to apply native result behavior to every action on that controller. The [proxy generator](#proxy-generator) treats controller-level and action-level attributes differently.

## Control the status and body

Because the action returns its ASP.NET result directly, the result you choose controls the HTTP status and payload. For example, if this status endpoint needs to report that the service is starting, return `StatusCode(503, new { State = "Starting" })` in place of `Ok(new { State = "Running" })`. The response is then HTTP 503 with this body:

```json
{"state":"Starting"}
```

This illustrates the response contract only; the action above does not check service readiness. Choose the status from your application's actual state.

## Validation still applies

`[AspNetResult]` does not bypass authentication or endpoint authorization, and it does not turn off model-state checks. For a POST action, invalid model state can still skip execution and produce an Arc command-result validation response. With `[ApiController]`, ASP.NET Core can reject invalid input earlier with its own HTTP 400 ProblemDetails response.

There is a narrower validation change: Arc's MVC validator adapter skips validators derived from `ConceptValidator<>` when `[AspNetResult]` is present on the action or controller. It still runs other discovered model validators, and ASP.NET Core validation attributes still apply. See [MVC input validation](validation.md) before relying on a concept rule at an unwrapped endpoint.

When preserving an external API contract, check invalid input and authorization failures as well as the successful body. The attribute alone does not give every failure the same response shape.

## Proxy generator

The proxy generator skips an action that carries `[AspNetResult]` itself, so the example's `Get` action has no generated Arc TypeScript proxy. Call it with a client that reads its native response body.

The generator checks the action's attributes only. If `[AspNetResult]` appears solely on the controller class, its `[HttpGet]` and `[HttpPost]` actions still get proxies that expect Arc's envelope and cannot read the native response. Put the attribute on each action that should be excluded from generation.

## See also

- [Controller commands](../commands/controller-based.md)
- [Controller queries](../queries/controller-based/index.md)
- [OpenAPI](../open-api/index.md)
