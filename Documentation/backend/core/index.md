# Arc.Core

Arc.Core provides a lightweight application model for building .NET applications without requiring ASP.NET Core. This is ideal for console applications, background services, microservices, and scenarios where you want minimal dependencies and maximum performance.

## Why Arc.Core?

Arc.Core is designed for developers who want the power of Arc's developer experience—commands, queries, identity, multi-tenancy, and validation—without the overhead of the full ASP.NET Core stack. It's perfect for:

- Console applications
- Background services and workers
- Lightweight microservices
- Custom HTTP listeners
- gRPC services
- Scenarios requiring fast startup and low memory footprint
- Native AOT (Ahead-of-Time) compilation scenarios

## Key Features

- **Minimal Dependencies** - No web server dependencies (Kestrel, HTTP.sys), no MVC/Razor dependencies
- **Native AOT Ready** - Designed for Native AOT compilation with smaller binaries and faster startup
- **Full Arc Features** - Commands, queries, identity, multi-tenancy, validation, and more
- **Static File Serving** - Serve static assets and host Single Page Applications
- **Flexible** - Use with any .NET application type
- **Performance** - Faster startup times and lower memory consumption

## Topics

- [Overview](overview.md) - Learn about the motivation and design philosophy
- [Getting Started](getting-started.md) - Build your first Arc.Core application
- [Endpoint Mapping](endpoint-mapping.md) - Map custom HTTP endpoints with MapGet and MapPost
- [Static Files](static-files.md) - Serve static files and host SPAs
- [Authentication](authentication.md) - Implement custom authentication handlers
- [Authorization](authorization.md) - Protect your endpoints with authorization attributes
- [OpenAPI Specifications](openapi.md) - Generate OpenAPI documentation for your API

## Next Steps

Ready to get started? Head over to the [Getting Started](getting-started.md) guide to build your first Arc.Core application.
