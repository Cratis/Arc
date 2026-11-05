---
title: Set invariant culture in Cratis Arc.Core services
description: Set invariant default cultures in a Cratis Arc.Core service so culture-sensitive parsing and formatting use the same conventions across machines.
---

Set invariant default cultures in a `Cratis.Arc.Core` service when culture-sensitive parsing and formatting should not depend on the machine's regional settings. Invariant culture supplies a shared set of conventions; code that explicitly selects another culture still uses that choice.

## Why Invariant Culture?

A worker parsing the same decimal text on two machines can interpret it differently if each machine supplies a different current culture. Setting an invariant default removes that dependency for operations that use the current culture.

Number formatting and parsing then use invariant conventions, including `.` as the decimal separator. Date formatting and parsing also use invariant conventions, but the setting does not select a time zone. Culture-sensitive string comparisons and sorting use invariant rules; they do not become ordinal comparisons. Serialization still follows each serializer's contract and explicit options.

## Configuration

For an `ArcApplication` console service or worker, set both default thread cultures before building the application:

```csharp
using System.Globalization;
using Cratis.Arc;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = ArcApplication.CreateBuilder(args);
builder.AddCratisArc();

var app = builder.Build();
app.UseCratisArc();

await app.RunAsync();
```

For a generic .NET host built with `IHostBuilder`, `UseInvariantCulture()` sets the same defaults in one call. This fragment builds the host; it does not start it or activate Arc HTTP endpoints:

```csharp
using Cratis.Arc;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .UseInvariantCulture()
    .Build();
```

## What Gets Configured

Both approaches set:

- `CultureInfo.DefaultThreadCurrentCulture` to `CultureInfo.InvariantCulture`
- `CultureInfo.DefaultThreadCurrentUICulture` to `CultureInfo.InvariantCulture`

These properties supply defaults for threads, including background and thread-pool threads, that have not been assigned a culture explicitly. They do not override an explicit culture set by application code.

## When to Use

Use invariant defaults for workers and backend data processing when machine-local formatting is not part of the input or output contract. This is useful when the same service runs on machines with different regional settings.

If a service accepts localized user input or produces localized output, choose that culture explicitly at the parsing or formatting boundary. An invariant process default does not replace the user's locale, a time-zone decision, or an explicit serialization contract.

## ASP.NET Core Applications

For an ASP.NET Core application, use its `WebApplicationBuilder` extension instead. That setup also configures request localization middleware; follow [Invariant Culture for ASP.NET Core](../asp-net-core/invariant-culture.md) rather than applying this host's setup to web requests.
