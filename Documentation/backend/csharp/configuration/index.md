# Configuring Arc

Arc reads all of its settings from a single `ArcOptions` object. Wherever Arc runs — an ASP.NET Core web app, an Arc.Core console or worker, or the full Cratis stack — you configure it the same way: bind `ArcOptions` from configuration, then optionally override it in code. This page is the map: the hosting models, the three configuration mechanisms, and the full `ArcOptions` tree.

## The three hosting models

| Host | Bootstrap | Activate | Use it for |
| --- | --- | --- | --- |
| **ASP.NET Core** | `WebApplication.CreateBuilder(args)` → `builder.AddCratisArc(...)` | `app.UseCratisArc()` → `app.Run()` | A web API or full-stack app. The listen URL comes from Kestrel / `launchSettings.json`. |
| **Arc.Core** | `ArcApplication.CreateBuilder(args)` → `builder.AddCratisArc(...)` | `app.UseCratisArc()` → `await app.RunAsync()` | A console app or worker with no ASP.NET Core. The listen URL comes from `ArcOptions.Hosting.ApplicationUrl`. |
| **Cratis stack** | `WebApplication.CreateBuilder(args)` → `builder.AddCratis(...)` | `app.UseCratis()` → `app.Run()` | Arc + Chronicle in one host — see the [Cratis package](../chronicle/cratis-package.md). |

`AddCratisArc` takes its arguments in this order: `configureOptions` (an `Action<ArcOptions>`), `configureBuilder` (an `Action<IArcBuilder>` for optional integrations), and `configSectionPath`. Use the named `configureBuilder:` argument when you only want to add a builder feature.

For example, this startup fragment adds the standalone MongoDB provider to an existing ASP.NET Core `WebApplicationBuilder`. First install `Cratis.Arc.MongoDB` and configure its connection and database as described in [MongoDB getting started](../mongodb/getting-started.md). It does not require Chronicle.

```csharp
using Cratis.Arc;

builder.AddCratisArc(configureBuilder: arc => arc.WithMongoDB());
```

For optional event sourcing, follow the host- and package-specific [Arc–Chronicle registration example](../chronicle/cratis-package.md) instead of substituting an unqualified `WithChronicle()` call.

For advanced wiring, `IServiceCollection.AddCratisArcCore()` registers Core services only; it is not equivalent to host bootstrap, configuration binding, request-context wiring, identity setup, endpoint mapping, or listener activation. `IHostBuilder.AddCratisArcCore(configureOptions: ...)` is a different overload. Prefer the host-specific builders above.

## Three ways to configure

Configuration-bindable settings can be supplied three ways, layered in this order — later wins:

1. **`appsettings.json`** under the `Cratis:Arc` section.
2. **Environment variables** with the `Cratis__Arc__` prefix (.NET maps the `__` separator onto nested keys), for example `Cratis__Arc__GeneratedApis__RoutePrefix`.
3. **Code**, via the `configureOptions` callback — it runs after binding, so it overrides the file and the environment.

```csharp
builder.AddCratisArc(options =>
{
    options.GeneratedApis.RoutePrefix = "v1/api";   // overrides appsettings / env
});
```

## The ArcOptions tree

| Option | Type | Default | What it controls |
| --- | --- | --- | --- |
| `ExposeExceptionDetails` | `bool` | `RuntimeEnvironment.IsDevelopment` | Expose exception messages and stack traces in serialized command/query results only in Development by default. Full detail remains in server logs when responses are redacted. Keep disabled in public production environments. |
| `CorrelationId.HttpHeader` | `string` | `X-Correlation-ID` | The header carrying the correlation ID. |
| `Tenancy.ResolverType` | `TenantResolverType` | `Header` | How the tenant is resolved: `Header`, `Query`, `Claim`, `Subdomain`, `Development`, or `Fixed`. |
| `Tenancy.HttpHeader` | `string` | `x-cratis-tenant-id` | The header used when `ResolverType` is `Header`, and the fallback header when it is `Subdomain`. |
| `Tenancy.BaseDomain` | `string` | empty | Required for `Subdomain`: the application-owned base domain. Exactly one preceding DNS label selects a tenant; other hosts fall back to `HttpHeader`. Validation checks syntax, not domain ownership or tenant membership. |
| `Tenancy.QueryParameter` | `string` | `tenantId` | The query parameter used when `ResolverType` is `Query`. |
| `Tenancy.ClaimType` | `string` | `tenant_id` | The claim used when `ResolverType` is `Claim`. |
| `Tenancy.FixedTenantId` | `string` | `development` | The tenant every request resolves to when `ResolverType` is `Fixed` or `Development`. |
| `Tenancy.DevelopmentTenantId` | `string` | `development` | The same value under its original name — reading or writing either key sets both. Supply only one; if both are present the binder's property order decides. |
| `GeneratedApis.RoutePrefix` | `string` | `api` | Base prefix for generated command and query routes. |
| `GeneratedApis.SegmentsToSkipForRoute` | `int` | `0` | Namespace segments to drop when building a route. |
| `GeneratedApis.IncludeCommandNameInRoute` | `bool` | `true` | Append the command name as the last route segment. |
| `GeneratedApis.IncludeQueryNameInRoute` | `bool` | `true` | Append the query name as the last route segment. |
| `GeneratedApis.EnableQueryHttpMethod` | `bool` | `true` | Expose generated queries over HTTP `QUERY` with JSON arguments in addition to `GET`. Disable when infrastructure rejects that method. |
| `Query.KeepAliveInterval` | `TimeSpan` | `00:00:30` | Idle keep-alive cadence for observable query hub connections; zero or negative disables keep-alive. |
| `IdentityDetailsProvider` | `Type?` | `null` (auto-discovered) | The identity details provider type. |
| `Hosting.ApplicationUrl` | `string` | `http://+:5001/` | The listen URL — **Arc.Core only** (ignored under ASP.NET Core). |
| `JsonSerializerOptions` | `JsonSerializerOptions` | Arc defaults | Generated Arc endpoints use these options; manual serialization must opt in. MVC receives only the naming policy and converters, not null/number handling or other settings. Configure in code only; see the [MVC serialization boundary](../asp-net-core/configuration.md#json-serialization). |

Route generation (`GeneratedApis`) and JSON serialization have worked examples on the [ASP.NET Core configuration](../asp-net-core/configuration.md) page; `Query.KeepAliveInterval` is covered with the [observable query demultiplexer](../queries/observable-query-demultiplexer.md).

## Adding features with the builder

The `configureBuilder` callback exposes `IArcBuilder`, which is where Arc's pluggable backends attach:

- [Arc–Chronicle registration](../chronicle/cratis-package.md) — optional event sourcing; choose the documented host-specific overload and packages.
- `arc.WithMongoDB()` — MongoDB read models. See [MongoDB](../mongodb/index.md).
- `arc.WithEntityFrameworkCore()` — relational read models. See [Entity Framework](../entity-framework/index.md).

## Identity and authentication

The header/query/subdomain resolvers select a tenant; they do not authorize membership. See [tenant resolution](../tenancy/resolvers.md) before accepting caller-selected tenant IDs.

Arc resolves an identity details provider automatically by type discovery. Set `ArcOptions.IdentityDetailsProvider` to pin a specific type, or register one explicitly:

```csharp
builder.Services.AddIdentityProvider<MyIdentityDetailsProvider>();
```

For authenticating requests, see [Authentication](../core/authentication.md). When you use the [Cratis package](../chronicle/cratis-package.md), Microsoft Identity Platform authentication is wired for you.

## Service provider validation

A singleton that takes a scoped dependency in its constructor holds that one instance forever — the classic captive dependency. In a multi-tenant application it is the difference between "the right tenant's data" and "whichever tenant happened to be first", and nothing about it fails loudly. .NET has a detector for exactly this: `ServiceProviderOptions.ValidateScopes`, which the host turns on in Development so the capture throws the moment you resolve it.

Arc keeps that detector on. Every host Arc supports settles two `ServiceProviderOptions` fields for you:

| Option            | Value Arc applies                     | Why                                                                                                                                                                                                                                                                                                                 |
| ----------------- | ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ValidateScopes`  | `builder.Environment.IsDevelopment()` | The host's own default, restated so it survives. On in Development, off everywhere else.                                                                                                                                                                                                                            |
| `ValidateOnBuild` | `false`                               | Arc supplies registrations contextually — `IHostApplicationBuilder`, the type a convention binding is for, values only an executing command or an in-flight request can hand over. Eager validation constructs every registration up front and can resolve none of them, so leaving it on fails `Build()` outright. |

The reason both fields have to be stated together is that `UseDefaultServiceProvider` and `ConfigureContainer` each start from a brand new options object — setting one field discards every other value the host had already applied. Turning `ValidateOnBuild` off without restating `ValidateScopes` is what silently took the captive-dependency check with it.

### Overriding it

You own your container. State your own choice and it wins — with one ordering rule that differs by host:

```csharp
// WebApplicationBuilder (ASP.NET Core): call it AFTER AddCratisArc.
builder.AddCratisArc();
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;    // on in every environment, not just Development
    options.ValidateOnBuild = false;  // keep this off — see the table above
});
```

For an `IHostBuilder`, call `UseDefaultServiceProvider` directly on the builder after its Arc registration (there is no `builder.Host` property). Arc calls `UseDefaultServiceProvider` itself on these hosts, and the last call wins, so a call placed _before_ `AddCratisArc` is discarded.

Arc.Core has no such ordering rule. `ArcApplicationBuilder` applies its defaults while it is being constructed, so a `ConfigureContainer` call — your own factory, Autofac, Lamar — replaces them whether you make it before or after `AddCratisArc`:

```csharp
var builder = ArcApplication.CreateBuilder(args);

builder.ConfigureContainer(new MyServiceProviderFactory());  // before or after — either wins
builder.AddCratisArc();
```

> [!WARNING]
> `ValidateOnBuild = true` fails `Build()` on any Arc application. The failure is an `AggregateException` naming the registrations Arc supplies contextually, and it is not a defect in your wiring — leave the flag off.

### When you get no validation at all

`ValidateScopes` follows `IsDevelopment()`, which is an exact match on the environment name `Development`. A host running under a custom name — `Local`, `Dev`, `Staging` — is _not_ Development by that rule, so it gets no scope validation, exactly as a bare .NET host would. If you want the check there, ask for it explicitly with the override above.

## A note on CORS

CORS is **not** an Arc option. In an ASP.NET Core host, configure it with standard ASP.NET Core (`builder.Services.AddCors(...)` and `app.UseCors(...)`). Arc neither wraps nor replaces it. These ASP.NET extensions do not apply to `ArcApplication`; configure cross-origin handling at your trusted ingress for that host.

Generated endpoints accept the [HTTP QUERY method](../queries/using-the-http-query-method.md) in addition to GET by default (`GeneratedApis.EnableQueryHttpMethod = true`). If clients use it across origins, include `QUERY` in your allowed methods (`policy.WithMethods("GET", "POST", "QUERY")`) — it is not a simple method, so cross-origin calls preflight. This is a method-allowlist consideration, not a substitute for configuring allowed origins, headers, and credentials. A client that uses GET does not need QUERY in that allowlist.

## Where to go next

- [ASP.NET Core configuration](../asp-net-core/configuration.md) — route-generation examples and JSON serialization in depth.
- [Arc.Core getting started](../core/getting-started.md) — the console and worker host end to end.
- [Tenancy](../tenancy/configuration.md) — configure how the tenant is resolved.
- [Identity](../identity/index.md) — the identity system and providers.
- [Proxy Generation](../proxy-generation/Configuration/index.md) — the build-time `CratisProxies*` settings that must match `GeneratedApis`.
