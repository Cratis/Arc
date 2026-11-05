---
title: Configure invariant culture in Cratis Arc
description: Use invariant culture defaults in an ASP.NET Core Arc host so culture-sensitive parsing and formatting do not depend on machine settings.
---

Cratis Arc's `UseInvariantCulture()` extensions set invariant culture defaults for an ASP.NET Core host and its requests. Use them when culture-sensitive parsing and formatting should follow the same conventions across machines, rather than each machine's regional settings. Explicit culture choices in your code can still override those defaults.

## Why Invariant Culture?

A service can move between machines with different regional settings. If its code uses the current culture implicitly, the same date or number text can be interpreted differently after deployment.

Invariant culture gives that code a common default: number formatting uses `.` as the decimal separator, and culture-sensitive date formatting and parsing use invariant conventions. It does not choose a time zone. Comparisons that use the current culture use invariant rules; this does not turn them into ordinal comparisons.

Serialization still follows the serializer's contract and explicit options. In particular, JSON numeric syntax is already culture-independent.

## Configuration

In an ASP.NET Core web project with `Cratis.Arc`, import `Cratis.Arc`. Call `UseInvariantCulture()` on the `WebApplicationBuilder` before building the application, and on the resulting `WebApplication` to activate the request localization middleware:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.UseInvariantCulture();
builder.AddCratisArc();

var app = builder.Build();

app.UseInvariantCulture();
app.UseCratisArc();

await app.RunAsync();
```

Place `app.UseInvariantCulture()` before middleware that parses or formats request data. Middleware that runs earlier will not have the request culture set by this extension.

## What Gets Configured

Calling `UseInvariantCulture()` on the builder:

- Sets `CultureInfo.DefaultThreadCurrentCulture` to `CultureInfo.InvariantCulture`
- Sets `CultureInfo.DefaultThreadCurrentUICulture` to `CultureInfo.InvariantCulture`
- Configures `RequestLocalizationOptions` to use only `InvariantCulture`
- Removes all request culture providers, preventing clients from overriding the culture via `Accept-Language` headers or query strings

Calling `UseInvariantCulture()` on the application:

- Registers the ASP.NET Core request localization middleware with the invariant culture settings

## Practical Example

For a price supplied as text, the meaning of a comma depends on the culture used to parse it. Setting invariant defaults removes the host's regional settings from that decision. A numeric JSON `price` is different: its wire format is defined by JSON, not request localization. This complete ASP.NET `Program.cs` example shows where to configure culture alongside such an endpoint:

```csharp
using Cratis.Arc;

var builder = WebApplication.CreateBuilder(args);

builder.UseInvariantCulture();
builder.AddCratisArc();

var app = builder.Build();

app.UseInvariantCulture();
app.UseCratisArc();

app.MapPost("/products", (ProductRequest request) =>
{
    // JSON binding uses the serializer's numeric contract
    return Results.Ok(new { Price = request.Price });
});

await app.RunAsync();

record ProductRequest(decimal Price, DateOnly ExpiryDate);
```

## When to Use

Choose invariant defaults for services that exchange machine-readable data or run across machines with different regional settings. If an operation intentionally reads or produces localized text, pass the required culture explicitly instead of relying on the host default.

For MVC inputs assembled from several HTTP sources, continue with [FromRequest binding](from-request.md). Culture configuration and binding precedence solve different problems; check both when defining the endpoint's input contract.
