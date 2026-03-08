# Invariant Culture

Arc provides a `.UseInvariantCulture()` extension method that configures your application to use invariant culture throughout, ensuring consistent and predictable behavior regardless of the host machine's regional settings.

## Why Invariant Culture?

Modern distributed applications run across different machines, containers, and cloud regions—each potentially configured with different regional settings. Without explicit culture configuration, operations like number parsing, date formatting, or string comparisons may produce inconsistent results depending on where the code runs.

Using invariant culture guarantees:

- **DateTime formatting and parsing** behaves identically everywhere
- **Number formatting and parsing** produces consistent results (e.g., decimal separator is always `.`)
- **String comparisons and sorting** are culture-independent
- **Serialization and deserialization** never depends on machine locale

## Configuration

Call `UseInvariantCulture()` on the `WebApplicationBuilder` before building the application, and on the resulting `WebApplication` to activate the request localization middleware:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.UseInvariantCulture();
builder.AddCratisArc();

var app = builder.Build();

app.UseInvariantCulture();
app.UseCratisArc();

await app.RunAsync();
```

> **Important**: Call `app.UseInvariantCulture()` before any middleware that processes request data to ensure invariant culture is applied to all incoming requests.

## What Gets Configured

Calling `UseInvariantCulture()` on the builder:

- Sets `CultureInfo.DefaultThreadCurrentCulture` to `CultureInfo.InvariantCulture`
- Sets `CultureInfo.DefaultThreadCurrentUICulture` to `CultureInfo.InvariantCulture`
- Configures `RequestLocalizationOptions` to use only `InvariantCulture`
- Removes all request culture providers, preventing clients from overriding the culture via `Accept-Language` headers or query strings

Calling `UseInvariantCulture()` on the application:

- Registers the ASP.NET Core request localization middleware with the invariant culture settings

## Practical Example

Consider an API that accepts a price value. Without invariant culture, `"1,5"` might parse as `1.5` on a European machine but fail on an English-locale machine. With invariant culture, parsing is always based on the standard `"1.5"` format:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.UseInvariantCulture();
builder.AddCratisArc();

var app = builder.Build();

app.UseInvariantCulture();

app.MapPost("/products", (ProductRequest request) =>
{
    // Price is always parsed using invariant culture rules
    return Results.Ok(new { Price = request.Price });
});

await app.RunAsync();

record ProductRequest(decimal Price, DateOnly ExpiryDate);
```

## When to Use

Invariant culture is recommended for:

- **APIs and services** that exchange data between systems
- **Applications deployed in multiple regions** or cloud environments
- **Data processing pipelines** where consistency is critical
- **Any scenario** where culture-sensitive behavior could lead to bugs or data inconsistencies

It is generally safe to apply invariant culture in back-end services, as the locale-specific formatting should be handled on the client side (e.g., a frontend application) rather than in the API itself.
