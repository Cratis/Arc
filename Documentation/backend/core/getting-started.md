---
title: Getting started with the lightweight host
description: Run standalone Arc commands and queries in a console project without ASP.NET Core or an event store.
---

Let's expose a greeting command and query from a console application. This checkpoint uses only `Cratis.Arc.Core`: no ASP.NET Core, MongoDB, EF Core, or Chronicle. A command can return ordinary response data; it does not need to append an event.

## Prerequisites and installation

Use the latest .NET SDK and current Cratis packages. The SDK must support the compiler APIs used by Arc's analyzers; that build requirement is separate from your application's target framework. Create a console project and add the lightweight package:

```bash
dotnet new console -n GreetingService
cd GreetingService
dotnet add package Cratis.Arc.Core
```

`Cratis.Arc` is the separate ASP.NET Core integration package. If you want Kestrel, MVC, or the ASP.NET middleware ecosystem, follow the [ASP.NET Core guide](../asp-net-core/index.md) instead.

## Complete example

Replace `Program.cs` with this runnable checkpoint. Keep these types in the global namespace below the top-level statements: the command's conventional route is `/api/greet`. The query uses an explicit `[Path]`.

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

`AddCratisArc` registers services and binds options. `UseCratisArc` maps Arc endpoints and schedules listener startup; `RunAsync` starts the host and waits for shutdown. Both registration and activation matter. This lesson deliberately permits anonymous access; read [authentication](authentication.md) and [authorization](authorization.md) before exposing private operations.

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

Arc builds lowercase, kebab-cased URLs from the configured route prefix, namespace segments, and command/query name. A query's `[Path]` overrides that convention; it is not a command routing attribute. Use [route configuration](../asp-net-core/configuration.md#route-generation-examples) rather than guessing a URL from the project name.

## Configuration

The following **configuration fragment** moves the listen URL into `appsettings.json` under the default `Cratis:Arc` section:

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

Remove the URL callback to let configuration supply it. Code options override configuration binding. The default listener URL is `http://+:5001/`; use an explicit loopback URL for local lessons. The [complete options reference](../configuration/index.md) covers environment variables, custom sections, and host-specific APIs.

## Adding services and integrations

`ArcApplicationBuilder` exposes standard .NET `Services`, `Configuration`, `Environment`, `Logging`, and `Metrics`. Register application services before `Build()` and inject them into `Handle()` or static query parameters. Background workers use `AddHostedService<T>()` with the normal .NET hosted-service lifecycle.

Choose integrations only when you need them:

- [MongoDB](../mongodb/index.md) and [Entity Framework Core](../entity-framework/index.md) add persistence without requiring event sourcing.
- [Chronicle](../chronicle/index.md) adds optional event sourcing, including its own package, registration, and activation requirements. `UseCratisArc()` alone does not activate the Chronicle client.
- [Endpoint mapping](endpoint-mapping.md), [static files](static-files.md), and [lightweight OpenAPI](openapi.md) are already available in Core.

## Troubleshooting

If no endpoints respond, verify `AddCratisArc()`, `UseCratisArc()`, and `RunAsync()` are all present. For listener failures, check the port, OS URL-binding permissions, and `Hosting.ApplicationUrl`. For deployed configuration files, ensure they are copied to the output directory.

Normal activation also maps [introspection](../introspection/index.md) and [identity discovery](../identity/development-and-topologies.md) endpoints. Review their anonymous Production defaults before publishing the service. `GET /.cratis/queries` describes discovered performers, not the final route table: this example's `Get` entry reports the convention-derived `route` `/api/get`, even though `[Path("/greeting")]` makes `/greeting` the callable URL. Introspection does not apply custom paths or all final mapping/deduplication decisions.

## Next steps

- [Commands](../commands/index.md) — validation, return values, and execution.
- [Queries](../queries/index.md) — data retrieval and observation.
- [Authentication](authentication.md) — establish trusted principals.
- [Tenancy](../tenancy/index.md) — select a tenant and enforce membership separately.
