---
title: Lightweight OpenAPI specifications
description: Expose route metadata with Arc.Core's built-in OpenAPI endpoint, without the ASP.NET integration package.
---

When you need a route catalog for a lightweight service, Core can generate a basic route document declaring OpenAPI 3.0, with the compatibility limitations below. This support is already in **`Cratis.Arc.Core`**. The separate `Cratis.Arc.OpenApi` package supplies [ASP.NET Core transformers](../open-api/index.md); it is not required for `ArcApplication.MapOpenApi()`.

## Getting started

This runnable `Program.cs` checkpoint assumes the console project from [getting started](getting-started.md):

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

`curl http://localhost:5000/openapi.json` returns a document with `openapi: "3.0.0"` and a `/health` path, alongside Arc's mapped routes. Mapping the document does not register Arc services or start the listener; the bootstrap above does both.

## Configuration options

`MapOpenApi` returns the application and accepts these optional arguments:

| Argument | Default |
| --- | --- |
| `pattern` | `/openapi.json` |
| `title` | `Arc Application` |
| `version` | `1.0.0` |

This **mapping fragment** replaces `app.MapOpenApi()` in the checkpoint:

```csharp
app.MapOpenApi(
    pattern: "/api/openapi.json",
    title: "Customer Management API",
    version: "1.0.0");
```

## Generated document structure

The generator reads currently registered routes when the document is requested. Endpoint metadata supplies operation IDs, summaries, and tags. The server URL is `/`. It emits generic 200, 401, and 500 response descriptions for each operation, not a comprehensive status contract.

For metadata with `AllowAnonymous = false`, it emits a Bearer/JWT security requirement and scheme. That is a **fixed documentation convention**: it does not inspect or install your actual authentication mechanism. It may not describe an API-key or forwarded-header deployment accurately.

## Limitations and production access

- No request-body or response-body schemas are generated. Do not use this document alone to generate a complete typed client.
- The document endpoint is explicitly anonymous. Restrict it at trusted ingress if route metadata should not be public.
- Documentation does not enforce permissions; see [authentication](authentication.md) and [manual endpoint boundaries](endpoint-mapping.md).
- The generator emits every registered HTTP method as a lowercase path-item member. With generated queries and the default `GeneratedApis.EnableQueryHttpMethod = true`, that includes `query`. OpenAPI 3.0 cannot represent this operation: strict tools may reject the document or ignore that member. The lightweight generator currently ignores `ExcludeFromApiDescription` metadata, including the QUERY reader's exclusion; the ASP.NET mapper handles that exclusion separately.
- If you choose to disable the QUERY transport, set `options.GeneratedApis.EnableQueryHttpMethod = false` in the `AddCratisArc` callback. Generated queries then accept GET only. This is an application transport choice, not a fix to the generator or a requirement to run Arc; verify clients do not depend on QUERY.
- The public lightweight mapping helpers currently expose GET and POST, not ASP.NET's full routing API.

Validate compatibility before importing the document into Postman or pointing a separately hosted Swagger UI at it. For richer ASP.NET type schemas, use [Cratis.Arc.OpenApi](../open-api/index.md) or [Cratis.Arc.Swagger](../asp-net-core/swagger.md), and review their documented limitations too.

## See also

- [Endpoint mapping](endpoint-mapping.md) — supply route metadata.
- [Introspection](../introspection/index.md) — command/query contract discovery and exposure defaults.
- [Getting started](getting-started.md) — a complete standalone command/query checkpoint.
