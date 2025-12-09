# Overview

Arc.Core is a lightweight application framework that brings the Arc developer experience to .NET applications without requiring ASP.NET Core. It's designed for scenarios where you need the power of Arc's conventions—commands, queries, identity, multi-tenancy, and validation—but don't need the full web framework.

## Motivation

Modern .NET development often requires building various types of applications beyond traditional web applications:

- **Console Applications** - CLI tools, utilities, and batch processors
- **Background Services** - Long-running workers and scheduled tasks
- **Microservices** - Lightweight services with minimal overhead
- **Custom Servers** - gRPC services, custom protocols, or specialized HTTP endpoints
- **Containerized Workloads** - Applications optimized for containers and serverless environments

For these scenarios, the full ASP.NET Core stack can be overkill, bringing unnecessary dependencies, slower startup times, and increased memory consumption. Arc.Core addresses this by providing a minimal foundation that preserves the Arc experience while removing web framework overhead.

## Design Philosophy

### Minimal Dependencies

Arc.Core intentionally excludes the full ASP.NET Core stack:

- **No Kestrel or HTTP.sys** - Uses .NET's built-in `HttpListener` for HTTP scenarios
- **No MVC/Razor** - No view rendering or controller infrastructure
- **No Middleware Pipeline** - Simplified request handling
- **Smaller Deployment Footprint** - Fewer assemblies to deploy

This results in:
- Faster startup times
- Lower memory consumption
- Smaller binary sizes
- Reduced attack surface

### Native AOT Ready

Arc.Core is designed with Native AOT (Ahead-of-Time) compilation in mind:

- **Faster Startup** - No JIT compilation at runtime
- **Smaller Binaries** - Single-file executables with tree-shaking
- **Lower Memory Footprint** - Reduced working set
- **Predictable Performance** - No JIT warmup time

> **Note**: While Arc.Core is designed to support AOT, full AOT compatibility depends on the features and libraries you use in your application. Always test your specific scenario.

### Full Arc Features

Despite being lightweight, Arc.Core provides all core Arc capabilities:

- **Commands** - Automatic endpoint generation and handling
- **Queries** - Filtering, sorting, and pagination support
- **Identity System** - User authentication and authorization
- **Multi-Tenancy** - Tenant isolation and context management
- **Correlation ID Tracking** - Request tracing across services
- **Validation** - Declarative validation with automatic error handling
- **Type Discovery** - Convention-based type discovery
- **Dependency Injection** - Full DI container support

### Flexibility

Arc.Core can be used in various scenarios:

- **Standalone HTTP Services** - Build HTTP APIs without ASP.NET Core
- **Console Applications** - Add commands and queries to CLI tools
- **Background Workers** - Combine with `IHostedService` for background processing
- **gRPC Services** - Use Arc features alongside gRPC
- **Custom Protocols** - Build any type of .NET application with Arc conventions

## What It's For

### Primary Use Cases

Arc.Core is ideal for:

1. **Lightweight Microservices**
   - Services that don't need the full web stack
   - Container-optimized deployments
   - Fast startup requirements
   - Low memory constraints

2. **Console Applications**
   - CLI tools that expose HTTP endpoints for management
   - Batch processing with API integration
   - Developer tools and utilities

3. **Background Services**
   - Long-running workers with HTTP endpoints for health checks
   - Scheduled tasks with monitoring APIs
   - Message processors with control endpoints

4. **Native AOT Scenarios**
   - Applications requiring fast cold starts
   - Single-file deployments
   - Environments with strict size constraints

5. **Learning and Prototyping**
   - Simpler setup for learning Arc concepts
   - Rapid prototyping without web framework complexity
   - Testing Arc patterns in isolation

### When Not to Use

Arc.Core is **not** suitable when you need:

- **Static File Serving** - Use ASP.NET Core's static file middleware
- **Razor Views** - Use ASP.NET Core MVC
- **Advanced Middleware** - Use ASP.NET Core's full middleware pipeline
- **Swagger UI** - Use Arc with ASP.NET Core and Swagger extension
- **High-Traffic Scenarios** - Consider ASP.NET Core with Kestrel for maximum throughput

## Architecture

Arc.Core is built around the `ArcApplicationBuilder` and `ArcApplication` abstractions, which mirror .NET's `HostBuilder` pattern:

```csharp
// Builder Pattern
var builder = ArcApplicationBuilder.CreateBuilder(args);
builder.AddCratisArc();
// Configure services, logging, metrics, etc.

// Application Pattern
var app = builder.Build();
app.UseCratisArc("http://localhost:5000/");
await app.RunAsync();
```

This familiar pattern makes it easy to transition between Arc.Core and ASP.NET Core-based Arc applications.

## Comparison with ASP.NET Core

| Aspect | Arc.Core | Arc with ASP.NET Core |
|--------|----------|----------------------|
| **Dependencies** | Minimal | Full ASP.NET Core stack |
| **Startup Time** | Faster | Standard |
| **Memory Usage** | Lower | Higher |
| **Binary Size** | Smaller | Larger |
| **AOT Support** | Designed for AOT | Limited AOT support |
| **HTTP Server** | HttpListener | Kestrel/HTTP.sys |
| **Middleware** | Basic | Full pipeline |
| **Static Files** | ❌ No | ✅ Yes |
| **Razor Views** | ❌ No | ✅ Yes |
| **Swagger UI** | ❌ No | ✅ Yes |
| **Commands** | ✅ Yes | ✅ Yes |
| **Queries** | ✅ Yes | ✅ Yes |
| **Identity** | ✅ Yes | ✅ Yes |
| **Multi-Tenancy** | ✅ Yes | ✅ Yes |
| **Validation** | ✅ Yes | ✅ Yes |

## Next Steps

Ready to build your first Arc.Core application? Head over to the [Getting Started](getting-started.md) guide.

To learn about specific features:
- [Authentication](authentication.md) - Implement custom authentication handlers
- [Authorization](authorization.md) - Protect your endpoints with authorization attributes
