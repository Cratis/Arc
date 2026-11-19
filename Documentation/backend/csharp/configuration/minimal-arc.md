---
title: Minimal Cratis Arc setup without an event store
description: Start an ASP.NET Core host with Cratis Arc commands and queries, without Chronicle or a database, and choose persistence and authentication explicitly.
---

Arc works without event sourcing. Use `Cratis.Arc` for ASP.NET Core; use `Cratis.Arc.Core` for the [lightweight HTTP host](../core/getting-started.md) instead.

## Install and start

Create an ASP.NET Core application:

```bash
dotnet new web -n ArcService
cd ArcService
dotnet add package Cratis.Arc
```

Replace `Program.cs` with:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc();

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

Run it with `dotnet run`. Registration discovers your application's commands and queries; `UseCratisArc` maps their endpoints. This minimal host has no application commands yet. Add a [model-bound command](../commands/model-bound/index.md) and [model-bound query](../queries/model-bound/index.md) next.

`AddCratisArc()` binds `Cratis:Arc` and does not register an event store, a database, or an authentication scheme. Commands can return response data, return `void`, or use application services. Add [MongoDB](../mongodb/getting-started.md) or [EF Core](../entity-framework/index.md) only if you need that persistence; MongoDB requires both `Server` and `Database`.

## Configure deliberately

- Choose [authentication and authorization](../authorizing-commands-and-queries.md). Authentication-required endpoints need a scheme that validates the caller; Arc does not infer one.
- Leave `TrustForwardedIdentityHeaders` off unless every request comes through a trusted ingress that authenticates callers and replaces identity headers.
- `ExposeExceptionDetails` defaults to the host's Development state; set it to `false` explicitly when needed. Discovery and query-health access are anonymous in Development and require authentication elsewhere.
- TypeScript generation requires the proxy build package and a nonempty `CratisProxiesOutputPath`; without it the generator logs that it is skipping output. Keep [build-time and runtime route settings aligned](../proxy-generation/Configuration/routing.md#settings-that-must-match-the-proxy-generator).

See [configuration keys](keys.md) for defaults, or [add Chronicle](minimal-arc-chronicle.md) when you need event sourcing.
