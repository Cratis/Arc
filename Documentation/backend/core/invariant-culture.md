# Invariant Culture

Arc.Core provides a `.UseInvariantCulture()` extension method on `IHostBuilder` that configures the application to use invariant culture throughout, ensuring consistent and predictable behavior regardless of the host machine's regional settings.

## Why Invariant Culture?

Applications running in distributed environments—different machines, containers, or cloud regions—may be configured with different regional settings. Without explicit culture configuration, operations like number parsing, date formatting, or string comparisons may produce inconsistent results depending on where the code runs.

Using invariant culture guarantees:

- **DateTime formatting and parsing** behaves identically everywhere
- **Number formatting and parsing** produces consistent results (e.g., decimal separator is always `.`)
- **String comparisons and sorting** are culture-independent
- **Serialization and deserialization** never depends on machine locale

## Configuration

Call `UseInvariantCulture()` on the `IHostBuilder` before building the application:

```csharp
using Cratis.Arc;

var builder = ArcApplication.CreateBuilder(args);

builder.UseInvariantCulture();
builder.AddCratisArc();

var app = builder.Build();
app.UseCratisArc();

await app.RunAsync();
```

## What Gets Configured

Calling `UseInvariantCulture()` on the builder:

- Sets `CultureInfo.DefaultThreadCurrentCulture` to `CultureInfo.InvariantCulture`
- Sets `CultureInfo.DefaultThreadCurrentUICulture` to `CultureInfo.InvariantCulture`

This ensures that all threads in the application, including background threads and thread pool threads, use invariant culture by default.

## When to Use

Invariant culture is recommended for:

- **Background services and workers** that process data without a web request context
- **Applications deployed in multiple regions** or cloud environments
- **Data processing pipelines** where consistency is critical
- **Any scenario** where culture-sensitive behavior could lead to bugs or data inconsistencies

It is generally safe to apply invariant culture in back-end services, as locale-specific formatting should be handled on the client side rather than in the service itself.

## ASP.NET Core Applications

If you are building an ASP.NET Core application, use the `WebApplicationBuilder` extension instead, which additionally configures the ASP.NET Core request localization middleware. See [Invariant Culture for ASP.NET Core](../asp-net-core/invariant-culture.md) for details.
