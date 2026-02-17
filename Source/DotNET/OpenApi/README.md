# Cratis Arc OpenApi

This project provides OpenAPI support for Cratis Arc applications using `Microsoft.AspNetCore.OpenApi`.

## Features

The package provides schema and operation transformers that enhance OpenAPI documentation for Arc applications:

### Schema Transformers

- **ConceptSchemaTransformer**: Unwraps `ConceptAs<T>` types to their underlying primitive types in the schema
- **EnumSchemaTransformer**: Represents enums as string names instead of integers
- **FromRequestSchemaTransformer**: Removes properties decorated with `[FromRoute]` or `[FromQuery]` from request body schemas

### Operation Transformers

- **CommandResultOperationTransformer**: Wraps command responses in `CommandResult` or `CommandResult<T>` unless marked with `[AspNetResult]`
- **QueryResultOperationTransformer**: Wraps query responses in `QueryResult` unless marked with `[AspNetResult]`
- **FromRequestOperationTransformer**: Handles parameters marked with `[FromRequest]` attribute
- **Model Bound Transformers**: Support for model-bound minimal API commands and queries

## Usage

Add the OpenApi package to your project and configure it in your `Program.cs`:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddConcepts();
});

var app = builder.Build();
app.MapOpenApi();
```

The `AddConcepts()` extension method registers all the transformers for Arc-specific features.

## Differences from Swagger Project

While this project provides the same functionality as the Swagger project, it uses different APIs:

- **Swagger**: Uses `Swashbuckle.AspNetCore` with `ISchemaFilter` and `IOperationFilter` (synchronous)
- **OpenApi**: Uses `Microsoft.AspNetCore.OpenApi` with `IOpenApiSchemaTransformer` and `IOpenApiOperationTransformer` (async)

Both packages use the same `Microsoft.OpenApi` types for representing OpenAPI schemas and operations.

## Requirements

- .NET 9.0 or later
- Microsoft.AspNetCore.OpenApi
