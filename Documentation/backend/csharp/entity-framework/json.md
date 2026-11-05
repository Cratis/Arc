---
title: Entity Framework Core JSON property conversion
description: Store marked Entity Framework Core properties as JSON with Cratis Arc, using provider-specific column types and configurable JSON converters.
---

Cratis Arc's JSON conversion stores a complex property or collection in one database column rather than mapping it to separate tables. Mark the property with `[Json]` and apply the conversion through `BaseDbContext` or `ApplyJsonConversion`; Arc serializes the value on save and deserializes it on load.

## What it does

Arc adds a value converter and a value comparer to each marked property. The converter handles JSON storage; the comparer uses serialized values for equality and creates snapshots for EF change tracking. The default JSON options include converters for Cratis concepts, so domain value types can also appear inside the stored object.

The model and startup fragments below extend the host from [getting started](./getting-started.md); they are not independent programs.

## Why it's important

Use JSON conversion when the application loads and saves a property as a whole, such as customer preferences or a collection of addresses. You keep that data together without defining a table for each nested type.

The stored JSON is still a data contract. When you change an object's shape or its converters, check how existing rows will deserialize and whether they need a data migration. If you need to filter or join on nested values, verify the provider's query support before choosing JSON storage.

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

Here, `Addresses` and `Preferences` each occupy a JSON-converted column. Nested objects and collections use the same JSON options, including the default Cratis concept converters.

Arc selects PostgreSQL `jsonb`, SQL Server `nvarchar(max)`, and SQLite `TEXT`. These are provider-specific column mappings, not equivalent server-side JSON query capabilities. A value converter alone does not translate arbitrary nested-property LINQ expressions into JSON operators.

The JSON options are separate from the plain serializer used by `AsPoint()`, `AsLineString()`, and `AsPolygon()`. Do not apply both conversion paths to one property; use the [geometry storage-path matrix](./point-conversion.md#choose-one-storage-path) to choose a representation.

## Default Converters

`JsonConversionOptions` starts with these converters:

| Converter | Handles |
| --------- | ------- |
| `ComplexKeyDictionaryJsonConverterFactory` | Dictionaries with complex keys |
| `PointJsonConverter`, `LineStringJsonConverter`, `PolygonJsonConverter` | Cratis geometry in GeoJSON format |
| `ConceptAsJsonConverterFactory` | Cratis `ConceptAs<T>` value types |
| `EnumerableConceptAsJsonConverterFactory` | `IEnumerable<ConceptAs<T>>` sequences |
| `EnumConverterFactory` | Enum values (serialized as integers) |
| `DateOnlyJsonConverter` | `System.DateOnly` |
| `TimeOnlyJsonConverter` | `System.TimeOnly` |
| `TypeJsonConverter` | `System.Type` |
| `UriJsonConverter` | `System.Uri` |
| `EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory` | Enumerable model-with-id to concept/primitive |
| `DerivedTypeJsonConverterFactory` | Added when `IDerivedTypes` is supplied; handles registered polymorphic types |

## Registering Custom Converters

If a property's declared type does not tell `JsonSerializer` which concrete type to construct, supply a matching converter or polymorphic type configuration. Interface and abstract types can otherwise fail to deserialize with `NotSupportedException`. The example below uses a converter to read an interface-typed property back as a concrete record.

Register additional converters at startup through the `JsonConverters` list on `EntityFrameworkCoreOptions`. The converters are appended after the built-in Arc defaults:

```csharp
builder.AddCratisArc(configureBuilder: arcBuilder =>
{
    arcBuilder.WithEntityFrameworkCore(options =>
    {
        options.ConnectionString = "Data Source=store.db";
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
    [Json] public required IMyRequest Request { get; set; }
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

If you're not using [`BaseDbContext`](./base-db-context.md), apply JSON conversion in `OnModelCreating`. In addition to the JSON namespace below, import `Cratis.Arc.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore`.

```csharp
using Cratis.Arc.EntityFrameworkCore.Json;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

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

When you use [`BaseDbContext`](./base-db-context.md) with `WithEntityFrameworkCore`, the context resolves the `JsonConversionOptions` singleton from dependency injection. Populate `EntityFrameworkCoreOptions.JsonConverters` at startup; you do not need to pass the options into model configuration yourself.

## How it works

`JsonConversion.ApplyJsonConversion()` inspects the supplied entity types for `[Json]` properties and annotated constructor parameters. It configures the corresponding properties with value converters, serialized-value comparers, and column types for the selected provider.

For context-wide conventions, continue with [BaseDbContext](./base-db-context.md). If a property holds geometry, compare [string conversion, JSON properties, and spatial column declarations](./point-conversion.md#choose-one-storage-path) before writing its migration.
