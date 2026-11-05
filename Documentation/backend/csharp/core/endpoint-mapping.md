---
title: Map manual HTTP endpoints in Cratis Arc.Core
description: Add GET and POST handlers to a Cratis Arc.Core service, use the request context, and configure authentication without entering command or query pipelines.
---

Use `MapGet` or `MapPost` in a `Cratis.Arc.Core` service when you need a health check or custom HTTP operation outside Arc's automatically mapped commands and queries. A manual handler does not enter their pipelines unless you call them explicitly, so it does not inherit pipeline validation or authorization.

## Basic usage

Start with a console project containing `Cratis.Arc.Core`, as in [getting started](getting-started.md). This complete `Program.cs` maps a public health check:

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

Start the service, then call `curl http://localhost:5000/health`; the response is `OK`. Keep `UseCratisArc()` even for manual endpoints: it schedules listener startup so `RunAsync()` can serve requests.

## Method signatures

Both extension methods return the application for chaining:

| Method | Arguments |
| --- | --- |
| `MapGet` | `string pattern`, `Func<IHttpRequestContext, Task> handler`, optional `EndpointMetadata? metadata = null` |
| `MapPost` | The same arguments |

The current lightweight mapping matches literal paths, not route-template parameters. Use a query parameter for an ID rather than `/users/{id}`. These public helpers expose GET and POST; choose ASP.NET Core for advanced routing and its broader HTTP API surface.

## Working with IHttpRequestContext

To read a query parameter and return JSON, add this mapping before `RunAsync()` in the example above:

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
| `AllowAnonymous` | Lets the request proceed even if authentication fails |
| `RequireAuthentication` | Throws `AuthenticationRequiredWithoutHandlers` when no authentication handler is available |
| `Roles` | Comma-separated roles; the authenticated caller must have at least one |

With authentication handlers available, an endpoint without `AllowAnonymous: true` requires successful authentication. The middleware returns 401 if authentication fails and 403 if the principal lacks every role listed in `Roles`. `AllowAnonymous: true` bypasses both checks.

With no handlers, the middleware proceeds unless `RequireAuthentication = true`, in which case it throws instead. For private manual endpoints, set that property and leave `AllowAnonymous` false. Merely leaving `AllowAnonymous` at its default is not protection against a missing authentication setup. See [authentication limits](authentication.md#endpoint-enforcement-and-limits).

These metadata checks do not run Arc command/query authorization policies. Before accessing private data, check any application-specific permission against the authenticated principal, or invoke the appropriate authorized pipeline. For webhooks, verify the provider's signature over the original payload using its supported library before processing it. Reading a signature header alone is not verification.

## Integration with OpenAPI

To describe these routes, add `using Cratis.Arc.OpenApi;` and call `app.MapOpenApi()` before `RunAsync()`. This exposes `/openapi.json` anonymously. The document includes routes and metadata, not complete body schemas; review [Core OpenAPI](openapi.md) before exposing it or using it with client tooling. That page also distinguishes the separate ASP.NET implementation.

## See also

- [Getting started](getting-started.md) — register and activate the host.
- [Authentication](authentication.md) — establish trusted request principals.
- [Authorization](authorization.md) — protect pipeline execution.
- [Static files](static-files.md) — assets and SPA fallback.
