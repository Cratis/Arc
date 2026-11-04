---
title: JSON conversion
description: Store explicitly marked EF properties as JSON with separate conversion options.
---

Mark a complex property with `[Json]` to store its value as a JSON document. `BaseDbContext` applies the conversion during model creation; an ordinary `DbContext` needs an explicit `ApplyJsonConversion` call, as shown below.

## What it does

These model/startup fragments extend the host from [getting started](./getting-started.md); they are not independent programs.

With conversion active, Arc serializes each marked property's value when saving and deserializes it to the declared type when loading. The configured JSON converters handle the types within that value, including Cratis concepts. The attribute selects which values use this conversion path.

## Why it's important

A JSON column keeps an object or collection together as a document rather than modeling its members as separate rows. Choose this representation around how you load and use that value. Changes to the application's types still need a plan for existing documents, and queries into the document depend on provider support.

## Model Usage

Apply `[Json]` to the properties you want stored as documents. This fragment uses an application-defined `CustomerPreferences`; supply that declaration and the model's initialization in your application:

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

Each marked property is stored as its own JSON value. Nested objects and collections are serialized as part of that value, using the configured converters; the default converters below include concept support.

The selected column types are PostgreSQL `jsonb`, SQL Server `nvarchar(max)`, and SQLite `text`. This is value conversion, not a promise that arbitrary nested-property LINQ expressions translate to server-side JSON operators. Test provider query support explicitly. JSON options are separate from the plain serializer used by `AsPoint()` / `AsLineString()` / `AsPolygon()`; do not apply both paths to one property.

## Default Converters

`JsonConversionOptions` starts with the following Arc default JSON converters. Check the types they handle before deciding whether your property needs an additional converter:

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

An interface or abstract type needs a way to select and construct a concrete value during deserialization. Supply a custom `JsonConverter` when the configured type handling does not cover your property's shape. Without suitable handling, `JsonSerializer` can report `NotSupportedException: Deserialization of interface or abstract types is not supported`.

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

The entity below has an interface-typed `[Json]` property. The registered `MyRequestConverter`, implemented in the next section, constructs its concrete `DoThing` value:

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

This converter reads `count` and constructs `DoThing`; writing it emits `Count` under the same JSON property name. To store additional concrete request types, extend the selection logic deliberately rather than assuming this converter distinguishes them.

## Manual Configuration

For a context that does not derive from [`BaseDbContext`](./base-db-context.md), apply JSON conversion in `OnModelCreating`. This fragment uses the same application model as above. In addition to the JSON namespace below, import `Cratis.Arc.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore`.

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

Pass a `JsonConversionOptions` instance directly to include custom converters in this manual setup:

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

`JsonConversion.ApplyJsonConversion()` selects the marked properties from the supplied entity types and attaches their value converters and comparers to the EF model. The converters use the configured serialization options to write and read JSON; the comparers use those same options for change tracking. This configures model conversion, not database schema creation.
