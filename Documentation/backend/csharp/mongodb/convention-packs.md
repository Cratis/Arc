---
title: Apply MongoDB convention packs in Cratis Arc
description: Apply shared BSON mapping rules in Cratis Arc, filter them by document type, and handle missing or null collections on MongoDB read models.
---

Use MongoDB convention packs when several document types need the same BSON mapping rule, such as storing enums as strings or using a naming policy. Cratis Arc discovers your pack providers and type filters during [MongoDB setup](./getting-started.md), so you can define the rule once and decide which types it affects.

## What are Convention Packs?

A convention pack groups rules that the MongoDB driver applies when it builds a class map, the definition of how a .NET type is stored as BSON. Use a [class map](./class-mapping.md) for one document type's exceptions and a convention pack for a rule shared by several types.

These registrations are process-wide. Choose them before serialization begins, and use type filters when a rule should apply only to part of the model. They are not per-tenant or per-request settings.

The examples below are provider and type declarations or fragments for a configured host, not independent programs. Import `Cratis.Arc.MongoDB`, `MongoDB.Bson`, `MongoDB.Bson.Serialization`, `MongoDB.Bson.Serialization.Conventions`, and `MongoDB.Bson.Serialization.Serializers` as needed. Examples with application types such as `BaseDocument` need your own declarations.

## Built-in Convention Packs

Arc registers the following packs during setup. You do not need to add them yourself.

### Naming Policy Convention

Applies your configured naming policy to all property names. The `RegisterConventionAsPack` calls in this built-in section describe private Arc initialization; they are not public APIs to call from application code:

```csharp
// Registered automatically with name: "Naming policy based name convention"
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

#### Why it exists — a separate serialization boundary

A non-nullable collection declaration tells C# callers they can use `.Any()` or `.Select()` without a null check. Stored documents may still omit that field or contain an explicit `null`, causing those calls to fail at runtime.

This convention handles both stored shapes when the MongoDB driver reads a type marked `[ReadModel]`. It works for standalone Arc read models; Chronicle is not required. Rules applied by a JSON serializer do not configure BSON reads through `IMongoCollection<T>`, so this behavior belongs at the MongoDB serialization boundary.

#### What it covers

An absent field and an explicit null need different handling:

| Stored shape | Mechanism | Result for a non-nullable member |
| --- | --- | --- |
| The field is **absent** from the document | a default value on the member map | an empty collection |
| The field is present and holds **`null`** | a serializer wrapping the member's own | an empty collection |

The driver does not call a member's serializer for an absent field, and it does not consult a default value for a field that is present. The convention therefore supplies both a default and a serializer wrapper.

#### What it deliberately leaves alone

The convention leaves these members and types to their normal mapping:

- Nullable collections such as `IEnumerable<T>?`, which can distinguish null from empty.
- Dictionaries, including `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>`, and `Dictionary<TKey, TValue>`.
- Strings, even though `string` implements `IEnumerable<char>`.
- Types without `[ReadModel]`.

It changes reads only. A collection that is null in memory is still written as BSON null.

#### Supported collection shapes

`IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `List<T>`,
`ISet<T>`, `IReadOnlySet<T>`, `HashSet<T>`, and single-dimension arrays `T[]` are supported. Interface-typed members receive a `List<T>`, or a `HashSet<T>` for set interfaces. Other collection types keep their normal driver behavior because the convention needs to create an empty value assignable to the declared member type.

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

For an absent or null field, the convention's empty value replaces a property initializer. A member initialized with
`public IEnumerable<Child> Children { get; set; } = [];` receives an empty `List<Child>`. If an initializer supplies meaningful non-empty data, use one of the opt-outs above to retain that default.

Register the integration before deserializing any documents. The driver freezes class maps when first used, so registering the convention afterward cannot update an already frozen map.

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
                new DateTimeUtcConvention()
            }
        );
    }
}
```

## Filtering Conventions

Use a filter when a pack should apply only to selected document types. For example, keep a legacy type's stored names unchanged while applying the naming policy to new domain models. A pack applies to a type only when every filter includes it.

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

For fine-grained control, use `IgnoreConventions` on specific types. This filters the packs that participate in Arc's convention filtering; it does not disable global BSON serializers, ordinary driver conventions, or Arc's deliberately unfiltered derived-type discriminator convention.

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
    public const string ConventionName = "Naming policy based name convention";
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
        if ((memberName == "CreatedAt" || memberName == "UpdatedAt") &&
            memberMap.MemberType == typeof(DateTimeOffset))
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

The date convention above only targets `DateTimeOffset` members; attaching that serializer to a `DateTime` member is incompatible. The default representation loses offsets and submillisecond precision; see [serializers](./serializers.md).

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

The `RequiredFieldConvention` example controls omission on **writes**; it does not validate commands or require a field on **reads**. Use explicit validation and, where appropriate, a required BSON member contract for those separate requirements.

## Registration and Lifecycle

### Automatic Discovery

Arc discovers convention pack providers and filters during setup and instantiates both with `Activator.CreateInstance`. Give them usable parameterless constructors rather than constructor-injected services. The following sketch describes discovery; it is not additional startup code:

```csharp
// This happens during UseCratisMongoDB()
var types = Types.Instance;
var providers = types.FindMultiple<ICanProvideMongoDBConventionPacks>();
var filters = types.FindMultiple<ICanFilterMongoDBConventionPacksForType>();

// Providers and filters are registered automatically
```

### Registration Order

Arc registers supplied packs before its built-in naming, extra-element, and read-model collection packs, then registers discovered class maps. Later member conventions can overwrite earlier choices, including choices made in a class map's `Configure` method. Filter conflicting packs or use `IgnoreConventions` rather than relying on provider discovery order.

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

This illustrative fragment assumes your application supplies `ExpensiveCheck(Type)`. Cache only process-stable decisions, never tenant/request authorization decisions:

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

After registering the integration and your providers, inspect the member maps to check which conventions applied. These test fragments assume your application's `DomainModel`, `IgnoredConventionsModel`, and test framework are available:

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
public class DateTimeUtcConvention : ConventionBase, IMemberMapConvention
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
public class MegaConvention : ConventionBase, IMemberMapConvention
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

Use [class mapping](class-mapping.md) when a rule belongs to one document type rather than a group. If the storage names or values still differ from what you expect, check [naming policies](naming-policies.md) and [serializers](serializers.md). For typed IDs and names that should be stored as primitive values, see [concepts](concepts.md).
