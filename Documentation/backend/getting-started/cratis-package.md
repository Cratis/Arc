# Cratis Package

The `Cratis` NuGet package provides a simplified setup for building applications with Arc and Chronicle event sourcing. It combines Arc's application framework with Chronicle's event sourcing capabilities in a single, streamlined package.

## What is the Cratis Package?

The Cratis package is a convenience package that bundles:

- **Arc Application Framework** - CQRS patterns, validation, multi-tenancy, and more
- **Chronicle Event Sourcing** - Event store, aggregates, projections, and event handling
- **Swagger/OpenAPI** - Automatic API documentation generation

It's designed to get you up and running quickly with a complete event-sourced application stack without manually configuring each component.

## Installation

Add the Cratis package to your ASP.NET Core project:

```bash
dotnet add package Cratis
```

## Basic Setup

Configure Cratis in your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Cratis with default configuration
builder.AddCratis();

var app = builder.Build();

// Use Cratis middleware and endpoints
app.UseCratis();

app.Run();
```

This minimal setup configures:

- Arc's command and query infrastructure
- Chronicle's event store and event handling
- Swagger/OpenAPI documentation
- Validation and model binding
- Multi-tenancy support

## Advanced Configuration

You can customize both Arc and Chronicle components using the optional configuration callbacks:

```csharp
builder.AddCratis(
    configureArcOptions: options =>
    {
        // Configure Arc options
        options.UseControllerBasedRouting = true;
    },
    configureArcBuilder: arcBuilder =>
    {
        // Configure additional Arc features
        arcBuilder.WithMongoDB();
    },
    configureArcChronicleOptions: options =>
    {
        // Configure Chronicle options
        options.ClusterName = "my-cluster";
    },
    configureChronicleBuilder: chronicleBuilder =>
    {
        // Configure Chronicle features
    });
```

## Adding MongoDB Support

By default, the Cratis package doesn't include MongoDB support. To use MongoDB with your application, add the MongoDB package separately:

```bash
dotnet add package Cratis.Applications.MongoDB
```

Then configure MongoDB using the `WithMongoDB` extension method:

```csharp
builder.AddCratis(
    configureArcBuilder: arcBuilder =>
    {
        arcBuilder.WithMongoDB();
    });
```

### MongoDB Configuration Options

You can customize MongoDB settings using the configuration callback:

```csharp
builder.AddCratis(
    configureArcBuilder: arcBuilder =>
    {
        arcBuilder.WithMongoDB(
            configureOptions: options =>
            {
                options.Server = "mongodb://localhost:27017";
                options.Database = "my-database";
            });
    });
```

### MongoDB Configuration from appsettings.json

Alternatively, configure MongoDB settings in `appsettings.json`:

```json
{
  "MongoDB": {
    "Server": "mongodb://localhost:27017",
    "Database": "my-database"
  }
}
```

The `WithMongoDB` extension automatically reads these settings from the configuration section.

### Custom Configuration Section Path

If your MongoDB settings are in a different configuration section:

```csharp
arcBuilder.WithMongoDB(
    mongoDBConfigSectionPath: "MyApp:Database:MongoDB");
```

## Next Steps

Now that you have Cratis set up, you can:

- Define [Commands](../commands/) to handle user actions
- Create [Queries](../queries/) to retrieve data
- Build [Aggregates](../chronicle/aggregates/) to model your domain
- Configure [MongoDB](../mongodb/) for read models and projections
- Set up [Multi-tenancy](../multi-tenancy.md) for your application

For more advanced scenarios, explore the individual Arc and Chronicle components in the documentation.
