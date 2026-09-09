# Swagger

Cratis Arc provides Swagger/OpenAPI filters through the optional `Cratis.Arc.Swagger` package for ASP.NET Core hosts. These describe Arc conventions, but do not guarantee that every generated schema matches the runtime contract. Review the [current limitations](#current-limitations) before generating clients.

## Overview

The Swagger extension adds filters for:

- **Concepts** - Maps concept types to their underlying primitive types
- **Commands** - Adds result-wrapper and error response schemas
- **Queries** - Adds result-wrapper schemas and paging/sorting parameters where applicable
- **FromRequest attributes** - Describes request-body binding
- **Model-bound endpoints** - Attempts to enrich generated command and query operations

## Setup

To use the Swagger enhancements, add the extension to your Swagger configuration:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.AddConcepts();
});
```

The `AddConcepts()` method adds all the necessary filters and operation filters automatically.

## Features

### Concept Schema Filter

Automatically maps concept types (types inheriting from `ConceptAs<T>`) to their underlying primitive types in the Swagger schema. This ensures that concepts appear as their actual data types (string, int, Guid, etc.) rather than complex objects in the API documentation.

**Example:**

```csharp
using Cratis.Concepts;

public record UserId(Guid Value) : ConceptAs<Guid>(Value);

// In Swagger, UserId parameters will appear as string (UUID format)
// instead of a complex object with a Value property
```

### Command Result Operation Filter

Enhances command endpoints by:

- Adding `CommandResult` or `CommandResult<T>` response schemas based on inferred return types
- Including standard HTTP status codes (200, 400, 403, 500) with appropriate error schemas
- Handling void/Task return types correctly
- Supporting concept return types

### Query Result Operation Filter

Enhances query endpoints by:

- Adding `QueryResult` response schemas
- Including standard HTTP status codes with error handling
- Adding pagination and sorting parameters where the filter detects paging support
- Supporting concept return types

### FromRequest Operation Filter

Properly handles the `[FromRequest]` attribute by:

- Removing the parameter from the query string/path parameters
- Adding it as a request body with the correct JSON schema
- Supporting complex model binding scenarios

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

- **Command adapter inference:** The model-bound command filter inspects the registered adapter's `Handle`, not the command record's return contract. The standard adapter returns `ValueTask<object?>`; this does not recover your concrete DTO, `Guid`, or typed-result response schema. Ordinary `Guid` command responses remain supported at runtime — the gap is schema inference.
- **Query matching and requiredness:** The query filter compares the operation-name suffix with `performer.Name`, while the GET endpoint mapper names operations with `performer.FullyQualifiedName`. It can therefore miss normal model-bound queries entirely. If it matches, it marks all query parameters `Required = false`, including arguments that runtime binding requires.
- **Query data schema:** The model-bound filter generates plain `QueryResult`, not a wrapper specialized to the query's concrete `data` type. Do not assume that this schema is enough to generate a typed query client.
- **Enum type mismatch:** The enum filter inserts string names without changing the schema type. An integer schema with string enum members is inconsistent with itself and with Arc's default numeric payloads.

Switching between `Cratis.Arc.OpenApi` and `Cratis.Arc.Swagger` does not resolve these shared gaps. Inspect generated documents and compare representative command/query payloads, required arguments, enums, and errors with actual HTTP responses before client generation.

## Integration with Arc Features

Use these guides to establish the runtime contract independently of the generated document:

- **[FromRequest](./from-request.md)**: Request binding
- **[Commands](../commands/index.md)**: Command execution and responses
- **[Queries](../queries/index.md)**: Query results and paging
- **[Validation](./validation.md)**: Validation failure information
- **[Without Wrappers](./without-wrappers.md)**: Response unwrapping — verify the schema against the unwrapped payload too
