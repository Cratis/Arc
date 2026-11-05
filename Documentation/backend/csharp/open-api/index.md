---
title: Configure OpenAPI for Cratis Arc
description: Install Cratis.Arc.OpenApi and configure ASP.NET Core OpenAPI documents for Arc concepts, commands, and queries, with controller and model-bound limits.
---

Use `Cratis.Arc.OpenApi` to describe Cratis Arc concepts, commands, and queries in an ASP.NET Core OpenAPI document. It adds transformers to `Microsoft.AspNetCore.OpenApi`: components that adjust generated type schemas and HTTP operation descriptions to reflect Arc conventions.

This package is for ASP.NET Core hosts. For a lightweight Arc.Core host, use [Core's built-in OpenAPI](../core/openapi.md). Before generating a client from either document, check which endpoint types it describes and where the schemas differ from the JSON sent over HTTP.

## Overview

| Topic | Description |
| ----- | ----------- |
| [Concepts](./concepts.md) | How concept types are mapped to their underlying primitive types in the API schema. |
| [Commands](./commands.md) | How command responses are wrapped with `CommandResult` in the API documentation. |
| [Queries](./queries.md) | How query responses are wrapped with `QueryResult`, including pagination parameters. |
| [Enums](./enums.md) | Enum schemas follow the effective JSON converter's numeric or string wire values. |
| [FromRequest Attribute](./from-request.md) | How complex model binding with `[FromRequest]` is reflected in the API schema. |
| [Model-Bound Operations](./model-bound.md) | How minimal API command and query endpoints appear in the API documentation. |

## Setup

Start with an existing ASP.NET Core Arc host targeting .NET 10, with `builder.AddCratisArc()` already registered. Add the NuGet package from the host project directory:

```shell
dotnet add package Cratis.Arc.OpenApi
```

Import `Cratis.Arc.OpenApi` and `Microsoft.Extensions.DependencyInjection` in `Program.cs`. Add this registration before `builder.Build()`, or add the `AddConcepts()` call to an existing `AddOpenApi` configuration:

```csharp
builder.Services.AddOpenApi(options => options.AddConcepts());
```

`AddConcepts()` registers the schema and operation transformers, including those for model-bound endpoints; it does not expose the document. After `builder.Build()`, map it with ASP.NET's `app.MapOpenApi()` (default `/openapi/v1.json`). Keep `app.UseCratisArc()` and the host's normal startup so Arc's command and query endpoints are mapped too. Expose the document only in intended environments or apply endpoint access controls; API documentation is not automatically private.

Once the host is running, inspect `/openapi/v1.json` for a command or query you intend to document. Compare its parameters and response schema with the endpoint's JSON before using it to generate a client. `MapOpenApi()` serves the document, not a browser UI; see [the Swagger setup](../asp-net-core/swagger.md) if you want a Swashbuckle-based UI.

## Controller and model-bound coverage

Controller operations and model-bound endpoints use different matching rules. Registration enables both sets of transformers, but it does not give them identical coverage:

| Endpoint type | What the transformers describe | What to check |
| --- | --- | --- |
| Controller command actions | `CommandResult` or `CommandResult<T>` schemas inferred from recognized command methods, plus error responses. | The inferred result type against the action's actual response. |
| Controller query actions | A non-generic `QueryResult` schema and error responses; paging and sorting parameters when the declared return type is detected as enumerable. | Typed query data and paging support against the endpoint contract. |
| Model-bound commands | Request payloads and result schemas for operations matched to registered command handlers. | Result inference inspects the handler adapter's `Handle` return type, which can differ from the command's own return type. |
| Model-bound queries | Parameters and result schemas only when the operation matches a registered query performer. | Generated operation IDs use the fully qualified query name, but the transformer matches the performer's simple name. Namespaced queries may not match; matched queries still use a non-generic `QueryResult`. |

These descriptions do not establish that every runtime status or payload is documented. Continue with [model-bound operations](./model-bound.md) for the matching rules and response-type limits, or [commands](./commands.md) and [queries](./queries.md) for controller behavior.

## Serialization boundaries

A concept is a domain value wrapped in `ConceptAs<T>`, such as a customer ID around a `Guid`. With Arc's default concept converters, concept values and collections use primitive schemas, and recursive values in dictionaries keyed by concepts use component references. A custom concept converter can change the JSON shape; OpenAPI leaves its schema unspecified unless you provide a matching schema transformer.

Other schema transformers follow the JSON converters configured for the endpoint. They describe date, time, URI and type strings, GeoJSON objects, and polymorphic base properties when the corresponding Arc converters are active. Controllers use Arc's JSON options, but `AddCratisArc()` does not install those non-concept converters on plain minimal APIs. Configure them explicitly on HTTP JSON options if needed; otherwise ASP.NET's defaults determine the format and schema. See [enums](./enums.md) for numeric and string converter behavior.

With Fundamentals' derived-type converter explicitly enabled, the base schema lists declared interface properties by camel-cased CLR name. A plain minimal API `Results.Ok<ISampleBase>(derived)` response uses the runtime concrete type: it includes derived-only properties but no `_derivedTypeId` discriminator. The base schema does not list those derived-only properties.

Serialization explicitly through the declared interface with Fundamentals' converter can include `_derivedTypeId`. Attributes such as `[JsonPropertyName]` and `[JsonIgnore]` on a base interface do not control its concrete property writes. Avoid indexers on derived types; the base schema excludes them. Check the actual endpoint's serialization rather than inferring its payload from the base schema.

## Requirements

`Cratis.Arc.OpenApi` targets `net10.0` and uses `Microsoft.AspNetCore.OpenApi` 10.x with the `Microsoft.OpenApi` 2.x schema types.

## Relationship to Swagger

If your host already uses Swashbuckle, follow the [Cratis.Arc.Swagger setup](../asp-net-core/swagger.md). That package registers Swashbuckle filters instead of Microsoft's OpenAPI transformers; each integration has its own schema and endpoint-matching limits:

| | `Cratis.Arc.Swagger` | `Cratis.Arc.OpenApi` |
| --- | --- | --- |
| Framework | Swashbuckle (`ISchemaFilter`, `IOperationFilter`) | `Microsoft.AspNetCore.OpenApi` (`IOpenApiSchemaTransformer`, `IOpenApiOperationTransformer`) |
| Host | ASP.NET Core | ASP.NET Core |
| Registration | `services.AddSwaggerGen(o => o.AddConcepts())` | `services.AddOpenApi(o => o.AddConcepts())` |

Choose the integration that matches your document generator. Registering `Cratis.Arc.OpenApi` does not configure Swagger UI.
