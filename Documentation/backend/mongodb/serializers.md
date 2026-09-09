---
title: BSON serializers
description: Understand Arc's BSON representations, precision limits, and serializer registration.
---

Cratis Applications provides a comprehensive set of custom serializers for MongoDB to handle common .NET types that don't have built-in MongoDB support or need special handling.

## Built-in Serializers

The following serializers are registered by `WithMongoDB()` / `UseCratisMongoDB()`. The model and custom serializer snippets on this page are illustrative declarations/fragments for the [configured host](./getting-started.md), not independent programs.

### DateTimeOffset Support

**Class**: `DateTimeOffsetSupportingBsonDateTimeSerializer`

By default, writes BSON DateTime using `ToUnixTimeMilliseconds()` and reads with `DateTimeOffset.FromUnixTimeMilliseconds()`. This preserves the instant only to millisecond precision: **the original UTC offset and submillisecond ticks are lost**, and the restored value has offset zero. Store a separate offset/time-zone field if your domain needs it.

```csharp
public class MyDocument
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; }
}
```

The serializer supports different BSON representations:

- `BsonType.DateTime` (default)
- `BsonType.String`

`Int64` is not accepted as a representation, even though BSON DateTime internally holds milliseconds. Other representations throw.

The current string format constant is `YYYY-MM-ddTHH:mm:ss.FFFFFFK`, not the standard round-trip `O` format. Uppercase `YYYY` is not a .NET year specifier, and only six fractional positions are included. Do not use this mode as a lossless offset/precision workaround; test legacy compatibility and use an application-owned serializer if a different storage contract is required.

### DateOnly Serializer

**Class**: `DateOnlySerializer`

Handles .NET 6+ `DateOnly` types, storing them efficiently in MongoDB:

```csharp
public class EventRecord
{
    public DateOnly EventDate { get; set; }
    public string Description { get; set; }
}
```

### TimeOnly Serializer

**Class**: `TimeOnlySerializer`

Handles .NET 6+ `TimeOnly` types for time-of-day values:

```csharp
public class Schedule
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
```

### TimeSpan Serializer

**Class**: `TimeSpanSerializer`

Handles serialization of `TimeSpan` values:

```csharp
public class Task
{
    public TimeSpan Duration { get; set; }
    public TimeSpan EstimatedTime { get; set; }
}
```

### Geospatial Serializers

Cratis provides specialized serializers for geospatial types from `Cratis.Geospatial`. Their writers produce GeoJSON; query filters use the driver's GeoJSON argument types. The current Polygon reader fails on the nested coordinate-pair arrays its writer emits: see the [Polygon read limitation](./geospatial/polygon.md#current-read-limitation) before relying on typed query materialization.

For comprehensive documentation on storing and querying geographic data, see the [Geospatial Types](./geospatial/) section, which covers:

- **[Point](./geospatial/point.md)** — Single coordinates for locations and landmarks
- **[LineString](./geospatial/linestring.md)** — Routes, paths, and trajectories
- **[Polygon](./geospatial/polygon.md)** — Geographic areas and boundaries with optional exclusion zones

### Type Serializer

**Class**: `TypeSerializer`

Serializes `System.Type` instances, useful for polymorphic scenarios or when storing type information:

```csharp
public class TypedDocument
{
    public Type DocumentType { get; set; }
    public object Data { get; set; }
}
```

## Guid Configuration

One of the most important default configurations is for `System.Guid`. MongoDB historically used a legacy GUID representation that could cause issues. Cratis Applications configures Guids to use the standard representation:

```csharp
// This is done automatically during setup
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
```

This is a description of Arc startup, **not an additional registration to paste after it**. BSON Guid values use Standard UUID binary representation. Existing legacy UUID data may need an explicit compatibility/migration strategy; changing registration does not rewrite stored values.

## Custom Serializers

You can register additional serializers if needed:

### Using MongoDB's Registration

```csharp
BsonSerializer.RegisterSerializer(new MyCustomSerializer());
```

### Using Serialization Providers

For more complex scenarios, implement `IBsonSerializationProvider`:

```csharp
public class MySerializationProvider : IBsonSerializationProvider
{
    public IBsonSerializer GetSerializer(Type type)
    {
        if (type == typeof(MyCustomType))
        {
            return new MyCustomTypeSerializer();
        }
        
        return null;
    }
}

// Register the provider
BsonSerializer.RegisterSerializationProvider(new MySerializationProvider());
```

## Serializer Configuration

Some serializers support configuration through interfaces. MongoDB's serializer registry is process-wide and caches registrations; do not register a second global serializer for a type Arc has already registered. For a specific member, set a compatible serializer in its class map before the map freezes.

### Representation Configurable

Serializers implementing `IRepresentationConfigurable<T>` can be configured for different BSON representations:

```csharp
// Representation selection only; see the string-format limitation above.
var serializer = new DateTimeOffsetSupportingBsonDateTimeSerializer()
    .WithRepresentation(BsonType.String);
```

## Polymorphic Serialization

For complex inheritance hierarchies, the framework includes custom discriminator handling:

### Custom Object Discriminator Convention

The `CustomObjectDiscriminatorConvention` provides better handling of polymorphic types by using more readable type strings instead of .NET's default assembly-qualified names.

```csharp
public abstract class BaseDocument
{
    public string Id { get; set; }
}

public class TextDocument : BaseDocument
{
    public string Content { get; set; }
}

public class ImageDocument : BaseDocument
{
    public byte[] ImageData { get; set; }
}
```

The discriminator will use simplified type names making the stored documents more readable and portable.

## Performance Considerations

### Serializer Caching

MongoDB serializers are cached by type, so there's no performance penalty for using custom serializers once they're registered.

### Concept Serializers

The [Concept serializers](concepts.md) are optimized to serialize only the underlying value, not the wrapper object, providing efficient storage and retrieval.

## Error Handling

Validation and null handling are serializer-specific; BSON serialization is not comprehensive domain or geometry validation.

- **Geometry**: The current Point, LineString, and Polygon serializers dereference null values when writing and do not accept explicit BSON null when reading. Nullable-reference annotations do not add a null-aware serializer.
- **Optional geometry**: Design and test an omission policy or a null-aware member serializer. For an ordinary nullable property with no initializer or required-member mapping, a missing field leaves it null. An omission setting such as `[BsonIgnoreIfNull]` skips writing that null member, but does **not** make reading an existing explicit BSON null safe. Test your actual class maps and stored data before adopting either policy.
- **Concepts**: Nullable [concept serialization](./concepts.md#serialization-and-validation) is separately supported; `ConceptSerializer<T>` explicitly writes and reads BSON null.
- **Geometry validation**: Records and write paths do not validate geometry. The Polygon reader has ring-count/closure checks, but also the [current read limitation](./geospatial/polygon.md#current-read-limitation).

## Next Steps

- Learn about [Concept serialization](concepts.md) for domain-driven design
- Explore [Class Mapping](class-mapping.md) for custom type mapping
- Configure [Naming Policies](naming-policies.md) for consistent field naming
