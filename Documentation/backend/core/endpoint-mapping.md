# Endpoint Mapping

Arc.Core provides extension methods for manually mapping HTTP endpoints to your application. This gives you full control over route patterns, handlers, and endpoint metadata.

## Overview

While Arc automatically maps commands and queries to endpoints, you may need to create custom endpoints for specific scenarios such as:

- Health checks
- Webhooks
- Custom API endpoints
- Static file serving
- Proxy endpoints

The `MapGet()` and `MapPost()` extension methods allow you to define these endpoints fluently.

## Basic Usage

### MapGet

Map a GET endpoint to handle HTTP GET requests:

```csharp
using Cratis.Arc;
using Cratis.Arc.Http;

var builder = ArcApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", async context =>
{
    await context.WriteAsync("OK");
});

await app.RunAsync();
```

### MapPost

Map a POST endpoint to handle HTTP POST requests:

```csharp
app.MapPost("/webhook", async context =>
{
    var data = await context.ReadBodyAsJsonAsync(typeof(object));
    // Process webhook data
    context.SetStatusCode(200);
});
```

## Method Signatures

Both methods follow the same pattern:

```csharp
ArcApplication MapGet(
    string pattern,
    Func<IHttpRequestContext, Task> handler,
    EndpointMetadata? metadata = null)

ArcApplication MapPost(
    string pattern,
    Func<IHttpRequestContext, Task> handler,
    EndpointMetadata? metadata = null)
```

### Parameters

- **pattern** - The route pattern (e.g., `/api/users`, `/health`)
- **handler** - An async function that processes the HTTP request
- **metadata** - Optional endpoint metadata for documentation and configuration

## Working with IHttpRequestContext

The handler function receives an `IHttpRequestContext` that provides access to the request and response:

### Reading Request Data

```csharp
app.MapPost("/api/data", async context =>
{
    // Query parameters
    var id = context.Query["id"];
    
    // Headers
    var authToken = context.Headers["Authorization"];
    
    // Cookies
    var sessionId = context.Cookies["SessionId"];
    
    // Request body as JSON
    var data = await context.ReadBodyAsJsonAsync(typeof(MyData));
    
    // Path and method
    var path = context.Path;
    var method = context.Method;
});
```

### Writing Response Data

```csharp
app.MapGet("/api/users", async context =>
{
    var users = new[] { new { Id = 1, Name = "Alice" } };
    
    // Set status code
    context.SetStatusCode(200);
    
    // Set content type
    context.ContentType = "application/json";
    
    // Write JSON response
    await context.WriteResponseAsJsonAsync(users, users.GetType());
    
    // Or write plain text
    // await context.WriteAsync("Hello World");
});
```

### Setting Response Headers

```csharp
app.MapGet("/api/data", async context =>
{
    context.SetResponseHeader("Cache-Control", "no-cache");
    context.SetResponseHeader("X-Custom-Header", "value");
    
    await context.WriteAsync("Data");
});
```

## Endpoint Metadata

Add metadata to provide documentation and configure endpoint behavior:

```csharp
using Cratis.Arc.Http;

app.MapGet("/api/users", async context =>
{
    // Handler implementation
},
new EndpointMetadata(
    Name: "GetAllUsers",
    Summary: "Retrieves a list of all users",
    Tags: ["Users"],
    AllowAnonymous: false));
```

### Metadata Properties

- **Name** - Unique identifier for the endpoint (used as operationId in OpenAPI)
- **Summary** - Human-readable description of what the endpoint does
- **Tags** - Categories for grouping related endpoints
- **AllowAnonymous** - Whether authentication is required (`false` = authentication required)

## Fluent API and Method Chaining

The extension methods return the `ArcApplication` instance, enabling fluent chaining:

```csharp
app.MapGet("/health", async context =>
    {
        await context.WriteAsync("OK");
    },
    new EndpointMetadata(Name: "Health", AllowAnonymous: true))
   .MapGet("/version", async context =>
    {
        await context.WriteAsync("1.0.0");
    },
    new EndpointMetadata(Name: "Version", AllowAnonymous: true))
   .MapPost("/api/events", async context =>
    {
        var evt = await context.ReadBodyAsJsonAsync(typeof(object));
        context.SetStatusCode(202);
    },
    new EndpointMetadata(Name: "ReceiveEvent", AllowAnonymous: false));

await app.RunAsync();
```

## Complete Examples

### Health Check Endpoint

```csharp
app.MapGet("/health", async context =>
{
    var health = new
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0"
    };
    
    context.ContentType = "application/json";
    await context.WriteResponseAsJsonAsync(health, health.GetType());
},
new EndpointMetadata(
    Name: "HealthCheck",
    Summary: "Returns the health status of the application",
    Tags: ["System"],
    AllowAnonymous: true));
```

### Webhook Handler

```csharp
app.MapPost("/webhooks/github", async context =>
{
    // Verify webhook signature
    var signature = context.Headers["X-Hub-Signature-256"];
    
    // Read webhook payload
    var payload = await context.ReadBodyAsJsonAsync(typeof(object));
    
    // Process webhook
    // ... your logic here ...
    
    context.SetStatusCode(200);
    await context.WriteAsync("Webhook received");
},
new EndpointMetadata(
    Name: "GitHubWebhook",
    Summary: "Receives GitHub webhook notifications",
    Tags: ["Webhooks"],
    AllowAnonymous: true));
```

### RESTful API Endpoint

```csharp
public record Product(int Id, string Name, decimal Price);

app.MapGet("/api/products", async context =>
{
    var products = new[]
    {
        new Product(1, "Product A", 29.99m),
        new Product(2, "Product B", 39.99m)
    };
    
    await context.WriteResponseAsJsonAsync(products, products.GetType());
},
new EndpointMetadata(
    Name: "ListProducts",
    Summary: "Get all available products",
    Tags: ["Products"],
    AllowAnonymous: true));

app.MapPost("/api/products", async context =>
{
    var product = await context.ReadBodyAsJsonAsync(typeof(Product)) as Product;
    
    if (product == null)
    {
        context.SetStatusCode(400);
        await context.WriteAsync("Invalid product data");
        return;
    }
    
    // Save product logic here
    
    context.SetStatusCode(201);
    context.SetResponseHeader("Location", $"/api/products/{product.Id}");
    await context.WriteResponseAsJsonAsync(product, typeof(Product));
},
new EndpointMetadata(
    Name: "CreateProduct",
    Summary: "Create a new product",
    Tags: ["Products"],
    AllowAnonymous: false));
```

### Custom Error Handler

```csharp
app.MapGet("/api/data/{id}", async context =>
{
    var id = context.Query["id"];
    
    if (string.IsNullOrEmpty(id))
    {
        context.SetStatusCode(400);
        var error = new { Error = "ID parameter is required" };
        await context.WriteResponseAsJsonAsync(error, error.GetType());
        return;
    }
    
    // Fetch data logic
    var data = FetchData(id);
    
    if (data == null)
    {
        context.SetStatusCode(404);
        var error = new { Error = $"Data with ID {id} not found" };
        await context.WriteResponseAsJsonAsync(error, error.GetType());
        return;
    }
    
    await context.WriteResponseAsJsonAsync(data, data.GetType());
});
```

## Authentication and Authorization

Control access to endpoints using the `AllowAnonymous` metadata property:

### Public Endpoint

```csharp
app.MapGet("/public/info", async context =>
{
    await context.WriteAsync("Public information");
},
new EndpointMetadata(
    Name: "PublicInfo",
    AllowAnonymous: true));  // No authentication required
```

### Protected Endpoint

```csharp
app.MapGet("/private/data", async context =>
{
    // Only accessible to authenticated users
    var user = context.User;
    await context.WriteAsync($"Hello, {user.Identity?.Name}");
},
new EndpointMetadata(
    Name: "PrivateData",
    AllowAnonymous: false));  // Authentication required
```

For more details on authentication, see [Authentication](authentication.md).

## Integration with OpenAPI

Endpoints mapped with `MapGet()` and `MapPost()` are automatically included in the OpenAPI specification when using the OpenAPI extensions:

```csharp
using Cratis.Arc;
using Cratis.Arc.OpenApi;

var builder = ArcApplication.CreateBuilder(args);
var app = builder.Build();

// Map custom endpoints
app.MapGet("/api/status", async context =>
    {
        await context.WriteAsync("Running");
    },
    new EndpointMetadata(
        Name: "GetStatus",
        Summary: "Get application status",
        Tags: ["System"]))
   .MapOpenApi();  // Generate OpenAPI document

await app.RunAsync();
```

The OpenAPI document will include your custom endpoints with the metadata you provided. See [OpenAPI Specifications](openapi.md) for more details.

## Best Practices

### Use Descriptive Route Patterns

```csharp
// Good
app.MapGet("/api/users/{id}");
app.MapPost("/api/orders");

// Avoid
app.MapGet("/u/{i}");
app.MapPost("/data");
```

### Provide Endpoint Metadata

Always include metadata for documentation and tooling:

```csharp
app.MapGet("/api/resource", handler,
    new EndpointMetadata(
        Name: "GetResource",
        Summary: "Clear description",
        Tags: ["ResourceCategory"],
        AllowAnonymous: false));
```

### Handle Errors Gracefully

```csharp
app.MapPost("/api/data", async context =>
{
    try
    {
        var data = await context.ReadBodyAsJsonAsync(typeof(MyData));
        // Process data
    }
    catch (Exception ex)
    {
        context.SetStatusCode(500);
        var error = new { Error = "Internal server error" };
        await context.WriteResponseAsJsonAsync(error, error.GetType());
    }
});
```

### Set Appropriate Status Codes

```csharp
context.SetStatusCode(200);  // OK
context.SetStatusCode(201);  // Created
context.SetStatusCode(400);  // Bad Request
context.SetStatusCode(401);  // Unauthorized
context.SetStatusCode(404);  // Not Found
context.SetStatusCode(500);  // Internal Server Error
```

### Use Dependency Injection

Access services through the request context:

```csharp
app.MapGet("/api/users", async context =>
{
    var userService = context.RequestServices.GetRequiredService<IUserService>();
    var users = await userService.GetAllAsync();
    await context.WriteResponseAsJsonAsync(users, users.GetType());
});
```

## Limitations

The current endpoint mapping implementation:

- Supports GET and POST methods only
- Does not support route parameters in the pattern (e.g., `/users/{id}`)
- Does not support PUT, DELETE, or PATCH methods

For full HTTP method support and advanced routing, consider using ASP.NET Core with Arc.

## See Also

- [Getting Started](getting-started.md) - Learn the basics of Arc.Core
- [OpenAPI Specifications](openapi.md) - Generate API documentation
- [Authentication](authentication.md) - Secure your endpoints
- [Authorization](authorization.md) - Control access to resources
