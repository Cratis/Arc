# Naming Policies

Naming policies in Cratis Applications control how collection names and property names are transformed when serializing to MongoDB. This ensures consistent naming conventions across your database.

## Overview

Collection names and their members are named based on a naming policy (`INamingPolicy`). The framework provides flexible configuration options and built-in policies for common scenarios.

## Default Behavior

The default naming policy does not alter the input, giving you the names exactly as defined in your types:

```csharp
public class UserAccount
{
    public string UserName { get; set; }       // Stored as: "UserName"
    public string EmailAddress { get; set; }   // Stored as: "EmailAddress"
    public DateTime CreatedDate { get; set; }  // Stored as: "CreatedDate"
}
```

## Built-in Naming Policies

### Camel Case Policy

The most common naming policy is camel case, which converts PascalCase property names to camelCase:

```csharp
builder.UseCratisMongoDB(configureMongoDB: builder => 
    builder.WithCamelCaseNamingPolicy());
```

With camel case policy:

```csharp
public class UserAccount
{
    public string UserName { get; set; }       // Stored as: "userName"
    public string EmailAddress { get; set; }   // Stored as: "emailAddress"
    public DateTime CreatedDate { get; set; }  // Stored as: "createdDate"
}
```

## Custom Naming Policies

You can create your own naming policy by implementing `INamingPolicy`:

```csharp
public class SnakeCaseNamingPolicy : INamingPolicy
{
    public string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
            
        var result = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                result.Append('_');
            result.Append(char.ToLower(name[i]));
        }
        return result.ToString();
    }
}
```

### Registering Custom Policies

You can register your custom naming policy in two ways:

#### By Type

```csharp
builder.UseCratisMongoDB(configureMongoDB: builder => 
    builder.WithNamingPolicy<SnakeCaseNamingPolicy>());
```

#### By Instance

If your naming policy requires constructor parameters:

```csharp
var namingPolicy = new CustomNamingPolicy(prefix: "app_", suffix: "_v1");

builder.UseCratisMongoDB(configureMongoDB: builder => 
    builder.WithNamingPolicy(namingPolicy));
```

## How Naming Policies Work

### Convention Integration

Naming policies are applied through the `NamingPolicyNameConvention`, which is automatically registered as a convention pack. This convention:

1. **Applies to all members**: Processes every property and field in your classes
2. **Uses configured policy**: Applies the naming policy you've configured
3. **Integrates with filtering**: Respects convention pack filters and ignore attributes

### Property Name Transformation

The convention applies to:

- **Public properties**: All public get/set properties
- **Public fields**: Public field members (if configured)
- **Nested objects**: Properties within embedded documents
- **Collection elements**: Properties of objects within arrays

## Configuration Examples

### Multiple Policies

You can create policies that combine multiple transformations:

```csharp
public class CompoundNamingPolicy : INamingPolicy
{
    private readonly INamingPolicy[] _policies;
    
    public CompoundNamingPolicy(params INamingPolicy[] policies)
    {
        _policies = policies;
    }
    
    public string ConvertName(string name)
    {
        return _policies.Aggregate(name, (current, policy) => policy.ConvertName(current));
    }
}

// Usage
var policy = new CompoundNamingPolicy(
    new CamelCaseNamingPolicy(),
    new PrefixNamingPolicy("data_")
);

builder.UseCratisMongoDB(configureMongoDB: builder => 
    builder.WithNamingPolicy(policy));
```

### Conditional Policies

Create policies that apply different rules based on the property name:

```csharp
public class ConditionalNamingPolicy : INamingPolicy
{
    public string ConvertName(string name)
    {
        // Don't transform ID fields
        if (name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            return name.ToLower();
            
        // Use camel case for everything else
        return char.ToLower(name[0]) + name[1..];
    }
}
```

## Convention Pack Integration

The naming policy is implemented as a MongoDB convention pack, which means:

### Automatic Application

```csharp
// This is registered automatically during setup
ConventionRegistry.Register(
    NamingPolicyNameConvention.ConventionName,
    new ConventionPack { new NamingPolicyNameConvention() },
    type => /* filter logic */
);
```

### Filtering Support

You can control which types the naming policy applies to using [convention pack filters](convention-packs.md#filtering-conventions):

```csharp
public class NoNamingPolicyForDTOs : ICanFilterMongoDBConventionPacksForType
{
    public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
    {
        if (conventionPackName == NamingPolicyNameConvention.ConventionName)
        {
            return !type.Name.EndsWith("DTO");
        }
        return true;
    }
}
```

## Ignore Naming Conventions

For specific types that shouldn't use naming policies, use the `IgnoreConventions` attribute:

```csharp
[IgnoreConventions(NamingPolicyNameConvention.ConventionName)]
public class LegacyDocument
{
    public string UserName { get; set; }     // Stored as: "UserName" (unchanged)
    public string EmailAddr { get; set; }    // Stored as: "EmailAddr" (unchanged)
}
```

## Impact on Queries

Remember that naming policies affect how you write queries:

### With Camel Case Policy

```csharp
// Property defined as: public string UserName { get; set; }
// Stored in MongoDB as: "userName"

// Query using the MongoDB field name
var filter = Builders<User>.Filter.Eq("userName", "john.doe");

// Or use expression trees (automatically converted)
var users = await collection
    .Find(u => u.UserName == "john.doe")  // Automatically converted to "userName"
    .ToListAsync();
```

## Error Handling

### Missing Policy Configuration

If no naming policy is configured, the framework will throw a descriptive error:

```shell
NamingPolicyNotConfigured: No naming policy has been configured. 
Use WithNamingPolicy<T>() or WithNamingPolicy(instance) to configure one.
```

### Null or Empty Names

Naming policies should handle edge cases:

```csharp
public string ConvertName(string name)
{
    if (string.IsNullOrWhiteSpace(name))
        return name;  // Return unchanged for invalid input
        
    // Your transformation logic
    return TransformName(name);
}
```

## Best Practices

### Consistency

Choose one naming convention and apply it consistently across your application:

```json
// Good: Consistent camelCase
{
    "userId": "123",
    "userName": "john",
    "createdAt": "2024-01-15T10:30:00Z"
}

// Avoid: Mixed conventions
{
    "userId": "123",
    "UserName": "john",
    "created_at": "2024-01-15T10:30:00Z"
}
```

### Consider External Systems

If you're integrating with external systems that expect specific naming conventions, align your policy accordingly:

```csharp
// For systems expecting snake_case
builder.WithNamingPolicy<SnakeCaseNamingPolicy>();

// For JavaScript/JSON APIs expecting camelCase  
builder.WithCamelCaseNamingPolicy();
```

### Performance Considerations

Naming policies are called for every property during serialization setup, so keep them efficient:

```csharp
// Good: Simple, efficient transformation
public string ConvertName(string name) => 
    char.ToLower(name[0]) + name[1..];

// Avoid: Complex regex or multiple string operations
public string ConvertName(string name) => 
    Regex.Replace(name, "([A-Z])", "_$1").ToLower().Trim('_');
```

## Next Steps

- Learn about [Class Mapping](class-mapping.md) for custom type configurations
- Explore [Convention Packs](convention-packs.md) for advanced customization
- Understand [Concepts](concepts.md) and how they work with naming policies
