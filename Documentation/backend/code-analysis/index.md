# Arc.Core Code Analysis

Arc.Core includes Roslyn analyzers that provide compile-time validation of Arc constructs to help catch errors early and enforce best practices.

## Overview

The Arc.Core.CodeAnalysis project (`Cratis.Arc.Core.CodeAnalysis`) provides static code analysis for:
- Command handler signatures
- ReadModel query method signatures

The analyzers are automatically included when you reference Arc.Core - no additional configuration is required.

## Analyzer Rules

### ARC0001 - Incorrect Query Method Signature on ReadModel

**Severity:** Error

Query methods on types with `[ReadModel]` attribute must return the ReadModel type, a collection, `Task`, `IAsyncEnumerable`, or `ISubject` of the ReadModel type.

**Valid return types:**
- `ReadModel`
- `IEnumerable<ReadModel>`, `List<ReadModel>`, `ReadModel[]`
- `Task<ReadModel>`
- `Task<IEnumerable<ReadModel>>`
- `IAsyncEnumerable<ReadModel>`
- `ISubject<ReadModel>`
- `ISubject<IEnumerable<ReadModel>>`

**Example - Correct:**

```csharp
[ReadModel]
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    // ✅ Correct - returns the ReadModel type
    public static User GetById(Guid id) => new();
    
    // ✅ Correct - returns collection of ReadModel
    public static IEnumerable<User> GetAll() => [];
    
    // ✅ Correct - returns Task<ReadModel>
    public static Task<User> GetByIdAsync(Guid id) => Task.FromResult(new User());
}
```

**Example - Incorrect:**

```csharp
[ReadModel]
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    // ❌ ARC0001 Error - must return User, IEnumerable<User>, Task<User>, etc.
    public static string GetName(Guid id)
    {
        return "name";
    }
}
```

### ARC0002 - Missing [Command] Attribute on Command-like Type

**Severity:** Warning

Types with `Handle` methods and properties should have the `[Command]` attribute to be recognized as commands.

**Example - Missing Attribute:**

```csharp
// ⚠️ ARC0002 Warning - has Handle method and properties but missing [Command] attribute
public class CreateUser
{
    public string Name { get; set; }
    
    public void Handle()
    {
        // Should have [Command] attribute
    }
}
```

**Example - Correct:**

```csharp
[Command]
public class CreateUser
{
    public string Name { get; set; }
    public string Email { get; set; }

    // ✅ Correct - has [Command] attribute
    public void Handle()
    {
        // Command logic
    }
}
```

```csharp
[Command]
public class CreateUser
{
    public string Name { get; set; }
    
    // ✅ Correct - can return a result
    public UserCreatedResult Handle()
    {
        return new UserCreatedResult { UserId = Guid.NewGuid() };
    }
}
```

## How to Use

The analyzers work automatically when you reference Arc.Core in your project:

```xml
<ItemGroup>
    <ProjectReference Include="../Arc.Core/Arc.Core.csproj" />
</ItemGroup>
```

The analyzers will:
- Run during build
- Provide real-time diagnostics in your IDE
- Show errors and warnings in the Error List
- Prevent builds when errors are present (unless suppressed)
