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

## Service provider validation

A singleton that takes a scoped dependency in its constructor holds that one instance forever — the classic captive dependency. In a multi-tenant application it is the difference between "the right tenant's data" and "whichever tenant happened to be first", and nothing about it fails loudly. .NET has a detector for exactly this: `ServiceProviderOptions.ValidateScopes`, which the host turns on in Development so the capture throws the moment you resolve it.

Arc keeps that detector on. Every host Arc supports settles two `ServiceProviderOptions` fields for you:

| Option | Value Arc applies | Why |
| --- | --- | --- |
| `ValidateScopes` | `builder.Environment.IsDevelopment()` | The host's own default, restated so it survives. On in Development, off everywhere else. |
| `ValidateOnBuild` | `false` | Arc supplies registrations contextually — `IHostApplicationBuilder`, the type a convention binding is for, values only an executing command or an in-flight request can hand over. Eager validation constructs every registration up front and can resolve none of them, so leaving it on fails `Build()` outright. |

The reason both fields have to be stated together is that `UseDefaultServiceProvider` and `ConfigureContainer` each start from a brand new options object — setting one field discards every other value the host had already applied. Turning `ValidateOnBuild` off without restating `ValidateScopes` is what silently took the captive-dependency check with it.

### Overriding it

You own your container. State your own choice and it wins — with one ordering rule that differs by host:

```csharp
// ASP.NET Core and the generic host: call it AFTER AddCratisArc.
builder.AddCratisArc();
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;    // on in every environment, not just Development
    options.ValidateOnBuild = false;  // keep this off — see the table above
});
```

`AddCratisArc` calls `UseDefaultServiceProvider` itself on these hosts, and the last call wins, so a call placed *before* `AddCratisArc` is discarded.

Arc.Core has no such ordering rule. `ArcApplicationBuilder` applies its defaults while it is being constructed, so a `ConfigureContainer` call — your own factory, Autofac, Lamar — replaces them whether you make it before or after `AddCratisArc`:

```csharp
var builder = ArcApplication.CreateBuilder(args);

builder.ConfigureContainer(new MyServiceProviderFactory());  // before or after — either wins
builder.AddCratisArc();
```

> [!WARNING]
> `ValidateOnBuild = true` fails `Build()` on any Arc application. The failure is an `AggregateException` naming the registrations Arc supplies contextually, and it is not a defect in your wiring — leave the flag off.

### When you get no validation at all

`ValidateScopes` follows `IsDevelopment()`, which is an exact match on the environment name `Development`. A host running under a custom name — `Local`, `Dev`, `Staging` — is *not* Development by that rule, so it gets no scope validation, exactly as a bare .NET host would. If you want the check there, ask for it explicitly with the override above.

## A note on CORS

CORS is **not** an Arc option — configure it with standard ASP.NET Core (`builder.Services.AddCors(...)` and `app.UseCors(...)`). Arc neither wraps nor replaces it.

If you opt queries into the [HTTP QUERY method](../queries/using-the-http-query-method.md), add `QUERY` to your allowed methods (`policy.WithMethods("GET", "POST", "QUERY")`) — it is not a simple method, so cross-origin calls preflight. The default `GET` transport needs no CORS change.

## Where to go next

- [ASP.NET Core configuration](../asp-net-core/configuration.md) — route-generation examples and JSON serialization in depth.
- [Arc.Core getting started](../core/getting-started.md) — the console and worker host end to end.
- [Tenancy](../tenancy/configuration.md) — configure how the tenant is resolved.
- [Identity](../identity/index.md) — the identity system and providers.
- [Proxy Generation](../proxy-generation/Configuration/index.md) — the build-time `CratisProxies*` settings that must match `GeneratedApis`.
