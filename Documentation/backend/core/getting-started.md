# Getting Started

This guide walks you through building your first Arc.Core application from scratch. You'll learn how to set up the application, define commands and queries, and run your service.

## Prerequisites

- .NET 9 SDK or later
- Basic understanding of C# and .NET concepts

## Installation

Add the Arc.Core package to your project:

```bash
dotnet add package Cratis.Applications
```

## Basic Setup

Create a new console application and configure Arc.Core:

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

The `ArcApplicationBuilder` provides a familiar builder pattern for configuring your application:

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
    "http://+:5001/"
);

// Default (http://localhost:5000/index.md)
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

## Working with Commands

Commands represent actions or operations in your application. They're automatically exposed as HTTP POST endpoints.

### Defining a Command

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

### Accessing Command Endpoints

Commands are exposed as POST endpoints:

```bash
curl -X POST http://localhost:5000/api/your-app/create-user \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john@example.com"}'
```

## Working with Queries

Queries represent data retrieval operations. They're automatically exposed as HTTP GET endpoints.

### Defining a Query

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

### Accessing Query Endpoints

Queries are exposed as GET endpoints:

```bash
curl http://localhost:5000/api/your-app/get-user?id=123e4567-e89b-12d3-a456-426614174000
```

## Configuration

### Using appsettings.json

Arc.Core supports standard .NET configuration:

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

Arc supports automatic service registration using attributes:

```csharp
// Services with [Singleton], [Scoped], or [Transient] attributes
// are automatically registered
[Singleton]
public class MyService : IMyService
{
    // Implementation
}
```

## Complete Example

Here's a complete working example that demonstrates commands, queries, and services:

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

## Advanced Scenarios

### Background Services

Combine Arc.Core with `IHostedService` for background processing:

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
3. Use `http://+:5001/` instead of `http://localhost:5001/` to listen on all interfaces

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

Now that you have a basic Arc.Core application running, explore these topics:

- [Authentication](authentication.md) - Implement custom authentication handlers
- [Authorization](authorization.md) - Protect your endpoints with authorization attributes
- [Commands](../commands/index.md) - Learn about advanced command patterns
- [Queries](../queries/index.md) - Discover query features like filtering and pagination
- [Identity](../identity.md) - Integrate the identity system
- [Tenancy](../tenancy/overview.md) - Configure multi-tenant applications
- [Validation](../commands/validation.md) - Add validation to commands and queries
