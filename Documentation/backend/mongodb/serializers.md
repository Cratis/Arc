# Serializers

Cratis Applications provides a comprehensive set of custom serializers for MongoDB to handle common .NET types that don't have built-in MongoDB support or need special handling.

## Built-in Serializers

The following serializers are automatically registered when you call `UseCratisMongoDB()`:

### DateTimeOffset Support

**Class**: `DateTimeOffsetSupportingBsonDateTimeSerializer`

Provides proper serialization of `DateTimeOffset` values, preserving timezone information that would otherwise be lost with MongoDB's default DateTime handling.

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
- `BsonType.Int64`

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

Cratis provides specialized serializers for geospatial types from `Cratis.Geospatial`. These types follow the GeoJSON specification and are fully integrated with MongoDB's geospatial query operators.

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
BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;
BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
```

This ensures that:

- Guids are stored in a predictable format
- They work correctly with .NET applications
- There are no surprises when viewing data in MongoDB tools

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

Some serializers support configuration through interfaces:

### Representation Configurable

Serializers implementing `IRepresentationConfigurable<T>` can be configured for different BSON representations:

```csharp
// Configure DateTimeOffset to serialize as string
var serializer = new DateTimeOffsetSupportingBsonDateTimeSerializer()
    .WithRepresentation(BsonType.String);

BsonSerializer.RegisterSerializer(serializer);
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

Serializers include comprehensive error handling:

- **Type validation**: Ensures only appropriate types are serialized
- **Null handling**: Proper null value handling across all serializers
- **Format validation**: Validates input data before serialization

## Next Steps

- Learn about [Concept serialization](concepts.md) for domain-driven design
- Explore [Class Mapping](class-mapping.md) for custom type mapping
- Configure [Naming Policies](naming-policies.md) for consistent field naming
