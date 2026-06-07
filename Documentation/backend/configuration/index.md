# Configuring Arc

Arc reads all of its settings from a single `ArcOptions` object. Wherever Arc runs — an ASP.NET Core web app, an Arc.Core console or worker, or the full Cratis stack — you configure it the same way: bind `ArcOptions` from configuration, then optionally override it in code. This page is the map: the hosting models, the three configuration mechanisms, and the full `ArcOptions` tree.

## The three hosting models

| Host | Bootstrap | Activate | Use it for |
| --- | --- | --- | --- |
| **ASP.NET Core** | `WebApplication.CreateBuilder(args)` → `builder.AddCratisArc(...)` | `app.UseCratisArc()` → `app.Run()` | A web API or full-stack app. The listen URL comes from Kestrel / `launchSettings.json`. |
| **Arc.Core** | `ArcApplication.CreateBuilder(args)` → `builder.AddCratisArc(...)` | `app.UseCratisArc()` → `await app.RunAsync()` | A console app or worker with no ASP.NET Core. The listen URL comes from `ArcOptions.Hosting.ApplicationUrl`. |
| **Cratis stack** | `WebApplication.CreateBuilder(args)` → `builder.AddCratis(...)` | `app.UseCratis()` → `app.Run()` | Arc + Chronicle in one host — see the [Cratis package](../chronicle/cratis-package.md). |

`AddCratisArc` takes its arguments in this order: `configureOptions` (an `Action<ArcOptions>`), `configureBuilder` (an `Action<IArcBuilder>` for adding Chronicle, MongoDB, or EF Core), and `configSectionPath`. Use the named `configureBuilder:` argument when you only want to add a builder feature:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithChronicle());
```

For raw `IServiceCollection` wiring (advanced), `AddCratisArcCore()` registers the same services without the builder.

## Three ways to configure

Every setting can be supplied three ways, layered in this order — later wins:

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
| `CorrelationId.HttpHeader` | `string` | `X-Correlation-ID` | The header carrying the correlation ID. |
| `Tenancy.ResolverType` | `TenantResolverType` | `Header` | How the tenant is resolved: `Header`, `Query`, `Claim`, or `Development`. |
| `Tenancy.HttpHeader` | `string` | `x-cratis-tenant-id` | The header used when `ResolverType` is `Header`. |
| `Tenancy.QueryParameter` | `string` | `tenantId` | The query parameter used when `ResolverType` is `Query`. |
| `Tenancy.ClaimType` | `string` | `tenant_id` | The claim used when `ResolverType` is `Claim`. |
| `Tenancy.DevelopmentTenantId` | `string` | `development` | The fixed tenant used when `ResolverType` is `Development`. |
| `GeneratedApis.RoutePrefix` | `string` | `api` | Base prefix for generated command and query routes. |
| `GeneratedApis.SegmentsToSkipForRoute` | `int` | `0` | Namespace segments to drop when building a route. |
| `GeneratedApis.IncludeCommandNameInRoute` | `bool` | `true` | Append the command name as the last route segment. |
| `GeneratedApis.IncludeQueryNameInRoute` | `bool` | `true` | Append the query name as the last route segment. |
| `Query.KeepAliveInterval` | `TimeSpan` | `00:00:30` | Keep-alive cadence for observable (real-time) queries. |
| `IdentityDetailsProvider` | `Type?` | `null` (auto-discovered) | The identity details provider type. |
| `Hosting.ApplicationUrl` | `string` | `http://+:5001/` | The listen URL — **Arc.Core only** (ignored under ASP.NET Core). |
| `JsonSerializerOptions` | `JsonSerializerOptions` | Arc defaults | The serializer used across controllers, manual serialization, and generated endpoints. Configure in code only. |

Route generation (`GeneratedApis`) and JSON serialization have worked examples on the [ASP.NET Core configuration](../asp-net-core/configuration.md) page; `Query.KeepAliveInterval` is covered with the [observable query demultiplexer](../queries/observable-query-demultiplexer.md).

## Adding features with the builder

The `configureBuilder` callback exposes `IArcBuilder`, which is where Arc's pluggable backends attach:

- `arc.WithChronicle()` — event sourcing with Cratis Chronicle.
- `arc.WithMongoDB()` — MongoDB read models. See [MongoDB](../mongodb/index.md).
- `arc.WithEntityFrameworkCore()` — relational read models. See [Entity Framework](../entity-framework/index.md).

## Identity and authentication

Arc resolves an identity details provider automatically by type discovery. Set `ArcOptions.IdentityDetailsProvider` to pin a specific type, or register one explicitly:

```csharp
builder.Services.AddIdentityProvider<MyIdentityDetailsProvider>();
```

For authenticating requests, see [Authentication](../core/authentication.md). When you use the [Cratis package](../chronicle/cratis-package.md), Microsoft Identity Platform authentication is wired for you.

## A note on CORS

CORS is **not** an Arc option — configure it with standard ASP.NET Core (`builder.Services.AddCors(...)` and `app.UseCors(...)`). Arc neither wraps nor replaces it.

## Where to go next

- [ASP.NET Core configuration](../asp-net-core/configuration.md) — route-generation examples and JSON serialization in depth.
- [Arc.Core getting started](../core/getting-started.md) — the console and worker host end to end.
- [Tenancy](../tenancy/configuration.md) — configure how the tenant is resolved.
- [Identity](../identity/index.md) — the identity system and providers.
- [Proxy Generation](../proxy-generation/Configuration/index.md) — the build-time `CratisProxies*` settings that must match `GeneratedApis`.
