---
title: Endpoint mapping
description: Map manual lightweight HTTP endpoints and understand their authentication and routing limits.
---

Arc maps commands and queries automatically. For a health check or a small custom HTTP operation, you can map a handler directly. A manual handler does **not** enter the command/query pipelines unless you explicitly call them.

## Basic usage

This runnable `Program.cs` checkpoint assumes a console project with `Cratis.Arc.Core` installed, as in [getting started](getting-started.md):

```csharp
using Cratis.Arc;
using Cratis.Arc.Http;

var builder = ArcApplication.CreateBuilder(args);
builder.AddCratisArc(options =>
    options.Hosting.ApplicationUrl = "http://localhost:5000/");
var app = builder.Build();
app.UseCratisArc();

app.MapGet("/health", context => context.Write("OK"),
    new EndpointMetadata(Name: "Health", AllowAnonymous: true));

await app.RunAsync();
```

`curl http://localhost:5000/health` returns `OK`. `UseCratisArc()` schedules listener startup; omitting it leaves this program without the listener activation required by `RunAsync()`.

## Method signatures

Both extension methods return the application for chaining:

| Method | Arguments |
| --- | --- |
| `MapGet` | `string pattern`, `Func<IHttpRequestContext, Task> handler`, optional `EndpointMetadata? metadata = null` |
| `MapPost` | The same arguments |

The current lightweight mapping matches literal paths, not route-template parameters. Use a query parameter for an ID rather than `/users/{id}`. These public helpers expose GET and POST; choose ASP.NET Core for advanced routing and its broader HTTP API surface.

## Working with IHttpRequestContext

The following **mapping fragment** belongs before `RunAsync()` in the checkpoint:

```csharp
app.MapGet("/lookup", async context =>
{
    if (!context.Query.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
    {
        context.SetStatusCode(400);
        await context.Write("An id query parameter is required.");
        return;
    }

    var result = new { Id = id };
    context.SetResponseHeader("Cache-Control", "no-store");
    await context.WriteResponseAsJson(result, result.GetType());
}, new EndpointMetadata(Name: "Lookup", AllowAnonymous: true));
```

For `/lookup?id=123`, the result is `{"id":"123"}`. Other context members include `Headers`, `Cookies`, `Path`, `Method`, `User`, `RequestServices`, and `RequestAborted`. Read JSON with `ReadBodyAsJson(typeof(T))`; use the request scope's service provider for dependencies. Treat headers, cookies, and query values as untrusted input.

## Endpoint metadata

| Property | Purpose |
| --- | --- |
| `Name` | Endpoint identifier and OpenAPI operation ID |
| `Summary` | Documentation text |
| `Tags` | Documentation grouping |
| `AllowAnonymous` | Whether lightweight authentication middleware lets an unauthenticated request proceed |

With authentication handlers available, `AllowAnonymous: false` requires a successful authentication result. **With no handlers, current middleware proceeds without authentication.** Metadata is not a role policy and does not run Arc command/query authorization. See [authentication limits](authentication.md#endpoint-enforcement-and-limits).

For private manual endpoints, also check the authenticated principal and application permission before accessing data, or invoke the appropriate authorized pipeline. For webhooks, verify the provider's signature over the original payload using its supported library **before** processing it. Merely reading a signature header is not verification.

## Integration with OpenAPI

In the checkpoint, add `using Cratis.Arc.OpenApi;` and call `app.MapOpenApi()` before `RunAsync()`. This exposes `/openapi.json`. The lightweight document describes routes and metadata, not complete body schemas, and is anonymously accessible by default. See [Core OpenAPI](openapi.md) for its limits and the separate ASP.NET implementation.

## See also

- [Getting started](getting-started.md) — register and activate the host.
- [Authentication](authentication.md) — establish trusted request principals.
- [Authorization](authorization.md) — protect pipeline execution.
- [Static files](static-files.md) — assets and SPA fallback.
