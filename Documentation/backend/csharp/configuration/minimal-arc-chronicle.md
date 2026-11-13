---
title: Minimal Cratis Arc and Chronicle setup
description: Start an ASP.NET Core application with Cratis Arc and a separate Chronicle event store, understand the identity defaults, and choose explicit registration when needed.
---

Use the `Cratis` composition package to register Arc, the Chronicle client, and the Microsoft Identity Platform forwarded-header authentication scheme together. Arc itself does not require Chronicle; see [Arc alone](minimal-arc.md).

## Install and start

Create an ASP.NET Core application:

```bash
dotnet new web -n EventService
cd EventService
dotnet add package Cratis
```

Replace `Program.cs` with:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCratis(
    configureChronicleOptions: options => options.EventStore = "EventService");

var app = builder.Build();
app.UseCratis();
app.Run();
```

Run a separate Chronicle instance and configure the client connection before running the application. Follow the [Chronicle setup and registration guide](../chronicle/cratis-package.md) for the local container and `Cratis:Chronicle` settings. `AddCratis()` adds a client; it does not start an event store inside the application. The tenant selects the Chronicle event-store namespace by default.

Add your application's [commands](../commands/model-bound/index.md), events, and [read models](../chronicle/read-models/index.md). Arc 23 defaults read protection to `On`; read-model parameters must satisfy the [protected-read requirements](../chronicle/consistency/index.md). Use an explicit `Off` or `Measure` declaration when intentionally adopting protection gradually, rather than assuming the Arc 22 default.

## Identity and storage

The registered header scheme ignores forwarded identity headers while `TrustForwardedIdentityHeaders` is `false`, its default. Requests remain anonymous and startup logs a warning. Opt in only behind a trusted ingress that authenticates callers and strips and replaces those headers. Otherwise register an appropriate validating authentication scheme. See [Microsoft Identity](../asp-net-core/microsoft-identity.md#trusting-the-forwarded-headers).

`AddCratis()` does not configure an independent MongoDB connection for Arc's standalone MongoDB provider. If you add that provider, configure its required `Cratis:MongoDB:Server` and `Database` separately. The Chronicle server owns its own storage configuration.

For explicit composition with your own authentication, use the [host-specific `AddCratisArc` and `WithChronicle` registration](../chronicle/cratis-package.md) instead of `AddCratis()`. ASP.NET Core's overload uses `ChronicleAspNetCoreOptions`; the generic-host overload uses `ChronicleClientOptions`. Do not import both extension namespaces and rely on overload selection.

See [configuration keys](keys.md) for the complete settings reference, and [proxy generation](../proxy-generation/getting-started.md) when adding TypeScript clients. Set `CratisProxiesOutputPath` explicitly and match the generator's route settings to `ArcOptions.GeneratedApis`.
