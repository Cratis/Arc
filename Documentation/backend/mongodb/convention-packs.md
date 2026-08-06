# Convention Packs

Convention packs provide a powerful way to apply consistent configuration across all your MongoDB class mappings. Cratis Applications includes a comprehensive system for creating, providing, and filtering convention packs.

## What are Convention Packs?

Convention packs are collections of conventions that MongoDB applies automatically to class maps during registration. They allow you to:

- **Apply consistent rules**: Ensure all classes follow the same patterns
- **Reduce boilerplate**: Avoid repeating configuration in every class map
- **Conditional application**: Apply different rules to different types
- **System-wide changes**: Modify behavior across your entire application

## Built-in Convention Packs

Cratis Applications automatically registers several convention packs:

### Naming Policy Convention

Applies your configured naming policy to all property names:

```csharp
// Registered automatically with name: "Naming policy convention"
RegisterConventionAsPack(
    conventionPackFilters, 
    NamingPolicyNameConvention.ConventionName, 
    new NamingPolicyNameConvention()
);
```

### Ignore Extra Elements

Ignores unknown properties during deserialization:

```csharp
// Registered automatically with name: "Ignore extra elements convention"
RegisterConventionAsPack(
    conventionPackFilters, 
    ConventionPacks.IgnoreExtraElements, 
    new IgnoreExtraElementsConvention(true)
);
```

### Read Model Collections Never Null

Materializes a `[ReadModel]`'s declared **non-nullable** collection members as empty collections instead of `null`:

```csharp
// Registered automatically with name: "Read model collections never null convention",
// scoped to types marked with [ReadModel]
RegisterConventionAsPack(
    conventionPackFilters,
    ConventionPacks.ReadModelCollectionsNeverNull,
    new ReadModelCollectionsNeverNullConvention(),
    type => type.IsReadModel()
);
```

#### Why it exists — the driver is a separate boundary from Chronicle

A read model that declares a child collection as a non-nullable `IEnumerable<T>` promises the type system that the
value is there. The store is free to disagree. Chronicle's read model sink writes **no field at all** for a child
collection that has never had an element, and can write an **explicit `null`** for one whose last element went away.
Nullable reference analysis has already concluded the member can never be `null`, so nothing warns, and an unguarded
`.Any()` / `.All()` / `.Select()` throws at runtime.

Chronicle closes this for **its own reader** with a `JsonTypeInfo` modifier on its client's `JsonSerializerOptions`.
That fix lives at Chronicle's serialization boundary and **does not reach the MongoDB driver**. Reading a read model
through `IMongoCollection<T>` — the sanctioned way to query by anything other than the key, and what Arc's own
server-side paging runs on — goes through the driver instead. This convention is the same guarantee, restated where
the driver can honor it.

#### What it covers

Both shapes a store can leave behind, through two different mechanisms:

| Stored shape | Mechanism | Result for a non-nullable member |
|---|---|---|
| The field is **absent** from the document | a default value on the member map | an empty collection |
| The field is present and holds **`null`** | a serializer wrapping the member's own | an empty collection |

Both halves are needed: the driver never invokes a member's serializer for an element that is not in the document, and
a default value is never consulted for one that is.

#### What it deliberately leaves alone

- **Nullable collections.** A member declared `IEnumerable<T>?` keeps the distinction between "no collection" and "an
  empty collection", because that model asked for it. Absent stays `null`; a stored `null` stays `null`.
- **Dictionaries.** `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>`, and `Dictionary<TKey, TValue>`
  are untouched.
- **`string`.** It is an `IEnumerable<char>`, and the classic trap here would be handing it an empty list.
- **Types without `[ReadModel]`.** The scoping is what keeps this from redefining what `null` means for every BSON
  type in the process.
- **Writing.** Serialization is unchanged — a collection that is `null` in memory is still written as `null`.

#### Supported collection shapes

`IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `List<T>`,
`ISet<T>`, `IReadOnlySet<T>`, `HashSet<T>`, and single-dimension arrays `T[]`. Interface-typed members are filled with
a `List<T>` (or a `HashSet<T>` for the set interfaces). Any other collection type is left as the driver would leave
it — the empty value is assigned straight into the member, so a shape that cannot be constructed safely is not guessed
at.

#### Opting out

Per type, through the standard mechanism:

```csharp
[IgnoreConventions(ConventionPacks.ReadModelCollectionsNeverNull)]
[ReadModel]
public record RawProjection(string Id, IEnumerable<Child> Children);
```

Per member, by stating the default the member wants — the convention skips any member carrying a
`[BsonDefaultValue]`, for an absent element and a stored `null` alike:

```csharp
[ReadModel]
public record Parent(string Id, [property: BsonDefaultValue(null)] IEnumerable<Child> Children);
```

#### Things to know

- **The default replaces whatever a property initializer set.** A member written as
  `public IEnumerable<Child> Children { get; set; } = [];` still comes back empty, but as a `List<Child>` rather than
  the `Child[]` the collection expression produced. A member whose initializer sets a **meaningful non-empty** default
  would be silently emptied — use one of the opt-outs above for that member.
- **Registration order matters.** A class map freezes its conventions the first time it is built, so this is
  registered during `AddCratisMongoDB`, before anything can deserialize.

## Creating Convention Pack Providers

To provide your own convention packs, implement `ICanProvideMongoDBConventionPacks`:

```csharp
public interface ICanProvideMongoDBConventionPacks
{
    IEnumerable<MongoDBConventionPackDefinition> Provide();
}
```

### Example Provider

```csharp
public class CustomConventionPackProvider : ICanProvideMongoDBConventionPacks
{
    public IEnumerable<MongoDBConventionPackDefinition> Provide()
    {
        // Read-only conventions
        yield return new MongoDBConventionPackDefinition(
            "ReadOnly Properties", 
            new ConventionPack
            {
                new ReadOnlyPropertiesConvention()
            }
        );
        
        // Enum string serialization
        yield return new MongoDBConventionPackDefinition(
            "Enum Conventions",
            new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            }
        );
        
        // Custom discriminator handling
        yield return new MongoDBConventionPackDefinition(
            "Custom Discriminator",
            new ConventionPack
            {
                new CustomDiscriminatorConvention(
                    CustomObjectDiscriminatorConvention.Instance,
                    GetTypesWithExistingDiscriminators()
                )
            }
        );
    }
    
    private static IEnumerable<Type> GetTypesWithExistingDiscriminators()
    {
        // Return types that already have discriminator configuration
        yield return typeof(BaseDocument);
        yield return typeof(AuditableEntity);
    }
}
```

### Advanced Convention Examples

```csharp
public class DomainConventionPackProvider : ICanProvideMongoDBConventionPacks
{
    public IEnumerable<MongoDBConventionPackDefinition> Provide()
    {
        // ID field conventions
        yield return new MongoDBConventionPackDefinition(
            "ID Field Conventions",
            new ConventionPack
            {
                new NamedIdMemberConvention("Id", "id", "_id"),
                new StringObjectIdIdGeneratorConvention()
            }
        );
        
        // Ignore null values
        yield return new MongoDBConventionPackDefinition(
            "Ignore Null Values",
            new ConventionPack
            {
                new IgnoreIfNullConvention(true)
            }
        );
        
        // Custom date handling
        yield return new MongoDBConventionPackDefinition(
            "Date Conventions",
            new ConventionPack
            {
                new DateTimeSerializationOptionsConvention(
                    DateTimeKind.Utc, 
                    BsonType.DateTime
                )
            }
        );
    }
}
```

## Filtering Conventions

You can control which types convention packs apply to using filters.

### ICanFilterMongoDBConventionPacksForType

Implement this interface to create custom filters:

```csharp
public interface ICanFilterMongoDBConventionPacksForType
{
    bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type);
}
```

### Example Filters

```csharp
public class DomainModelFilter : ICanFilterMongoDBConventionPacksForType
{
    public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
    {
        // Only apply naming conventions to domain models
        if (conventionPackName == NamingPolicyNameConvention.ConventionName)
        {
            return type.Namespace?.Contains("Domain.Models") == true;
        }
        
        return true;
    }
}

public class NoConventionsForDTOs : ICanFilterMongoDBConventionPacksForType
{
    public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
    {
        // Don't apply any conventions to DTOs
        if (type.Name.EndsWith("DTO") || type.Name.EndsWith("Dto"))
        {
            return false;
        }
        
        return true;
    }
}

public class LegacySystemFilter : ICanFilterMongoDBConventionPacksForType
{
    public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
    {
        // Don't apply naming conventions to legacy types
        if (conventionPackName == NamingPolicyNameConvention.ConventionName &&
            type.Namespace?.Contains("Legacy") == true)
        {
            return false;
        }
        
        return true;
    }
}
```

## IgnoreConventions Attribute

For fine-grained control, use the `IgnoreConventions` attribute on specific types:

### Ignore All Conventions

```csharp
[IgnoreConventions]
public class RawDocument
{
    public string _id { get; set; }          // Keep exact field names
    public string user_name { get; set; }    // No naming policy applied
    public object extra_data { get; set; }   // No serialization conventions
}
```

### Ignore Specific Conventions

```csharp
[IgnoreConventions(NamingPolicyNameConvention.ConventionName)]
public class ExactFieldNames
{
    public string UserName { get; set; }     // Stored as "UserName"
    public string EmailAddr { get; set; }    // Stored as "EmailAddr"
}

[IgnoreConventions(ConventionPacks.IgnoreExtraElements)]
public class StrictDocument
{
    public string Name { get; set; }
    // Will throw exception if extra fields are present during deserialization
}
```

### Multiple Ignore Attributes

```csharp
[IgnoreConventions(NamingPolicyNameConvention.ConventionName)]
[IgnoreConventions("Custom Enum Convention")]
public class SpecialDocument
{
    public string PropertyName { get; set; }    // No naming policy
    public MyEnum Status { get; set; }          // No enum convention
}
```

## Built-in Convention Pack Names

The framework defines constants for well-known convention pack names:

```csharp
public static class ConventionPacks
{
    public const string IgnoreExtraElements = "Ignore extra elements convention";
    public const string ReadModelCollectionsNeverNull = "Read model collections never null convention";
}

public class NamingPolicyNameConvention
{
    public const string ConventionName = "Naming policy convention";
}
```

## Advanced Convention Pack Examples

### Audit Field Conventions

```csharp
public class AuditConventionPackProvider : ICanProvideMongoDBConventionPacks
{
    public IEnumerable<MongoDBConventionPackDefinition> Provide()
    {
        yield return new MongoDBConventionPackDefinition(
            "Audit Fields",
            new ConventionPack
            {
                new AuditFieldConvention()
            }
        );
    }
}

public class AuditFieldConvention : ConventionBase, IMemberMapConvention
{
    public void Apply(BsonMemberMap memberMap)
    {
        var memberName = memberMap.MemberName;
        
        // Auto-configure audit fields
        if (memberName == "CreatedAt" || memberName == "UpdatedAt")
        {
            memberMap.SetSerializer(new DateTimeOffsetSupportingBsonDateTimeSerializer());
            
            if (memberName == "CreatedAt")
            {
                memberMap.SetIgnoreIfDefault(true);
            }
        }
        
        // Configure user audit fields
        if (memberName == "CreatedBy" || memberName == "UpdatedBy")
        {
            memberMap.SetIgnoreIfNull(true);
        }
    }
}
```

### Validation Conventions

```csharp
public class ValidationConventionPackProvider : ICanProvideMongoDBConventionPacks
{
    public IEnumerable<MongoDBConventionPackDefinition> Provide()
    {
        yield return new MongoDBConventionPackDefinition(
            "Required Fields",
            new ConventionPack
            {
                new RequiredFieldConvention()
            }
        );
    }
}

public class RequiredFieldConvention : ConventionBase, IMemberMapConvention
{
    public void Apply(BsonMemberMap memberMap)
    {
        var memberInfo = memberMap.MemberInfo;
        
        // Check for Required attribute
        if (memberInfo.GetCustomAttribute<RequiredAttribute>() != null)
        {
            memberMap.SetIgnoreIfDefault(false);
            memberMap.SetIgnoreIfNull(false);
        }
    }
}
```

## Registration and Lifecycle

### Automatic Discovery

Convention pack providers are automatically discovered during setup:

```csharp
// This happens during UseCratisMongoDB()
var types = Types.Instance;
var providers = types.FindMultiple<ICanProvideMongoDBConventionPacks>();
var filters = types.FindMultiple<ICanFilterMongoDBConventionPacksForType>();

// Providers and filters are registered automatically
```

### Registration Order

Convention packs are registered in the order they're provided. If multiple conventions affect the same aspect, later conventions may override earlier ones.

### Filter Application

For each convention pack, all filters are consulted:

```csharp
static bool ShouldInclude(
    IEnumerable<ICanFilterMongoDBConventionPacksForType> filters, 
    string conventionPackName, 
    IConventionPack conventionPack, 
    Type type)
{
    // All filters must return true for the convention pack to be applied
    return filters.All(filter => 
        filter.ShouldInclude(conventionPackName, conventionPack, type));
}
```

## Performance Considerations

### Filter Efficiency

Convention pack filters are called for every type, so keep them efficient:

```csharp
// Good: Simple, fast checks
public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
{
    return !type.Name.EndsWith("DTO");
}

// Avoid: Expensive operations
public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
{
    return !type.GetCustomAttributes().Any(attr => attr is DTOAttribute);
}
```

### Caching Results

Consider caching filter results for frequently-checked types:

```csharp
public class CachedFilter : ICanFilterMongoDBConventionPacksForType
{
    private static readonly ConcurrentDictionary<(string, Type), bool> _cache = new();
    
    public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
    {
        return _cache.GetOrAdd((conventionPackName, type), key => 
            ComputeShouldInclude(key.Item1, conventionPack, key.Item2));
    }
    
    private bool ComputeShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
    {
        // Expensive computation here
        return ExpensiveCheck(type);
    }
}
```

## Testing Convention Packs

You can test your convention packs to ensure they work correctly:

```csharp
[Test]
public void should_apply_naming_convention_to_domain_models()
{
    // Arrange
    var classMap = new BsonClassMap<DomainModel>();
    classMap.AutoMap();
    
    // Act
    classMap.ApplyConventions();
    
    // Assert
    var memberMap = classMap.GetMemberMap(m => m.PropertyName);
    memberMap.ElementName.ShouldEqual("propertyName");  // camelCase applied
}

[Test]
public void should_ignore_conventions_when_attribute_present()
{
    // Arrange
    var classMap = new BsonClassMap<IgnoredConventionsModel>();
    classMap.AutoMap();
    
    // Act
    classMap.ApplyConventions();
    
    // Assert
    var memberMap = classMap.GetMemberMap(m => m.PropertyName);
    memberMap.ElementName.ShouldEqual("PropertyName");  // No naming convention applied
}
```

## Best Practices

### Keep Conventions Simple

Each convention should have a single responsibility:

```csharp
// Good: Single purpose
public class DateTimeUtcConvention : IMemberMapConvention
{
    public void Apply(BsonMemberMap memberMap)
    {
        if (memberMap.MemberType == typeof(DateTime))
        {
            memberMap.SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
        }
    }
}

// Avoid: Multiple concerns
public class MegaConvention : IMemberMapConvention
{
    public void Apply(BsonMemberMap memberMap)
    {
        // Handles dates, strings, numbers, etc. - too complex
    }
}
```

### Use Descriptive Names

Convention pack names should clearly indicate their purpose:

```csharp
// Good: Clear naming
"Audit Field Conventions"
"Required Field Validation"
"Legacy System Compatibility"

// Avoid: Vague naming
"Custom Convention"
"Special Rules"
"Fixes"
```

### Document Filter Logic

Make filter logic clear and well-documented:

```csharp
public class ApiModelFilter : ICanFilterMongoDBConventionPacksForType
{
    /// <summary>
    /// Applies naming conventions only to API models (types ending with "ApiModel")
    /// and excludes internal types from convention processing.
    /// </summary>
    public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
    {
        if (conventionPackName == NamingPolicyNameConvention.ConventionName)
        {
            return type.Name.EndsWith("ApiModel") && !type.IsNotPublic;
        }
        
        return true;
    }
}
```

## Next Steps

- Learn about [Class Mapping](class-mapping.md) for type-specific configurations
- Explore [Concepts](concepts.md) for domain-driven design patterns
- Understand [Naming Policies](naming-policies.md) for consistent property naming
- Review [Serializers](serializers.md) for custom type handling
