---
title: Run a lightweight HTTP service with Cratis Arc.Core
description: Create a console service with Cratis Arc.Core, expose a greeting command and query, and call them over HTTP without ASP.NET Core or an event store.
---

Build a console service that accepts a greeting command and answers a query over HTTP using only `Cratis.Arc.Core`. You do not need ASP.NET Core, a database, or Chronicle: the command returns response data without appending an event.

## Prerequisites and installation

Use the latest .NET SDK and current Cratis packages. Arc's analyzers need an SDK that supports their compiler APIs, even if your application targets an older framework. Create a console project and add the lightweight package:

```bash
dotnet new console -n GreetingService
cd GreetingService
dotnet add package Cratis.Arc.Core
```

Use `Cratis.Arc.Core` for this host, not `Cratis.Arc`, which is the separate ASP.NET Core integration package. That package's controller-based query discovery needs MVC services this host does not register; referencing it here causes startup to fail with `Unable to resolve service for type ... IActionDescriptorCollectionProvider`. If you need Kestrel, MVC, or ASP.NET middleware, follow the [ASP.NET Core guide](../asp-net-core/index.md) instead.

## Complete example

Replace `Program.cs` with the following example. Keep the types below the top-level statements, without a namespace, so the command's conventional route is `/api/greet`. The query sets its route explicitly with `[Path]`.

```csharp
using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;

var builder = ArcApplication.CreateBuilder(args);
builder.AddCratisArc(options =>
    options.Hosting.ApplicationUrl = "http://localhost:5000/");

var app = builder.Build();
app.UseCratisArc();
await app.RunAsync();

[AllowAnonymous]
[Command]
public record Greet(string Name)
{
    public string Handle() => $"Hello, {Name}!";
}

[ReadModel]
public record Greeting(string Text)
{
    [AllowAnonymous]
    [Path("/greeting")]
    public static Greeting Get(string name) => new($"Hello, {name}!");
}
```

The command and read model sit in the global namespace to keep the command route short. This causes two `CA1050` warnings in the single-file example.

`AddCratisArc` registers services and binds options. After `Build()`, `UseCratisArc` maps the endpoints and schedules listener startup; `RunAsync` starts the host and waits for shutdown. Keep all three calls: registration alone does not start a listener.

Both operations deliberately allow anonymous access. Before adding private operations, configure [authentication](authentication.md) to establish the caller and [authorization](authorization.md) to decide what they may do.

## Run and observe

Start the application:

```bash
dotnet run
```

In another terminal, call the command and query:

```bash
curl -X POST http://localhost:5000/api/greet \
  -H "Content-Type: application/json" \
  -d '{"name":"World"}'
curl 'http://localhost:5000/greeting?name=World'
```

The command result's `response` is `"Hello, World!"`; the query result's `data` contains `{"text":"Hello, World!"}`. Both use Arc result wrappers. Neither operation persists anything. Stop the service with Ctrl+C.

Arc builds lowercase, kebab-cased URLs from the configured route prefix, namespace segments, and command/query name. A query's or command's `[Path]` overrides that convention (import the shared attribute from `Cratis.Arc.Queries.ModelBound` for both). Use [route configuration](../asp-net-core/configuration.md#route-generation-examples) rather than guessing a URL from the project name.

## Configuration

To keep the listen URL outside `Program.cs`, add this section to `appsettings.json`. Arc uses `Cratis:Arc` by default:

```json
{
    "Cratis": {
        "Arc": {
            "Hosting": {
                "ApplicationUrl": "http://localhost:5000/"
            }
        }
    }
}
```

Remove the URL callback from `AddCratisArc` so configuration supplies the value; code options override configuration binding. Keep an explicit loopback URL while working locally: the default listener URL, `http://+:5001/`, is not loopback-only. Use the [complete options reference](../configuration/index.md) when you need environment variables, custom sections, or host-specific APIs.

## Adding services and integrations

`ArcApplicationBuilder` exposes standard .NET `Services`, `Configuration`, `Environment`, `Logging`, and `Metrics`. Register application services before `Build()` and inject them into `Handle()` or static query parameters. Background workers use `AddHostedService<T>()` with the normal .NET hosted-service lifecycle.

Choose integrations only when you need them:

- [MongoDB](../mongodb/index.md) and [Entity Framework Core](../entity-framework/index.md) add persistence without requiring event sourcing.
- [Chronicle](../chronicle/index.md) adds optional event sourcing, including its own package, registration, and activation requirements. `UseCratisArc()` alone does not activate the Chronicle client.
- [Endpoint mapping](endpoint-mapping.md), [static files](static-files.md), and [lightweight OpenAPI](openapi.md) are already available in Core.

## Troubleshooting

If no endpoints respond, verify `AddCratisArc()`, `UseCratisArc()`, and `RunAsync()` are all present. For listener failures, check the port, OS URL-binding permissions, and `Hosting.ApplicationUrl`. For deployed configuration files, ensure they are copied to the output directory.

Activation also maps [introspection](../introspection/index.md) and [identity discovery](../identity/development-and-topologies.md) endpoints. By default, they allow anonymous access only in Development and require authentication elsewhere. Review the [production access settings](../introspection/index.md#production-access) before deploying the service.

Use `GET /.cratis/queries` to inspect discovered queries, not as a final route table. In this example, its `Get` entry reports `/greeting`, matching `[Path]`; introspection does not apply every final mapping or deduplication decision.

## Next steps

- [Commands](../commands/index.md) — validation, return values, and execution.
- [Queries](../queries/index.md) — data retrieval and observation.
- [Authentication](authentication.md) — establish trusted principals.
- [Tenancy](../tenancy/index.md) — select a tenant and enforce membership separately.
