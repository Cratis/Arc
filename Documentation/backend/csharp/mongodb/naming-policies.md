---
title: Configure MongoDB collection and field names
description: Configure how Cratis Arc names MongoDB collections and BSON fields, and keep queries aligned with stored names when using built-in or custom policies.
---

Cratis Arc uses a naming policy to choose MongoDB collection names and field names in BSON, MongoDB's document format. Set the policy before writing data so reads and writes use the same names. Changing it later does not rename stored fields or collections; plan that change as a data migration.

The examples extend the [configured host](./getting-started.md). Import `Cratis.Serialization` for naming policies and `Cratis.Arc.MongoDB` for builder customization. Custom policy excerpts focus on the MongoDB transformations; they are not complete programs.

## Overview

An `INamingPolicy` supplies two MongoDB naming decisions: `GetReadModelName` selects a collection name from a type, and `GetPropertyName` selects each mapped field name. Arc's `database.GetCollection<T>()` extension uses the collection name; the naming convention applies field names during class mapping.

## Default Behavior

The default policy leaves field names unchanged and pluralizes collection names. For example, `UserAccount` uses the `UserAccounts` collection, with the property names shown below. Pass `pluralizeReadModelNames: false` when constructing `DefaultNamingPolicy` if the collection should keep the singular type name.

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

Use `WithCamelCaseNamingPolicy()` when stored names should start with a lowercase letter. It converts both property and collection names, so `UserAccount` uses `userAccounts`. Collection names remain plural unless you pass `pluralizeReadModels: false`.

```csharp
builder.UseCratisMongoDB(configureMongoDB: builder => 
    builder.WithCamelCaseNamingPolicy());
```

The properties then use these stored field names:

```csharp
public class UserAccount
{
    public string UserName { get; set; }       // Stored as: "userName"
    public string EmailAddress { get; set; }   // Stored as: "emailAddress"
    public DateTime CreatedDate { get; set; }  // Stored as: "createdDate"
}
```

## Custom Naming Policies

Implement `INamingPolicy` when the built-in policies do not match your stored data. The excerpt below converts property names and the simple type name to snake case. Unlike the built-in policies, this `GetReadModelName` implementation does not pluralize the collection name.

The interface also exposes `JsonPropertyNamingPolicy`. The custom policy excerpts on this page omit that member to focus on MongoDB naming; supply it in your implementation, using `null` if no JSON property policy is needed. The snake-case excerpt also needs `System.Text` for `StringBuilder`.

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

Passing an instance lets you supply constructor arguments too. In this fragment, `CustomNamingPolicy` is an application-defined policy that accepts a prefix and suffix:

```csharp
var namingPolicy = new CustomNamingPolicy(prefix: "app_", suffix: "_v1");

builder.UseCratisMongoDB(configureMongoDB: builder => 
    builder.WithNamingPolicy(namingPolicy));
```

## How Naming Policies Work

### Convention Integration

Arc registers `NamingPolicyNameConvention` as a convention pack. For each member the MongoDB driver maps, the convention passes its element name to your policy's `GetPropertyName`. Convention pack filters and ignore attributes determine which types receive that convention.

### Property Name Transformation

The policy works on mapped members, including public properties and any fields included in the class map. Embedded documents and objects inside arrays use their own class maps, so their field names follow the policy when those types receive the convention too.

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

Use convention filtering when some document types must keep an existing storage format. Filtering the field-name convention does not change the collection name chosen by `GetReadModelName`.

### Illustrative registration

This fragment shows how the MongoDB driver registers a convention with a namespace filter. Arc already registers its naming convention, so do not add this second registration after initialization. `MyApp.Models` is only an example namespace; customize Arc's registration with [convention pack filters](convention-packs.md#filtering-conventions).

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

To leave a type's field names unchanged by the policy, use the `IgnoreConventions` attribute. This does not change the collection name:

```csharp
[IgnoreConventions(NamingPolicyNameConvention.ConventionName)]
public class LegacyDocument
{
    public string UserName { get; set; }     // Stored as: "UserName" (unchanged)
    public string EmailAddr { get; set; }    // Stored as: "EmailAddr" (unchanged)
}
```

## Impact on Queries

Expression-based filters use the mapped property name, keeping the query aligned with your policy. When supplying a stored field name as a string, use the name in MongoDB, such as `userName` below.

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

Decide how a custom policy handles empty input before applying a transformation that indexes the first character. This method fragment returns blank names unchanged; `TransformName` represents your transformation:

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

Choose names that match your existing documents or the format you intend to store. Mixing conventions makes string-based queries harder to maintain:

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

If another system reads the same MongoDB documents, agree on the stored field names before choosing a policy. These builder fragments select snake case or camel case; they do not configure an HTTP API's JSON serialization:

```csharp
// For systems expecting snake_case
builder.WithNamingPolicy(new SnakeCaseNamingPolicy());

// For JavaScript/JSON APIs expecting camelCase  
builder.WithCamelCaseNamingPolicy();
```

### Performance Considerations

The driver applies field-name transformations while setting up class maps. Keep that work simple where it fits your naming rules. The following method fragments contrast a first-character conversion with a regex transformation:

```csharp
// Good: Simple, efficient transformation
public string GetPropertyName(string name) => 
    char.ToLower(name[0]) + name[1..];

// Avoid: Complex regex or multiple string operations
public string GetPropertyName(string name) => 
    Regex.Replace(name, "([A-Z])", "_$1").ToLower().Trim('_');
```

## Next Steps

Use [Class Mapping](class-mapping.md) when a type needs explicit member configuration, or [Convention Packs](convention-packs.md) to decide which types receive conventions. For strongly typed values stored inside documents, continue with [Concepts](concepts.md).
