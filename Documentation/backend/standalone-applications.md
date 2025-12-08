# Standalone Applications (Without ASP.NET Core)

Arc.Core provides a lightweight application model for building .NET applications without requiring ASP.NET Core. This is ideal for console applications, background services, microservices, and scenarios where you want minimal dependencies and maximum performance.

## Benefits

### Minimal Dependencies

Arc.Core has significantly fewer dependencies compared to the full ASP.NET Core stack:

- No web server dependencies (Kestrel, HTTP.sys)
- No MVC/Razor dependencies
- Smaller deployment footprint
- Faster startup time
- Reduced memory consumption

### Native AOT Ready

Arc.Core is designed with Native AOT (Ahead-of-Time) compilation in mind:

- Compatible with .NET's Native AOT compilation
- Smaller binary sizes (single-file executables)
- Faster startup times (no JIT compilation at runtime)
- Lower memory footprint
- Ideal for containerized workloads and serverless scenarios

> **Note**: While Arc.Core is designed to support AOT, full AOT compatibility depends on the features and libraries you use in your application. Always test your specific scenario.

### Flexibility

- Use with console applications
- Background services and workers
- Custom HTTP listeners
- gRPC services
- Any .NET application that needs commands, queries, and identity

### Full Arc Features

Despite being lightweight, you get access to all core Arc features:

- Commands with automatic endpoint generation
- Queries with filtering, sorting, and pagination
- Identity system
- Multi-tenancy support
- Correlation ID tracking
- Validation
- Type discovery and conventions
- Dependency injection

## Getting Started

### Installation

Add the Arc.Core package to your project:

```bash
dotnet add package Cratis.Applications
```

### Basic Setup

Create a simple Arc application using `ArcApplicationBuilder`:

```csharp
using Cratis.Arc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = ArcApplicationBuilder.CreateBuilder(args);

// Add Arc services
builder.AddCratisArc();

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Build and run the application
var app = builder.Build();

// Start the HTTP listener on specified prefixes
app.UseCratisArc("http://localhost:5000/");

Console.WriteLine("Application started on http://localhost:5000/");
Console.WriteLine("Press Ctrl+C to stop...");

await app.RunAsync();
```

## ArcApplicationBuilder API

### Creating a Builder

```csharp
// Create with command-line arguments
var builder = ArcApplicationBuilder.CreateBuilder(args);

// Or without arguments
var builder = ArcApplicationBuilder.CreateBuilder();
```

### Available Properties

The builder exposes several properties for configuration:

```csharp
// Configuration system
IConfigurationManager Configuration = builder.Configuration;

// Host environment information
IHostEnvironment Environment = builder.Environment;

// Logging configuration
ILoggingBuilder Logging = builder.Logging;

// Service collection for dependency injection
IServiceCollection Services = builder.Services;

// Metrics configuration
IMetricsBuilder Metrics = builder.Metrics;
```

### Adding Arc Services

```csharp
builder.AddCratisArc(arcBuilder =>
{
    // Configure Arc-specific options
    // Add extensions like Chronicle, MongoDB, etc.
});
```

## ArcApplication API

### Starting the Application

The `UseCratisArc` method starts the built-in HTTP listener:

```csharp
// Single prefix
app.UseCratisArc("http://localhost:5000/");

// Multiple prefixes
app.UseCratisArc(
    "http://localhost:5000/",
    "http://*:5001/"
);

// Default (http://localhost:5000/)
app.UseCratisArc();
```

### Running the Application

```csharp
// Run and block until shutdown (Ctrl+C)
await app.RunAsync();

// Or control start/stop manually
await app.StartAsync();
// ... do work ...
await app.StopAsync();
```

## Configuration

### Using appsettings.json

Arc.Core supports standard .NET configuration:

**appsettings.json**:

```json
{
  "Cratis": {
    "Arc": {
      "GeneratedApis": {
        "RoutePrefix": "api"
      },
      "CorrelationId": {
        "HttpHeader": "X-Correlation-ID"
      },
      "Tenancy": {
        "HttpHeader": "X-Tenant-ID"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Environment-Specific Configuration

Use environment-specific files following .NET conventions:

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides

```csharp
var builder = ArcApplicationBuilder.CreateBuilder(args);

// Configuration is automatically loaded based on environment
var environment = builder.Environment.EnvironmentName;
Console.WriteLine($"Running in {environment} environment");
```

### Custom Configuration Section

Specify a custom configuration section path:

```csharp
builder.AddCratisArc(
    configSectionPath: "MyApp:ArcSettings"
);
```

## Working with Commands and Queries

### Defining Commands

Commands work exactly as they do with ASP.NET Core:

```csharp
using Cratis.Arc.Commands;

public record CreateUser(string Name, string Email) : ICommand;

public class CreateUserHandler(ILogger<CreateUserHandler> logger) : ICommandHandler<CreateUser>
{
    public Task<CommandResult> Handle(CreateUser command, CommandContext context)
    {
        logger.LogInformation("Creating user: {Name}", command.Name);
        
        // Your business logic here
        
        return Task.FromResult(CommandResult.Success);
    }
}
```

### Defining Queries

Queries also work the same way:

```csharp
using Cratis.Arc.Queries;

public record GetUser(Guid Id) : IQuery<UserDto>;

public class GetUserHandler : IQueryHandler<GetUser, UserDto>
{
    public Task<UserDto> Handle(GetUser query, QueryContext context)
    {
        // Your query logic here
        return Task.FromResult(new UserDto(query.Id, "John Doe"));
    }
}

public record UserDto(Guid Id, string Name);
```

### Accessing Endpoints

Commands and queries are automatically exposed as HTTP endpoints:

```bash
# POST command
curl -X POST http://localhost:5000/api/your-microservice/create-user \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john@example.com"}'

# GET query
curl http://localhost:5000/api/your-microservice/get-user?id=123e4567-e89b-12d3-a456-426614174000
```

## Adding Services

### Logging

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});
```

### Custom Services

```csharp
// Singleton
builder.Services.AddSingleton<IMyService, MyService>();

// Scoped (per request)
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Transient (per injection)
builder.Services.AddTransient<IEmailSender, EmailSender>();
```

### Using Arc Conventions

Arc supports automatic service registration:

```csharp
// Services with [Singleton], [Scoped], or [Transient] attributes
// are automatically registered
[Singleton]
public class MyService : IMyService
{
    // Implementation
}
```

## Advanced Scenarios

### Background Services

Combine with `IHostedService` for background processing:

```csharp
public class BackgroundWorker(ILogger<BackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background worker started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            // Do background work
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        
        logger.LogInformation("Background worker stopped");
    }
}

// Register it
builder.Services.AddHostedService<BackgroundWorker>();
```

### Integrating with Chronicle

Add event sourcing capabilities:

```csharp
builder.AddCratisArc(arcBuilder =>
{
    arcBuilder.WithChronicle();
});
```

### Integrating with MongoDB

Add MongoDB support:

```csharp
builder.AddCratisArc(arcBuilder =>
{
    arcBuilder.WithMongoDB();
});
```

### Custom Identity Provider

Implement your own identity provider:

```csharp
using Cratis.Arc.Identity;

public class CustomIdentityDetailsProvider : IIdentityDetailsProvider
{
    public Task<IdentityDetails> GetIdentityDetails()
    {
        // Your custom logic to retrieve identity
        return Task.FromResult(new IdentityDetails(
            UserId: "user-123",
            UserName: "John Doe"
        ));
    }
}

// Register it
builder.Services.AddSingleton<IIdentityDetailsProvider, CustomIdentityDetailsProvider>();
```

## Complete Example

Here's a complete working example:

```csharp
using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = ArcApplicationBuilder.CreateBuilder(args);

builder.AddCratisArc();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

app.UseCratisArc("http://localhost:5000/");

Console.WriteLine("Application started!");
Console.WriteLine("Available endpoints:");
Console.WriteLine("  POST   http://localhost:5000/api/my-app/greet");
Console.WriteLine("  GET    http://localhost:5000/api/my-app/get-greeting?name=World");
Console.WriteLine("  GET    http://localhost:5000/.cratis/me");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop...");

await app.RunAsync();

// Commands
public record Greet(string Name) : ICommand;

public class GreetHandler(ILogger<GreetHandler> logger) : ICommandHandler<Greet>
{
    public Task<CommandResult> Handle(Greet command, CommandContext context)
    {
        logger.LogInformation("Greeting {Name}", command.Name);
        return Task.FromResult(CommandResult.Success);
    }
}

// Queries
public record GetGreeting(string Name) : IQuery<string>;

public class GetGreetingHandler : IQueryHandler<GetGreeting, string>
{
    public Task<string> Handle(GetGreeting query, QueryContext context)
    {
        return Task.FromResult($"Hello, {query.Name}!");
    }
}
```

## Comparison with ASP.NET Core

| Feature | Arc.Core (Standalone) | Arc with ASP.NET Core |
|---------|----------------------|----------------------|
| Dependencies | Minimal | Full ASP.NET Core stack |
| Startup Time | Faster | Slower |
| Memory Usage | Lower | Higher |
| Binary Size | Smaller | Larger |
| AOT Support | Designed for AOT | Limited AOT support |
| HTTP Server | Built-in HttpListener | Kestrel or HTTP.sys |
| Middleware Pipeline | Basic | Full ASP.NET Core pipeline |
| Static Files | No | Yes |
| Razor Views | No | Yes |
| MVC Controllers | No | Yes (optional) |
| Swagger UI | No | Yes (via extension) |
| Commands & Queries | ✅ Yes | ✅ Yes |
| Identity System | ✅ Yes | ✅ Yes |
| Multi-Tenancy | ✅ Yes | ✅ Yes |
| Validation | ✅ Yes | ✅ Yes |

## When to Use Arc.Core

Choose Arc.Core for standalone applications when:

- You don't need a full web framework
- You want minimal dependencies and smaller deployments
- You're building console applications or background services
- You need fast startup times
- You want to explore Native AOT compilation
- You're building microservices with minimal overhead
- You want the Arc developer experience without ASP.NET Core

Choose Arc with ASP.NET Core when:

- You need the full ASP.NET Core middleware pipeline
- You need to serve static files or Razor views
- You want Swagger UI for API documentation
- You need advanced HTTP features from Kestrel
- You're building traditional web applications

## Performance Considerations

### Startup Time

Arc.Core applications typically start 2-3x faster than equivalent ASP.NET Core applications due to fewer dependencies and simpler initialization.

### Memory Footprint

Expect 30-50% lower memory usage compared to ASP.NET Core, making it ideal for containerized environments and high-density deployments.

### Throughput

While the built-in `HttpListener` is suitable for most scenarios, Kestrel (ASP.NET Core) offers higher throughput for high-traffic scenarios. Choose based on your performance requirements.

## Troubleshooting

### Endpoints Not Found

Ensure you've called `app.UseCratisArc()` before `app.RunAsync()`:

```csharp
var app = builder.Build();
app.UseCratisArc("http://localhost:5000/");  // Must be called!
await app.RunAsync();
```

### HTTP Listener Errors

If you get HTTP listener errors, ensure:

1. The port is not already in use
2. You have permissions to bind to the port (on Windows, non-admin users can't bind to port 80)
3. Use `http://*:5001/` instead of `http://localhost:5001/` to listen on all interfaces

### Configuration Not Loading

Ensure `appsettings.json` is copied to output:

```xml
<ItemGroup>
  <None Update="appsettings*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Next Steps

- Explore [Commands](./commands/) for advanced command patterns
- Learn about [Queries](./queries/) for data retrieval
- Understand [Identity](./identity.md) system integration
- Configure [Multi-Tenancy](./multi-tenancy.md) support
- Add [Validation](./validation.md) to your commands and queries
