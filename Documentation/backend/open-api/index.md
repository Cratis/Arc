# Microsoft.AspNetCore.OpenApi

The `Cratis.Arc.OpenApi` package provides deep integration with `Microsoft.AspNetCore.OpenApi` (.NET 10+), automatically generating accurate API documentation for all Arc-specific features and conventions.

## Overview

| Topic | Description |
| ----- | ----------- |
| [Concepts](./concepts.md) | How concept types are mapped to their underlying primitive types in the API schema. |
| [Commands](./commands.md) | How command responses are wrapped with `CommandResult` in the API documentation. |
| [Queries](./queries.md) | How query responses are wrapped with `QueryResult`, including pagination parameters. |
| [Enums](./enums.md) | How enum values are represented as string names rather than integers. |
| [FromRequest Attribute](./from-request.md) | How complex model binding with `[FromRequest]` is reflected in the API schema. |
| [Model-Bound Operations](./model-bound.md) | How minimal API command and query endpoints appear in the API documentation. |

## Setup

Add the `Cratis.Arc.OpenApi` NuGet package to your project and call `AddConcepts()` inside your `AddOpenApi` configuration:

```csharp
builder.Services.AddOpenApi(options => options.AddConcepts());

app.MapOpenApi();
```

The `AddConcepts()` method registers all schema and operation transformers automatically.

## Requirements

The `Cratis.Arc.OpenApi` package targets `.NET 10` and later. The transformer API (`IOpenApiSchemaTransformer`, `IOpenApiOperationTransformer`) and the underlying `Microsoft.OpenApi` 2.x schema types are only available from .NET 10 onwards.

## Relationship to Swagger

Arc also ships a separate `Cratis.Arc.Swagger` package for Swashbuckle-based Swagger UI. Both packages cover the same set of Arc features, but use different APIs:

| | `Cratis.Arc.Swagger` | `Cratis.Arc.OpenApi` |
|---|---|---|
| Framework | Swashbuckle (`ISchemaFilter`, `IOperationFilter`) | `Microsoft.AspNetCore.OpenApi` (`IOpenApiSchemaTransformer`, `IOpenApiOperationTransformer`) |
| .NET version | net8.0+ | net10.0+ |
| Registration | `services.AddSwaggerGen(o => o.AddConcepts())` | `services.AddOpenApi(o => o.AddConcepts())` |

Both are fully independent; choose the one that matches your toolchain.

