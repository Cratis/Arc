# OpenAPI Specifications

Arc provides built-in support for generating OpenAPI 3.0 specification documents from your registered HTTP endpoints. This allows you to document your API and integrate with tools like Swagger UI, Postman, and other OpenAPI-compatible clients.

## Overview

The OpenAPI support in Arc automatically generates a specification document based on:

- Registered routes (GET, POST, PUT, DELETE, PATCH)
- Endpoint metadata (operation IDs, summaries, tags)
- Authentication requirements
- Response codes

## Getting Started

To add OpenAPI support to your Arc application, you need to:

1. Add a reference to the `Cratis.Arc.OpenApi` package
2. Call the `MapOpenApi()` extension method on your `ArcApplication`

### Installation

Add the package reference to your project:

```bash
dotnet add package Cratis.Arc.OpenApi
```

### Basic Configuration

```csharp
using Cratis.Arc;
using Cratis.Arc.OpenApi;

var builder = ArcApplication.CreateBuilder(args);
var app = builder.Build();

// Map the OpenAPI endpoint
app.MapOpenApi();

await app.RunAsync();
```

This will expose the OpenAPI specification document at `/openapi.json` by default.

## Configuration Options

The `MapOpenApi()` method accepts several optional parameters to customize the generated document:

### Custom Endpoint Path

Change where the OpenAPI document is served:

```csharp
app.MapOpenApi(pattern: "/api/swagger.json");
```

### API Title and Version

Customize the API information in the document:

```csharp
app.MapOpenApi(
    title: "My API",
    version: "2.0.0");
```

### Complete Example

```csharp
app.MapOpenApi(
    pattern: "/api/openapi.json",
    title: "Customer Management API",
    version: "1.0.0");
```

## Generated Document Structure

The OpenAPI document includes:

### API Information

- **Title**: The name of your API
- **Version**: The API version

### Servers

- Default server URL (`/`)

### Paths

All registered routes with their:
- HTTP methods (get, post, put, delete, patch)
- Operation IDs (from endpoint metadata)
- Summaries (from endpoint metadata)
- Tags (from endpoint metadata)
- Response codes:
  - `200` - Success
  - `401` - Unauthorized (for authenticated endpoints)
  - `500` - Internal Server Error

### Security

If any endpoints require authentication (have `AllowAnonymous = false`), the document includes:
- Bearer token security scheme
- JWT format specification
- Security requirements per operation

## Endpoint Metadata

To provide rich OpenAPI documentation, use endpoint metadata when registering routes:

```csharp
app.MapGet("/api/customers", async context =>
{
    // Handler implementation
},
new EndpointMetadata(
    Name: "GetCustomers",
    Summary: "Retrieves all customers",
    Tags: ["Customers"],
    AllowAnonymous: false));
```

### Metadata Properties

- **Name**: Becomes the `operationId` in the OpenAPI document
- **Summary**: Becomes the `summary` in the OpenAPI document
- **Tags**: Used for grouping operations in API documentation tools
- **AllowAnonymous**: When `false`, adds security requirements to the operation

## Integration with API Clients

### Swagger UI

You can use the generated OpenAPI document with Swagger UI:

```html
<!DOCTYPE html>
<html>
<head>
    <title>API Documentation</title>
    <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css">
</head>
<body>
    <div id="swagger-ui"></div>
    <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
    <script>
        SwaggerUIBundle({
            url: '/openapi.json',
            dom_id: '#swagger-ui',
        });
    </script>
</body>
</html>
```

### Postman

Import the OpenAPI document into Postman:

1. Open Postman
2. Click **Import**
3. Choose **Link** and enter your OpenAPI URL (e.g., `http://localhost:5000/openapi.json`)
4. Click **Continue** to import the collection

### Code Generation

Use OpenAPI generators to create client libraries:

```bash
# Using openapi-generator-cli
openapi-generator-cli generate \
    -i http://localhost:5000/openapi.json \
    -g csharp \
    -o ./generated-client
```

## Example Application

Here's a complete example of an Arc application with OpenAPI support:

```csharp
using Cratis.Arc;
using Cratis.Arc.Http;
using Cratis.Arc.OpenApi;

var builder = ArcApplication.CreateBuilder(args);
var app = builder.Build();

// Configure OpenAPI endpoint
app.MapOpenApi(
    pattern: "/openapi.json",
    title: "Product Catalog API",
    version: "1.0.0");

// Register API endpoints
app.MapGet("/api/products", async context =>
{
    var products = new[]
    {
        new { Id = 1, Name = "Product 1", Price = 29.99 },
        new { Id = 2, Name = "Product 2", Price = 39.99 }
    };
    
    await context.WriteResponseAsJsonAsync(products, products.GetType());
},
new EndpointMetadata(
    Name: "ListProducts",
    Summary: "Get all products in the catalog",
    Tags: ["Products"],
    AllowAnonymous: true));

app.MapPost("/api/products", async context =>
{
    var product = await context.ReadBodyAsJsonAsync(typeof(object));
    context.SetStatusCode(201);
    await context.WriteResponseAsJsonAsync(product, product?.GetType() ?? typeof(object));
},
new EndpointMetadata(
    Name: "CreateProduct",
    Summary: "Create a new product",
    Tags: ["Products"],
    AllowAnonymous: false));

await app.RunAsync();
```

## Best Practices

### Use Descriptive Names

Provide clear operation IDs and summaries:

```csharp
new EndpointMetadata(
    Name: "GetCustomerById",
    Summary: "Retrieves a customer by their unique identifier",
    Tags: ["Customers"])
```

### Organize with Tags

Group related endpoints together:

```csharp
// All customer-related endpoints use the "Customers" tag
Tags: ["Customers"]

// Order-related endpoints use the "Orders" tag
Tags: ["Orders"]
```

### Document Security Requirements

Be explicit about authentication:

```csharp
// Public endpoint
new EndpointMetadata(
    Name: "GetPublicData",
    AllowAnonymous: true)

// Protected endpoint
new EndpointMetadata(
    Name: "GetUserData",
    AllowAnonymous: false)
```

### Keep Versions Updated

Update the version when making breaking changes:

```csharp
app.MapOpenApi(
    title: "My API",
    version: "2.0.0");  // Incremented for breaking changes
```

## Limitations

The current OpenAPI implementation:

- Generates basic request/response schemas (no detailed type information)
- Does not include request body schemas
- Does not include response body schemas
- Supports standard HTTP methods (GET, POST, PUT, DELETE, PATCH)

For more advanced OpenAPI features with full schema generation, consider using `Cratis.Arc.Swagger` with ASP.NET Core.

## See Also

- [Getting Started](getting-started.md) - Learn how to build Arc.Core applications
- [Authentication](authentication.md) - Implement authentication for your API
- [Authorization](authorization.md) - Secure endpoints with authorization
