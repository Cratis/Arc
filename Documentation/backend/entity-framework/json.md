# JSON Conversion

The JSON conversion feature provides automatic serialization and deserialization of complex objects to JSON format in the database. This cross-database approach allows you to store rich data structures while maintaining compatibility across different database providers.

## What it does

The JSON conversion automatically configures Entity Framework Core to handle properties marked with the `[Json]` attribute:

1. **Serialization**: Complex objects are automatically serialized to JSON when saving to the database
2. **Deserialization**: JSON data is automatically converted back to objects when loading from the database
3. **Cross-Provider Compatibility**: Uses the most appropriate JSON storage method for each database provider
4. **Concept Support**: Includes automatic support for Cratis concepts through the integrated converter factory

This automatic configuration ensures that complex data structures are properly stored and retrieved while maintaining type safety and database compatibility.

## Why it's important

Using JSON conversion provides several key benefits:

- **Rich Data Storage**: Store complex objects and collections without creating separate tables
- **Schema Flexibility**: Easily evolve object structures without complex database migrations
- **Cross-Database Compatibility**: Consistent JSON handling across different database providers
- **Performance**: Reduces the need for complex joins when loading related data
- **Type Safety**: Maintains strong typing while providing flexible storage

## Model Usage

Your entity models can use complex properties by marking them with the `[Json]` attribute:

```csharp
using Cratis.Arc.EntityFrameworkCore.Json;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    [Json]
    public IEnumerable<Address> Addresses { get; set; }

    [Json]
    public CustomerPreferences Preferences { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}
```

The conversion will automatically:

- Store collections and complex objects as JSON in the database
- Handle serialization of nested objects and collections
- Support Cratis concepts within the JSON data
- Maintain type safety when loading the data back

## Default Converters

`JsonConversionOptions` is pre-populated with all Arc default JSON converters so that common types work
out of the box without any extra configuration:

| Converter | Handles |
| --------- | ------- |
| `ConceptAsJsonConverterFactory` | Cratis `ConceptAs<T>` value types |
| `EnumerableConceptAsJsonConverterFactory` | `IEnumerable<ConceptAs<T>>` sequences |
| `EnumConverterFactory` | Enum values (serialised as integers) |
| `DateOnlyJsonConverter` | `System.DateOnly` |
| `TimeOnlyJsonConverter` | `System.TimeOnly` |
| `TypeJsonConverter` | `System.Type` |
| `UriJsonConverter` | `System.Uri` |
| `EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory` | Enumerable model-with-id to concept/primitive |
| `DerivedTypeJsonConverterFactory` | Polymorphic types registered via `IDerivedTypes` (interfaces, abstract base classes) |

## Registering Custom Converters

Some property types require a custom `JsonConverter` to round-trip correctly — for example, interface types, abstract base classes, or discriminated unions. Without a matching converter, `JsonSerializer` throws `NotSupportedException: Deserialization of interface or abstract types is not supported`.

Register additional converters at startup through the `JsonConverters` list on `EntityFrameworkCoreOptions`. The converters are appended after the built-in Arc defaults:

```csharp
builder.AddCratisArc(configureBuilder: arcBuilder =>
{
    arcBuilder.WithEntityFrameworkCore(options =>
    {
        options.ConnectionString = "...";
        options.JsonConverters.Add(new MyRequestConverter());
    });
});
```

With the converter registered, an entity with an interface-typed `[Json]` property works correctly:

```csharp
public interface IMyRequest { int Count { get; } }
public sealed record DoThing(int Count) : IMyRequest;

public class MyEntity
{
    [Key] public Guid Id { get; set; }
    [Json] public IMyRequest Request { get; set; } = default!;  // round-trips correctly
}
```

### Writing a Custom Converter

Implement `JsonConverter<T>` where `T` is the interface or abstract type that needs to be handled:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

public class MyRequestConverter : JsonConverter<IMyRequest>
{
    public override IMyRequest? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        // Use a discriminator or well-known property to pick the concrete type
        var count = doc.RootElement.GetProperty("count").GetInt32();
        return new DoThing(count);
    }

    public override void Write(
        Utf8JsonWriter writer, IMyRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("count", value.Count);
        writer.WriteEndObject();
    }
}
```

## Manual Configuration

If you're not using the [`BaseDbContext`](./base-db-context.md), you can manually apply JSON conversion in your `DbContext`:

```csharp
using Cratis.Arc.EntityFrameworkCore.Json;

public class StoreDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes();
        modelBuilder.ApplyJsonConversion(entityTypes, Database.GetDatabaseType());
        base.OnModelCreating(modelBuilder);
    }
}
```

To include custom converters in a manual setup, pass a `JsonConversionOptions` instance directly:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    var jsonOptions = new JsonConversionOptions();
    jsonOptions.JsonSerializerOptions.Converters.Add(new MyRequestConverter());
    var entityTypes = modelBuilder.Model.GetEntityTypes();
    modelBuilder.ApplyJsonConversion(entityTypes, Database.GetDatabaseType(), jsonOptions);
    base.OnModelCreating(modelBuilder);
}
```

> Note: When using [`BaseDbContext`](./base-db-context.md) with `WithEntityFrameworkCore`, the `JsonConversionOptions` singleton is resolved automatically from DI — you only need to populate `EntityFrameworkCoreOptions.JsonConverters` at startup.

## How it works

The conversion system uses reflection to:

1. Identify all properties in your entities that are marked with the `[Json]` attribute
2. Create appropriate value converters that handle serialization and deserialization
3. Configure value comparers for proper change tracking
4. Apply these converters to the Entity Framework model builder

The conversion is handled by the `JsonConversion.ApplyJsonConversion()` extension method, which automatically discovers and configures all JSON properties in your model.

