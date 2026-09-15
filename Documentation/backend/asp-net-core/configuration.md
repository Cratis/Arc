# Configuration

Cratis Arc can be configured both through `appsettings.json` and programmatically to customize its behavior. The main configuration is handled through the `ArcOptions` class.

This page covers the ASP.NET Core host specifically. Code blocks are configuration/service fragments for an existing `Cratis.Arc` web project; import `Cratis.Arc`, `System.Text.Json`, and `Microsoft.Extensions.Options` as used. Types such as `MyCustomIdentityDetailsProvider` and `MyCustomConverter` are application-owned examples, not built-in Arc types. For the lightweight Arc.Core host and its listen URL configuration, see [Arc.Core Getting Started](../core/getting-started.md).

## Default Configuration Section

By default, Arc looks for configuration under the `Cratis:Arc` section in your `appsettings.json` file. The same keys can be supplied through environment variables — .NET maps the `__` separator onto nested keys, so `Cratis:Arc:GeneratedApis:RoutePrefix` becomes `Cratis__Arc__GeneratedApis__RoutePrefix`.

## Configuration Options

### Configuration example

Here's an example covering the most common options, bound from `appsettings.json` under `Cratis:Arc`. The [complete options tree](../configuration/index.md#the-arcoptions-tree) also documents `ExposeExceptionDetails`, `Tenancy.BaseDomain`, and host-only settings.

```json
{
    "Cratis": {
        "Arc": {
            "CorrelationId": {
                "HttpHeader": "X-Correlation-ID"
            },
            "Tenancy": {
                "ResolverType": "Header",
                "HttpHeader": "x-cratis-tenant-id"
            },
            "GeneratedApis": {
                "RoutePrefix": "api",
                "SegmentsToSkipForRoute": 0,
                "IncludeCommandNameInRoute": true,
                "IncludeQueryNameInRoute": true
            },
            "Query": {
                "KeepAliveInterval": "00:00:30"
            }
        }
    }
}
```

> [!NOTE]
> `ArcOptions.Hosting` (the listen URL) applies only to **Arc.Core** hosts — in an ASP.NET Core app the URL comes from Kestrel and `launchSettings.json`, not from Arc. For the Arc.Core hosting shape, see [Arc.Core Getting Started](../core/getting-started.md).

### Configuration Properties

#### CorrelationId

Controls how correlation IDs are handled in HTTP requests.

- **HttpHeader** (string, default: `"X-Correlation-ID"`): The HTTP header name to use for correlation ID tracking.

#### Tenancy

Controls how the active tenant is resolved on each request.

- **ResolverType** (`TenantResolverType`, default: `Header`): How to resolve the tenant — `Header`, `Query`, `Claim`, `Subdomain`, `Development`, or `Fixed`.
- **HttpHeader** (string, default: `"x-cratis-tenant-id"`): The HTTP header used when `ResolverType` is `Header`, and the fallback header when it is `Subdomain`.
- **QueryParameter** (string, default: `"tenantId"`): The query-string parameter used when `ResolverType` is `Query`.
- **ClaimType** (string, default: `"tenant_id"`): The claim used when `ResolverType` is `Claim`.
- **FixedTenantId** (string, default: `"development"`): The tenant every request resolves to when `ResolverType` is `Fixed` or `Development`. Prefer `Fixed` for a single-tenant production deployment — `Development` resolves identically but is named for an environment it does not check.
- **DevelopmentTenantId** (string, default: `"development"`): The same value under its original name — reading or writing either key sets both. Supply only one; if both are present the binder's property order decides.

#### GeneratedApis

Controls how automatically generated API endpoints are configured for commands and queries.

- **RoutePrefix** (string, default: `"api"`): The base route prefix for all generated API endpoints.
- **SegmentsToSkipForRoute** (int, default: `0`): Number of namespace segments to skip when constructing routes from type namespaces.
- **IncludeCommandNameInRoute** (bool, default: `true`): Whether to include the command type name as the last segment of the route for command endpoints.
- **IncludeQueryNameInRoute** (bool, default: `true`): Whether to include the query name as the last segment of the route for query endpoints.
- **EnableQueryHttpMethod** (bool, default: `true`): Whether generated queries also accept HTTP `QUERY` with arguments in a JSON body. GET remains available. See [HTTP QUERY](../queries/using-the-http-query-method.md).

#### Query

Controls observable (real-time) queries.

- **KeepAliveInterval** (`TimeSpan`, default: `00:00:30`): How often a keep-alive frame is sent on an open observable-query connection.

#### IdentityDetailsProvider

- **IdentityDetailsProvider** (Type, default: `null`): Specifies a custom identity details provider type. If not specified, the system will use type discovery to find one automatically.

## Setup Methods

The ASP.NET Core host bootstraps with `WebApplication.CreateBuilder`, registers Arc with `AddCratisArc`, and activates it with `UseCratisArc`.

### Using Configuration File

The most common approach is to use the configuration file with the default section:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Bind the default configuration section (Cratis:Arc)
builder.AddCratisArc();

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

### Using Custom Configuration Section

You can specify a custom configuration section path:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Bind a custom configuration section
builder.AddCratisArc(configSectionPath: "MyApp:CratisConfig");

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

### Programmatic Configuration

You can configure the options through code with the `configureOptions` callback:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCratisArc(options =>
{
    // Configure correlation ID
    options.CorrelationId.HttpHeader = "X-My-Correlation-ID";

    // Configure tenancy
    options.Tenancy.HttpHeader = "X-Custom-Tenant";

    // Configure generated APIs
    options.GeneratedApis.RoutePrefix = "myapi";
    options.GeneratedApis.SegmentsToSkipForRoute = 2;
    options.GeneratedApis.IncludeCommandNameInRoute = false;
    options.GeneratedApis.IncludeQueryNameInRoute = false;

    // Set a custom identity details provider
    options.IdentityDetailsProvider = typeof(MyCustomIdentityDetailsProvider);
});

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

### Hybrid Configuration

`AddCratisArc` binds `appsettings.json` first and then applies the `configureOptions` callback, so the callback acts as a programmatic override on top of file-based settings:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Values come from Cratis:Arc; the callback overrides specific settings
builder.AddCratisArc(options =>
{
    options.GeneratedApis.RoutePrefix = "v1/api";
});

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

## Environment-Specific Configuration

You can use different configurations for different environments using the standard ASP.NET Core configuration pattern:

**appsettings.json** (base configuration):

```json
{
    "Cratis": {
        "Arc": {
            "GeneratedApis": {
                "RoutePrefix": "api"
            }
        }
    }
}
```

**appsettings.Development.json** (development overrides):

```json
{
    "Cratis": {
        "Arc": {
            "CorrelationId": {
                "HttpHeader": "X-Dev-Correlation-ID"
            }
        }
    }
}
```

**appsettings.Production.json** (production overrides):

```json
{
    "Cratis": {
        "Arc": {
            "GeneratedApis": {
                "RoutePrefix": "v1"
            }
        }
    }
}
```

## Route Generation Examples

The `GeneratedApis` configuration affects how routes are generated for your commands and queries. Here are some examples:

Given a command class `MyApp.Sales.Commands.CreateOrderCommand`:

### Default Configuration

```json
{
    "GeneratedApis": {
        "RoutePrefix": "api",
        "SegmentsToSkipForRoute": 0,
        "IncludeCommandNameInRoute": true,
        "IncludeQueryNameInRoute": true
    }
}
```

**Generated route**: `/api/my-app/sales/commands/create-order-command`

### Skip Namespace Segments

```json
{
    "GeneratedApis": {
        "RoutePrefix": "api",
        "SegmentsToSkipForRoute": 2,
        "IncludeCommandNameInRoute": true,
        "IncludeQueryNameInRoute": true
    }
}
```

**Generated route**: `/api/commands/create-order-command` (skips `MyApp` and `Sales`).

### Exclude Type Names

```json
{
    "GeneratedApis": {
        "RoutePrefix": "api",
        "SegmentsToSkipForRoute": 2,
        "IncludeCommandNameInRoute": false,
        "IncludeQueryNameInRoute": false
    }
}
```

**Generated route**: `/api/commands` for this single command. A single query with location `MyApp.Sales.Queries` would use `/api/queries` with these options.

:::note[Route conflicts restore the type name]
When `IncludeCommandNameInRoute` or `IncludeQueryNameInRoute` is set to `false`, Arc detects route conflicts. If multiple commands or queries remain in the same namespace after skipping segments, Arc includes the type name in the route. Within those generated command or query groups:

- Single command/query in a namespace: Route remains clean without the type name
- Multiple commands/queries in the same namespace: Type names are automatically added to prevent route collisions
- Both runtime endpoint mapping and proxy generation apply this logic consistently
:::

For example, with the configuration above:

- If you have only `MyApp.Sales.Commands.CreateOrder`, the route will be `/api/commands`
- If you have both `MyApp.Sales.Commands.CreateOrder` and `MyApp.Sales.Commands.UpdateOrder`, the routes will be `/api/commands/create-order` and `/api/commands/update-order` respectively (type names added automatically to avoid conflict)

> [!NOTE]
> The runtime `GeneratedApis` settings and the build-time proxy-generation settings must agree. If you change route generation here, mirror it in the `CratisProxies*` MSBuild properties so the generated TypeScript clients call the same routes. See [Proxy Generation Configuration](../proxy-generation/Configuration/index.md).

## JSON Serialization

Arc-generated endpoints use `ArcOptions.JsonSerializerOptions`. Arc's MVC post-configuration copies only the property naming policy and appends Arc converters; it does not copy the entire options object. Settings such as `DefaultIgnoreCondition`, `NumberHandling`, and other MVC serializer options require separate MVC configuration if controller output must match. Manual serialization must explicitly use the Arc options; unrelated minimal API endpoints use their ASP.NET serializer configuration.

### Default Configuration

Arc's own `JsonSerializerOptions` has the following defaults (not all are copied to MVC):

- **Property Naming**: Camel case with acronym-friendly handling (e.g., `XMLParser` becomes `xmlParser`)
- **Null Handling**: Null values are ignored when writing JSON
- **Enums**: Serialized as integers (not strings)
- **Concepts**: Full support for Cratis Concepts (strongly-typed primitives)
- **Date/Time**: Support for `DateOnly` and `TimeOnly` types
- **Types**: Support for `System.Type` and `System.Uri` serialization
- **Derived Types**: Polymorphic serialization support when derived types are discovered

### Accessing JsonSerializerOptions

The configured `JsonSerializerOptions` is available through dependency injection:

```csharp
using System.Text.Json;

public class MessageSerializer(JsonSerializerOptions jsonOptions)
{
    public string Serialize(string message) =>
        JsonSerializer.Serialize(new { Message = message }, jsonOptions);
}
```

Or through `ArcOptions`:

```csharp
public class MyService
{
    public MyService(IOptions<ArcOptions> arcOptions)
    {
        var jsonOptions = arcOptions.Value.JsonSerializerOptions;
    }
}
```

### Customizing JSON Serialization

You can add custom converters or modify the configuration through the options pattern:

```csharp
builder.AddCratisArc(options =>
{
    // Add a custom converter
    options.JsonSerializerOptions.Converters.Add(new MyCustomConverter());
});
```

For controllers, configure null omission, number handling, and other non-copied settings separately through MVC's `AddJsonOptions`. Arc's post-configuration still sets the naming policy, removes the first `JsonStringEnumConverter` if present, and appends Arc converters. `ConfigureHttpJsonOptions` configures ASP.NET minimal API serialization; it does **not** replace Arc's serializer for generated endpoints. Converter order matters: the first matching converter wins, so appending a converter may not override an existing Arc converter. Test the actual request/response contract; see [OpenAPI enum limitations](../open-api/enums.md).

## Best Practices

1. **Use Configuration Files**: For most scenarios, use `appsettings.json` configuration as it allows easy environment-specific overrides without code changes.

2. **Environment-Specific Settings**: Use `appsettings.{Environment}.json` files for environment-specific configurations.

3. **Programmatic Configuration**: Use programmatic configuration when you need to:
    - Set configuration based on runtime conditions
    - Use custom identity providers
    - Override specific settings that can't be easily expressed in JSON

4. **Route Planning**: Consider your API route structure carefully when configuring `GeneratedApis` options, especially in public-facing APIs where route stability is important.

5. **Header Standardization**: Use standard HTTP header names for correlation IDs and tenant IDs that align with your organization's conventions and any API gateways or load balancers in use.

6. **JSON Consistency**: Use the injected `JsonSerializerOptions` for manual serialization that must match generated Arc endpoints. Configure and test MVC's remaining options separately when controller output must match too.
