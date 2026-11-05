---
title: Expose route metadata with Cratis Arc.Core OpenAPI
description: Use MapOpenApi() in Cratis Arc.Core to expose routes and endpoint metadata as a basic OpenAPI document, without body schemas or ASP.NET Core.
---

`MapOpenApi()` exposes a lightweight service's routes and endpoint metadata as a document declaring OpenAPI 3.0. Use it to inspect the HTTP surface of a `Cratis.Arc.Core` service, not to generate a complete typed client: it does not include request or response body schemas.

The method is included in `Cratis.Arc.Core`. You do not need the separate `Cratis.Arc.OpenApi` package, which supplies [ASP.NET Core transformers](../open-api/index.md).

## Getting started

In the console project from [getting started](getting-started.md), use this complete `Program.cs` to map a health check and the document endpoint:

```csharp
using Cratis.Arc;
using Cratis.Arc.Http;
using Cratis.Arc.OpenApi;

var builder = ArcApplication.CreateBuilder(args);
builder.AddCratisArc(options =>
    options.Hosting.ApplicationUrl = "http://localhost:5000/");
var app = builder.Build();
app.UseCratisArc();

app.MapGet("/health", context => context.Write("OK"),
    new EndpointMetadata(
        Name: "Health",
        Summary: "Reports that the service is running",
        Tags: ["Operations"],
        AllowAnonymous: true));
app.MapOpenApi();

await app.RunAsync();
```

Start the service, then call `curl http://localhost:5000/openapi.json`. The document contains `openapi: "3.0.0"` and a `/health` path alongside Arc's mapped routes. Keep the host setup shown above: `MapOpenApi()` does not register Arc services or start the listener.

## Configuration options

`MapOpenApi` returns the application and accepts these optional arguments:

| Argument | Default |
| --- | --- |
| `pattern` | `/openapi.json` |
| `title` | `Arc Application` |
| `version` | `1.0.0` |

To change the document URL and API information, replace `app.MapOpenApi()` in the example above with:

```csharp
app.MapOpenApi(
    pattern: "/api/openapi.json",
    title: "Customer Management API",
    version: "1.0.0");
```

## Generated document structure

The generator reads currently registered routes when the document is requested. Endpoint metadata supplies operation IDs, summaries, and tags. The server URL is `/`. It emits generic 200, 401, and 500 response descriptions for each operation, not a comprehensive status contract.

For metadata with `AllowAnonymous = false`, it emits a Bearer/JWT security requirement and scheme. This is a fixed documentation convention, not detection or configuration of your authentication mechanism. If your service uses API keys or forwarded headers, the generated security description may not match it.

## Limitations and production access

- No request-body or response-body schemas are generated. Do not use this document alone to generate a complete typed client.
- The document endpoint is explicitly anonymous. Restrict it at trusted ingress if route metadata should not be public.
- Documentation does not enforce permissions; see [authentication](authentication.md) and [manual endpoint boundaries](endpoint-mapping.md).
- The generator emits eligible registered HTTP methods as lowercase path-item members. OpenAPI 3.0 cannot represent a `query` operation: strict tools may reject the document or ignore that member if you manually expose it. The generated QUERY reader is excluded, as are commands or queries marked `[ExcludeFromDiscovery]`; the ASP.NET mapper also excludes these from its API descriptions.
- If you choose to disable the QUERY transport, set `options.GeneratedApis.EnableQueryHttpMethod = false` in the `AddCratisArc` callback. Generated queries then accept GET only. This is an application transport choice, not a fix to the generator or a requirement to run Arc; verify clients do not depend on QUERY.
- The public lightweight mapping helpers currently expose GET and POST, not ASP.NET's full routing API.

Before importing the document into Postman or pointing a separately hosted Swagger UI at it, check that the tool accepts the generated operations and that the security description matches your service. If you are using ASP.NET Core and need type schemas, continue with [Cratis.Arc.OpenApi](../open-api/index.md) or [Cratis.Arc.Swagger](../asp-net-core/swagger.md); each has its own setup and documented limits.

## See also

- [Endpoint mapping](endpoint-mapping.md) — supply route metadata.
- [Introspection](../introspection/index.md) — command/query contract discovery and exposure defaults.
- [Getting started](getting-started.md) — a complete standalone command/query checkpoint.
