# Swagger

Cratis Arc provides enhanced Swagger/OpenAPI support through the `Cratis.Arc.Swagger` package. This extension automatically configures Swagger to properly handle the Arc's specific features and conventions.

## Overview

The Swagger extension adds several filters and enhancements to provide accurate API documentation for:

- **Concepts** - Properly represents concept types as their underlying primitive types
- **Commands** - Adds correct response schemas with validation and error handling
- **Queries** - Includes pagination/sorting parameters and proper response schemas
- **FromRequest attributes** - Correctly handles complex model binding scenarios
- **Model-bound endpoints** - Supports minimal API endpoints for commands and queries

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
public class UserId : ConceptAs<Guid>;

// In Swagger, UserId parameters will appear as string (UUID format)
// instead of a complex object with a Value property
```

### Command Result Operation Filter

Enhances command endpoints by:

- Adding proper `CommandResult` or `CommandResult<T>` response schemas
- Including standard HTTP status codes (200, 400, 403, 500) with appropriate error schemas
- Handling void/Task return types correctly
- Supporting concept return types

### Query Result Operation Filter

Enhances query endpoints by:

- Adding `QueryResult` response schemas
- Including standard HTTP status codes with error handling
- Automatically adding pagination and sorting parameters for enumerable results
- Supporting concept return types

### FromRequest Operation Filter

Properly handles the `[FromRequest]` attribute by:

- Removing the parameter from the query string/path parameters
- Adding it as a request body with the correct JSON schema
- Supporting complex model binding scenarios

### Model-Bound Operation Filters

Provides support for minimal API endpoints that use model binding for commands and queries, ensuring they appear correctly in the Swagger documentation.

### Pagination and Sorting Parameters

For query endpoints that return enumerable results, the following query parameters are automatically added to the Swagger documentation:

| Parameter | Type | Description |
|-----------|------|-------------|
| `sortBy` | string | Field name to sort by |
| `sortDirection` | string | Sort direction (`asc` or `desc`) |
| `pageSize` | integer | Number of items per page |
| `page` | integer | Page number (0-based) |

## Enum Schema Filter

Provides proper schema generation for enum types, ensuring they are documented correctly in the API specification.

## Response Schemas

The Swagger extension automatically adds consistent response schemas for all command and query endpoints:

### Success Responses (200)

- Commands: `CommandResult` or `CommandResult<T>`
- Queries: `QueryResult` with the actual data type

### Error Responses

- **400 Bad Request**: Validation errors or malformed requests
- **403 Forbidden**: Authorization failures
- **500 Internal Server Error**: Unexpected server errors

All error responses use the same result schema as success responses but with error information populated.

## Integration with Arc Features

The Swagger extension seamlessly integrates with other Arc features:

- **[FromRequest](./from-request.md)**: Properly documents complex model binding
- **[Commands](../commands/index.md)**: Accurate documentation of command endpoints and responses
- **[Queries](../queries/index.md)**: Complete documentation including pagination for collection results
- **[Validation](./validation.md)**: Error responses include validation failure information
- **[Without Wrappers](./without-wrappers.md)**: Works correctly with unwrapped responses

This ensures that your API documentation accurately reflects the actual behavior and capabilities of your Arc-based API.
