# ASP.NET Core Integration

The Arc provides enhanced capabilities for ASP.NET Core applications, building upon the core Arc features with web-specific functionality. This integration offers powerful tools for API development including advanced model binding, validation, authorization, and automatic API documentation.

## Features

- **[Configuration](configuration.md)** - Configure Arc through appsettings.json or programmatically
- **[Authorization](authorization.md)** - Enhanced authorization with role-based access and policy support
- **[Microsoft Identity](microsoft-identity.md)** - Integration with Microsoft Client Principal for Azure services
- **[FromRequest Attribute](from-request.md)** - Advanced model binding combining multiple HTTP request sources
- **[Swagger](swagger.md)** - Enhanced OpenAPI documentation with Arc-specific schema generation
- **[Validation](validation.md)** - Comprehensive validation with FluentValidation support
- **[Without Wrappers](without-wrappers.md)** - Control response wrapping behavior for specific endpoints

## When to Use ASP.NET Core Integration

Use the ASP.NET Core integration when you need:

- Full web framework capabilities (Kestrel, middleware pipeline, static files)
- Razor views or MVC features
- Swagger UI for API documentation
- Advanced middleware scenarios
- Maximum HTTP throughput with Kestrel
- Traditional web application patterns

## When to Use Arc.Core Instead

Consider using [Arc.Core](../core/overview.md) (without ASP.NET Core) when you need:

- Minimal dependencies and smaller binary size
- Faster startup times
- Lower memory footprint
- Native AOT compilation support
- Console applications or background services
- Scenarios where full web framework is unnecessary

## See Also

- [Arc.Core Overview](../core/overview.md) - Lightweight alternative without ASP.NET Core
- [Commands](../commands/index.md) - Command handling patterns
- [Queries](../queries/index.md) - Query patterns and conventions
- [Tenancy](../tenancy/index.md) - Tenant isolation and management
