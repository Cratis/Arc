---
title: Naming policies
description: Configure MongoDB collection and member names before persisting data.
---

Naming policies control collection and BSON member names. Configure them once before writing data; changing a policy does not rename existing fields or collections. Examples are policy declarations or startup/query fragments for the [configured host](./getting-started.md), not complete programs. Import `Cratis.Serialization` for `INamingPolicy` and `Cratis.Arc.MongoDB` for builder customization.

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

You can create your own naming policy by implementing `INamingPolicy`. The interface has
two members: `GetPropertyName` transforms a member name, and `GetReadModelName` transforms
a type into a collection name:

```csharp
public class SnakeCaseNamingPolicy : INamingPolicy
{
    public string GetPropertyName(string name) => ToSnakeCase(name);

    public string GetReadModelName(Type readModelType) => ToSnakeCase(readModelType.Name);

    static string ToSnakeCase(string name)
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

Register your custom naming policy by passing an instance to `WithNamingPolicy`:

```csharp
builder.UseCratisMongoDB(configureMongoDB: builder => 
    builder.WithNamingPolicy(new SnakeCaseNamingPolicy()));
```

This also works when an application-defined policy requires constructor parameters. `CustomNamingPolicy` in this illustrative fragment is your own implementation, not an Arc type:

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

You can create policies that combine transformations. `PrefixNamingPolicy` below is an application-defined implementation, not a built-in Arc policy. This example intentionally applies property transformations to the simple type name for collections; it does not compose each policy's `GetReadModelName` behavior:

```csharp
public class CompoundNamingPolicy : INamingPolicy
{
    private readonly INamingPolicy[] _policies;
    
    public CompoundNamingPolicy(params INamingPolicy[] policies)
    {
        _policies = policies;
    }
    
    public string GetPropertyName(string name) =>
        _policies.Aggregate(name, (current, policy) => policy.GetPropertyName(current));

    public string GetReadModelName(Type readModelType) =>
        _policies.Aggregate(readModelType.Name, (current, policy) => policy.GetPropertyName(current));
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
    public string GetPropertyName(string name)
    {
        // Don't transform ID fields
        if (name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            return name.ToLower();
            
        // Use camel case for everything else
        return char.ToLower(name[0]) + name[1..];
    }

    public string GetReadModelName(Type readModelType) =>
        char.ToLower(readModelType.Name[0]) + readModelType.Name[1..];
}
```

## Convention Pack Integration

The naming policy is implemented as a MongoDB convention pack, which means:

### Illustrative registration

The namespace filter below illustrates an application-specific convention registration; `MyApp.Models` is **not** Arc's automatic startup filter. Arc registers this convention using the configured convention-pack filters. Do not add this second registration after Arc initialization; customize Arc through the filtering mechanism below instead.

```csharp
// Illustrative application filter, not Arc's automatic registration
ConventionRegistry.Register(
    NamingPolicyNameConvention.ConventionName,
    new ConventionPack { new NamingPolicyNameConvention() },
    type => type.Namespace?.StartsWith("MyApp.Models", StringComparison.Ordinal) == true
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

A `DefaultNamingPolicy` is installed automatically. If custom builder code explicitly clears the policy, validation throws `NamingPolicyNotConfigured`:

```shell
NamingPolicyNotConfigured: A naming policy for MongoDB has not been configured.
Please configure it using the WithNamingPolicy method.
```

### Null or Empty Names

Naming policies should handle edge cases:

```csharp
public string GetPropertyName(string name)
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

```jsonc
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
builder.WithNamingPolicy(new SnakeCaseNamingPolicy());

// For JavaScript/JSON APIs expecting camelCase  
builder.WithCamelCaseNamingPolicy();
```

### Performance Considerations

Naming policies are called for every property during serialization setup, so keep them efficient:

```csharp
// Good: Simple, efficient transformation
public string GetPropertyName(string name) => 
    char.ToLower(name[0]) + name[1..];

// Avoid: Complex regex or multiple string operations
public string GetPropertyName(string name) => 
    Regex.Replace(name, "([A-Z])", "_$1").ToLower().Trim('_');
```

## Next Steps

- Learn about [Class Mapping](class-mapping.md) for custom type configurations
- Explore [Convention Packs](convention-packs.md) for advanced customization
- Understand [Concepts](concepts.md) and how they work with naming policies
