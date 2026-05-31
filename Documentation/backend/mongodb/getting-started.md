# Getting Started

MongoDB integration in Cratis Applications is designed to be simple to set up while providing powerful defaults that work out of the box.

## Basic Setup

The simplest way to add MongoDB support to your application is through the configuration extension methods provided for `WebApplicationBuilder` or `HostBuilder`.

### With WebApplicationBuilder

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc();

builder.UseCratisMongoDB();

var app = builder.Build();
app.UseCratisArc();
```

### With HostBuilder

```csharp
var host = Host.CreateDefaultBuilder(args)
    .AddCratisArc()
    .UseCratisMongoDB()
    .Build();
```

## What Gets Configured

When you call `UseCratisMongoDB()`, the following components are automatically configured:

### Default Serializers

- **DateTimeOffset**: Proper handling of timezone information
- **DateOnly**: .NET 6+ date-only types
- **TimeOnly**: .NET 6+ time-only types  
- **System.Type**: Serialization of .NET type information
- **Guid**: Configured to use the standard .NET representation instead of MongoDB's legacy format
- **Cratis Concepts**: Automatic serialization for all types implementing `ConceptAs<T>`

### Convention Packs

- **Naming Policy Convention**: Applies your configured naming policy to all properties
- **Ignore Extra Elements**: Ignores unknown properties during deserialization

### Automatic Discovery

- **Class Maps**: All implementations of `IBsonClassMapFor<T>` are automatically discovered and registered
- **Convention Pack Providers**: All implementations of `ICanProvideMongoDBConventionPacks` are discovered
- **Convention Pack Filters**: All implementations of `ICanFilterMongoDBConventionPacksForType` are discovered

## Configuration Options

You can customize the MongoDB setup by providing configuration options:

```csharp
builder.UseCratisMongoDB(configureMongoDB: mongoBuilder =>
{
    mongoBuilder
        .WithCamelCaseNamingPolicy()
        .WithServerResolver<MyCustomServerResolver>()
        .WithDatabaseNameResolver<MyCustomDatabaseNameResolver>();
});
```

## Connection Configuration

The framework uses resolver patterns for determining connection details:

### Server Connection

Implement `IMongoServerResolver` to provide connection string logic:

```csharp
public class MyServerResolver : IMongoServerResolver
{
    public MongoUrl GetMongoUrl()
    {
        return new MongoUrl("mongodb://localhost:27017");
    }
}
```

### Database Name

Implement `IMongoDatabaseNameResolver` to provide database naming logic:

```csharp
public class MyDatabaseNameResolver : IMongoDatabaseNameResolver
{
    public string GetDatabaseName()
    {
        return "MyApplicationDatabase";
    }
}
```

## Sane Defaults

### Guid Representation

One of the most common pain points when working with MongoDB and .NET is Guid serialization. By default, MongoDB stores Guids using a legacy binary format that can cause issues. Cratis Applications configures Guids to use the standard .NET representation, making them more predictable and interoperable.

### Error Handling

The framework includes sensible error handling for common configuration issues:

- Missing server resolver configuration
- Missing database name resolver configuration  
- Missing naming policy configuration

These will throw descriptive exceptions with guidance on how to fix the configuration.

## Next Steps

- Learn about [Serializers](serializers.md) for custom type handling
- Explore [Concepts](concepts.md) for domain-driven design patterns
- Configure [Naming Policies](naming-policies.md) for consistent property naming
- Set up [Class Mapping](class-mapping.md) for complex type mappings
- Implement [Convention Packs](convention-packs.md) for advanced customization
