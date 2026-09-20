# ASP.NET Core Integration

The Arc provides enhanced capabilities for ASP.NET Core applications, building upon the core Arc features with web-specific functionality. Install `Cratis.Arc` for this host (`Cratis.Arc.Core` alone is the lightweight alternative). Register with `WebApplicationBuilder.AddCratisArc()` and activate with `WebApplication.UseCratisArc()`. No event store is required. OpenAPI/Swagger have separate optional packages.

## Features

- **[Configuration](configuration.md)** - Configure Arc through appsettings.json or programmatically
- **[Authorization](authorization.md)** - Arc role checks versus separately configured ASP.NET endpoint policies
- **[Microsoft Identity](microsoft-identity.md)** - Integration with Microsoft Client Principal for Azure services
- **[FromRequest Attribute](from-request.md)** - Advanced model binding combining multiple HTTP request sources
- **[Swagger](swagger.md)** - Enhanced OpenAPI documentation with Arc-specific schema generation
- **[Validation](validation.md)** - Comprehensive validation with FluentValidation support
- **[Without Wrappers](without-wrappers.md)** - Control response wrapping behavior for specific endpoints
- **[Invariant Culture](invariant-culture.md)** - Configure invariant culture defaults and understand request-level overrides

## When to Use ASP.NET Core Integration

Use the ASP.NET Core integration when you need:

- Full web framework capabilities (Kestrel, middleware pipeline, static files)
- Razor views or MVC features
- Swagger UI for API documentation
- Advanced middleware scenarios
- Kestrel's HTTP server capabilities
- Traditional web application patterns

## When to Use Arc.Core Instead

Consider using [Arc.Core](../core/overview.md) (without ASP.NET Core) when you need:

- Minimal dependencies and smaller binary size
- A simplified `HttpListener` host rather than the ASP.NET middleware ecosystem
- A deployment whose performance and Native AOT compatibility you verify with your actual dependencies
- Console applications or background services
- Scenarios where full web framework is unnecessary

## See Also

- [Arc.Core Overview](../core/overview.md) - Lightweight alternative without ASP.NET Core
- [Commands](../commands/index.md) - Command handling patterns
- [Queries](../queries/index.md) - Query patterns and conventions
- [Tenancy](../tenancy/index.md) - Tenant isolation and management
