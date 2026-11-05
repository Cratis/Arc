---
title: Generate Swagger documents for Cratis Arc
description: Add Cratis Arc filters to Swashbuckle to describe commands, queries, and concepts, then check schema limits before generating API clients.
---

Use the optional `Cratis.Arc.Swagger` package to describe Cratis Arc commands, queries, and concept types in a Swashbuckle-generated OpenAPI document. In an ASP.NET Core host, you can serve that document and Swagger UI for people exploring the API.

The filters describe Arc conventions, but some generated schemas differ from the runtime contract. Check the [current limitations](#current-limitations) before using the document to generate clients.

## Overview

Arc's filters map concept types to their underlying primitives and add command/query result wrappers to matching operations. They also describe `[FromRequest]` body parameters and add paging and sorting parameters where supported. Model-bound endpoint matching and response-type inference have the limits described below.

## Setup

Add the `Cratis.Arc.Swagger` package, which brings Swashbuckle with it, then register the document generator with Arc's filters and expose the document and UI in `Program.cs`:

```csharp
using Cratis.Arc.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AddConcepts());

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCratisArc();
await app.RunAsync();
```

`AddConcepts()` registers the schema and operation filters; it does not serve anything by itself. `UseSwagger()` serves `/swagger/v1/swagger.json`, which lists the Arc command, validate, and query routes alongside Arc's `/.cratis/*` endpoints, and `UseSwaggerUI()` serves the browser UI at `/swagger`. The example serves both only in Development; decide deliberately whether a production host should publish its API description.

## Features

### Concept Schema Filter

A concept is a domain-specific value type inheriting from `ConceptAs<T>`, such as `UserId` around a `Guid`. The concept schema filter describes its underlying primitive instead of an object with a `Value` property:

**Example:**

```csharp
using Cratis.Concepts;

public record UserId(Guid Value) : ConceptAs<Guid>(Value);

// In Swagger, UserId parameters will appear as string (UUID format)
// instead of a complex object with a Value property
```

### Command Result Operation Filter

For matching controller command actions, the filter infers a `CommandResult` or `CommandResult<T>` response from the return type, including concept return types and void/Task forms. It adds result schemas and describes HTTP 400, 403, and 500 error responses. Model-bound commands use a separate filter with the [adapter-inference limit](#current-limitations) below.

### Query Result Operation Filter

For matching controller query actions, the filter adds a non-generic `QueryResult` schema and error response schemas. It adds paging and sorting parameters when it detects paging support. Model-bound queries use a separate filter, whose matching and typed-data limits are described below.

### FromRequest Operation Filter

The filter removes a matching `[FromRequest]` parameter from the operation's query/path parameter list and describes it as an `application/json` request body. This does not establish that every route and query property in the merged model is documented correctly. Check those parameters against the [binding contract](from-request.md).

### Model-Bound Operation Filters

Attempts to match generated command and query operations to registered handlers and performers, then adds request and result schemas. Matching and type inference have the limitations below.

### Pagination and Sorting Parameters

For matched model-bound query performers with `SupportsPaging = true` (`IQueryable` results), the filter adds the following parameters:

| Parameter | Type | Description |
| ----------- | ------ | ------------- |
| `sortby` | string | Field name to sort by |
| `sortDirection` | string | Sort direction (`asc` or `desc`) |
| `pageSize` | integer | Number of items per page |
| `page` | integer | Page number (0-based) |

## Enum Schema Filter

Replaces enum values with their names, without changing the schema type. This can leave string members in an integer schema and does not change Arc's default numeric wire format. See [enum limitations](../open-api/enums.md).

## Response Schemas

The filters add the following response descriptions when they match an operation. These are generated descriptions, not proof of the runtime payload or every possible status:

### Success Responses (200)

- Commands: `CommandResult` or `CommandResult<T>`
- Queries: `QueryResult`; the model-bound filter does not supply a concrete typed `data` contract

### Error Responses

- **400 Bad Request**: Validation errors or malformed requests
- **403 Forbidden**: Authorization failures
- **500 Internal Server Error**: Unexpected server errors

The model-bound filters reuse the result schema for the listed error responses; compare it with actual error responses from your host.

## Current limitations

The Swagger implementations share the limitations of the [Microsoft OpenAPI model-bound transformers](../open-api/model-bound.md) and [enum transformer](../open-api/enums.md):

- The model-bound command filter inspects the registered adapter's `Handle`, not the command record's return contract. The standard adapter returns `ValueTask<object?>`; this does not recover your concrete DTO, `Guid`, or typed-result response schema. Ordinary `Guid` command responses remain supported at runtime; the gap is schema inference.
- The query filter compares the operation-name suffix with `performer.Name`, while the GET endpoint mapper names operations with `performer.FullyQualifiedName`. It can therefore miss normal model-bound queries entirely. If it matches, it marks all query parameters `Required = false`, including arguments that runtime binding requires.
- The model-bound query filter generates plain `QueryResult`, not a wrapper specialized to the query's concrete `data` type. Do not assume this schema is enough to generate a typed query client.
- The enum filter inserts string names without changing the schema type. An integer schema with string enum members is inconsistent with itself and with Arc's default numeric payloads.

Switching between `Cratis.Arc.OpenApi` and `Cratis.Arc.Swagger` does not resolve these shared gaps. Inspect generated documents and compare representative command/query payloads, required arguments, enums, and errors with actual HTTP responses before client generation.

## Integration with Arc Features

Use these guides to establish the runtime contract independently of the generated document:

- [FromRequest](./from-request.md): establish which source supplies each request value.
- [Commands](../commands/index.md): check execution and response behavior.
- [Queries](../queries/index.md): check query results and paging.
- [Validation](./validation.md): understand validation failure information.
- [Without Wrappers](./without-wrappers.md): compare the schema with endpoints that return unwrapped payloads.
