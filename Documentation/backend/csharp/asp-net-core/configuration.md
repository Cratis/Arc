---
title: Configuration
description: Configure Arc in an ASP.NET Core host through appsettings.json or code, including route generation and JSON settings.
---

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

## Turning controllers off

`AddCratisArc` registers ASP.NET Core MVC and discovers the controllers in your project assemblies. An application that only uses model-bound commands and queries doesn't need MVC. Turn it off with `WithoutControllers` on the Arc builder:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

Controllers are on unless you call `WithoutControllers` or turn them off with the `CratisArcControllersSupport` property (see below). With controllers off:

- Model-bound commands and queries, observable queries included, work as before. `UseCratisArc` maps their endpoints without MVC.
- Controller-based commands and queries aren't available, because no controllers are discovered.
- MVC-only features are unavailable. Calling `MapControllers` fails, because MVC isn't registered, and `[FromRequest]` model binding, `[AspNetResult]` and MVC model validation are unavailable. Arc still validates model-bound commands and queries.
- OpenAPI documents describe the model-bound endpoints only.
- The application registers the ASP.NET Core services it used to get from MVC. Add `builder.Services.AddCors()` before `UseCors`, and `builder.Services.AddEndpointsApiExplorer()` when you use Swashbuckle. Arc registers the ASP.NET Core authentication and authorization services, so `UseAuthentication`, `UseAuthorization` and policies work without calling `AddAuthorization`. Authentication schemes still come from your own `AddAuthentication(...)`, and protected introspection needs a default scheme, as it does with controllers on.

`WithoutControllers` works with `AddCratis` too, through `configureArcBuilder`. The `IHostBuilder` overload of `AddCratisArc` doesn't support `configureBuilder` (passing one throws), so there it's only the MSBuild property below that turns controllers off.

### Removing MVC from a trimmed or NativeAOT publish

`WithoutControllers` is a runtime choice. `AddCratisArc` still references the code that registers MVC, so a trimmed or NativeAOT publish keeps it, along with the trim warnings MVC raises. To let the trimmer remove it, turn controllers off in the application's project file instead:

```xml
<PropertyGroup>
    <CratisArcControllersSupport>false</CratisArcControllersSupport>
</PropertyGroup>
```

The `Cratis.Arc` package turns the property into the `Cratis.Arc.Controllers.IsSupported` runtime switch in the application's `runtimeconfig.json`. With it set to `false`:

- Every `AddCratisArc` overload, the `IHostBuilder` one included, registers no MVC and discovers no controllers, exactly as `WithoutControllers` does, and everything listed above applies. You don't need to call `WithoutControllers` as well.
- A trimmed or NativeAOT publish removes the code that registers MVC, so MVC's trim warnings and most MVC assemblies drop out of the output. Arc still references a few MVC types elsewhere, so `Microsoft.AspNetCore.Mvc.Core` and `Microsoft.AspNetCore.Mvc.Abstractions` stay. This works for `net8.0`, `net9.0` and `net10.0`.
- The rest of Arc isn't trim or NativeAOT compatible yet, so a trimmed publish still reports trim warnings from Arc itself.

The switch is written into the `runtimeconfig.json` of the project that sets the property, so it applies to whichever process runs. A test project that hosts the application, for example through `WebApplicationFactory<Program>`, runs with its own `runtimeconfig.json` and keeps MVC unless it sets the property too. Set the property in a `Directory.Build.props` shared by the application and its tests, so tests run the way the published application does.

Leave the property unset, or set it to `true`, to keep controllers on. The property only has an effect through the `Cratis.Arc` package; a project that references Arc's source through a `ProjectReference` has to import `build/Cratis.Arc.targets` itself.

### Configuring MVC when controllers are on

With controllers on, `AddCratisArc` registers MVC after the `configureBuilder` callback returns. Configure MVC after `AddCratisArc` returns, not inside `configureBuilder`:

```csharp
builder.AddCratisArc();
builder.Services.Configure<MvcOptions>(options => { /* ... */ });
builder.Services.PostConfigure<Microsoft.AspNetCore.Mvc.JsonOptions>(options => { /* ... */ });
```

MVC settings made inside the callback run before Arc's own setup and before MVC's defaults exist. Arc's JSON configuration then overrides yours, Arc's model binder goes ahead of one you insert, and removing a default formatter finds nothing to remove.

## Hosting under a path prefix

For an app reached at `/workbench`, configure ASP.NET Core's `UsePathBase` **before** `UseRouting` and `UseCratisArc`. It removes the prefix from the request path before endpoint matching; leave Arc's generated API route prefix (normally `/api`) unchanged.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc();

var app = builder.Build();
app.UsePathBase("/workbench");
app.UseRouting();
app.UseCratisArc();
app.Run();
```

Set `<Arc apiBasePath="/workbench">` in the frontend and set the application router's `basename` to `/workbench` separately. Neither setting changes the server's routes; see [React provider configuration](../../../frontend/react/arc.md#hosting-under-a-path-prefix). If a reverse proxy already removes the prefix, the server sees unprefixed paths, so configure the frontend and ingress accordingly.

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

Arc-generated endpoints use `ArcOptions.JsonSerializerOptions`. Arc's MVC post-configuration copies only the property naming policy and appends Arc converters; it does not copy the entire options object. Settings such as `DefaultIgnoreCondition`, `NumberHandling`, and other MVC serializer options require separate MVC configuration if controller output must match. Plain ASP.NET minimal API endpoints receive only Arc's concept converters by default (single concepts, concept collections and dictionaries keyed by concepts). Concepts in request and response bodies use their underlying primitive JSON values; other types retain ASP.NET's JSON defaults. Manual serialization must explicitly use the Arc options.

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

For controllers, configure null omission, number handling, and other non-copied settings separately through MVC's `AddJsonOptions`. Arc's post-configuration still sets the naming policy, removes the first `JsonStringEnumConverter` if present, and appends Arc converters. `ConfigureHttpJsonOptions` configures ASP.NET minimal API serialization; it does **not** replace Arc's serializer for generated endpoints. For minimal APIs, ASP.NET Core's camel-case naming remains unchanged, including when you explicitly choose `JsonNamingPolicy.CamelCase`. Configure `ConfigureHttpJsonOptions` to choose another naming policy, opt out of primitive concept serialization by registering an application converter for that concept type (for example, a `JsonConverter<AccountId>` that writes and reads `{ "value": "..." }`), or set null omission, number handling, and other serializer settings. Application converters keep their order and take precedence over Arc's appended concept converters. Converter order matters: the first matching converter wins. Enum, date/time, URI, type, geospatial and derived-type converters are **not** added to plain minimal APIs; configure those explicitly if needed. Test the actual request/response contract; see [OpenAPI enum schemas](../open-api/enums.md).

### Adding Source-Generated Metadata

Arc resolves the metadata for its own wire types - `CommandResult`, `QueryResult`, change sets, observable query messages and the identity responses - from a source-generated `JsonSerializerContext` it ships. Everything else, such as your read models, command responses and query arguments, resolves through reflection. In a trimmed or NativeAOT application reflection-based serialization is disabled, so Arc needs metadata for those types from you.

Declare a `JsonSerializerContext` for your types and add it with `AddJsonTypeInfoResolver`:

```csharp
using System.Text.Json.Serialization;
using Cratis.Arc.Commands;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(IEnumerable<Order>))]
[JsonSerializable(typeof(PlaceOrder))]
[JsonSerializable(typeof(CommandResult<OrderId>))]
public partial class AppJsonSerializerContext : JsonSerializerContext;
```

```csharp
builder.AddCratisArc(options => options.AddJsonTypeInfoResolver(AppJsonSerializerContext.Default));
```

`Order` and `PlaceOrder` stand in for your own read model and command, and `OrderId` for the response `PlaceOrder` returns. Arc's `JsonSerializerOptions` look up a type's metadata in this order:

1. The resolvers you add with `AddJsonTypeInfoResolver`. Your contracts win, including any you customize for Arc's own types. They are consulted first whether you add them before or after you wrap, replace or clear `TypeInfoResolver`; added while Arc's resolver is still in the chain, they are consulted in the order you add them.
2. Resolvers you append to `JsonSerializerOptions.TypeInfoResolverChain` with `Add`, in the order you append them.
3. Arc's source-generated metadata for its own wire types.
4. The reflection-based resolver, only when reflection-based serialization is enabled (`JsonSerializer.IsReflectionEnabledByDefault`). This is the resolver `System.Text.Json` falls back to for options without one, so an application that adds nothing serializes exactly as before.

The serializer options still decide naming, null handling, number handling and converters, so the JSON is the same whichever resolver supplies the metadata. Use `JsonSourceGenerationMode.Metadata`, as Arc does. Arc's options carry custom converters, so `System.Text.Json` cannot use a generated serialization fast path with them; generating one only adds code.

Keep these in mind:

- Register `CommandResult<TResponse>` for every response type your commands return, such as `CommandResult<OrderId>` above. Arc writes a command's response wrapped in `CommandResult<TResponse>`, where `TResponse` is the response's runtime type - a generic type its own metadata cannot cover, so registering `TResponse` alone is not enough. Without it, a command that returns a response fails with a `NotSupportedException` when reflection is disabled.
- Register the runtime types of query data, not only the declared ones. `QueryResult.Data` and change-set items are written by their runtime type, so if a query returns a `List<Order>`, register `List<Order>`.
- Add resolvers while configuring Arc. `JsonSerializerOptions` become read-only once they have been used.
- Arc keeps a single resolver of its own in `TypeInfoResolverChain`, after the resolvers added with `AddJsonTypeInfoResolver` and before anything you append. That resolver consults the resolvers appended after it first, so a resolver you append - for instance a `DefaultJsonTypeInfoResolver` with `Modifiers` that rename or ignore members - still decides the contract of every type it knows, Arc's own included, as it did when it was the only resolver in the chain. Types it does not know fall through to Arc's metadata and, when enabled, reflection. The same holds for a copy of Arc's options, `new JsonSerializerOptions(options)`: a resolver appended to the copy wins for the copy and leaves Arc's options alone.
- Assigning `JsonSerializerOptions.TypeInfoResolver` replaces the whole chain, Arc's resolver included. The resolver you assign then serializes every type on its own, so with reflection disabled it has to cover Arc's wire types too. Prefer `AddJsonTypeInfoResolver`.
- To add modifiers to every contract, wrap the resolver the options carry: `options.TypeInfoResolver = options.TypeInfoResolver!.WithAddedModifier(...)`. Arc's resolver stays inside the wrapper, so the modifiers apply to what it resolves too. On .NET 8 and .NET 9, wrap `TypeInfoResolver` before appending anything to `TypeInfoResolverChain`, or build the combination explicitly with `JsonTypeInfoResolver.Combine(...)`: wrapping after appending crashes with a stack overflow on those versions. That is `System.Text.Json` behavior, not Arc's.
- Arc's options always carry a resolver. `TypeInfoResolver ??= x`, or `if (TypeInfoResolver is null) { ... }`, therefore no longer assigns anything - use `AddJsonTypeInfoResolver`, `TypeInfoResolverChain.Add` or an explicit assignment instead. And `TypeInfoResolver?.WithAddedModifier(...)` now takes effect.
- Assigning `options.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.TypeInfoResolver, other)` flattens into Arc's resolver followed by `other`, so `other` wins for the types it knows, like any appended resolver. Only appending a combination that contains Arc's resolver, `options.TypeInfoResolverChain.Add(JsonTypeInfoResolver.Combine(options.TypeInfoResolver, other))`, resolves in the order you compose it: Arc's resolver, and with it Arc's metadata and reflection, before `other`.
- `AddJsonTypeInfoResolver` resolvers are consulted first in all these cases: after you wrap `TypeInfoResolver` they go ahead of the wrapper, and after you set it to `null` Arc's resolver is composed anew behind them, so Arc's metadata and the reflection fallback are kept. Once you have wrapped the resolver, a resolver you add later is consulted before the ones you added earlier.
- The resolvers apply to Arc's own endpoints and serialization. MVC controllers and plain minimal APIs keep their own serializer options.

When you create options with `ConfigureArcDefaults()` outside `ArcOptions`, a resolver the options already carry stays first, followed by Arc's resolver: `new JsonSerializerOptions { TypeInfoResolver = AppJsonSerializerContext.Default }.ConfigureArcDefaults()`.

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
