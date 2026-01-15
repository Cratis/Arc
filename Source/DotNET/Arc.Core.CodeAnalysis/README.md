# Arc Code Analyzers

This directory contains Roslyn analyzers for Arc projects to enforce correct usage of Arc constructs.

## Projects

### Arc.Core.CodeAnalysis
**Assembly Name:** `Cratis.Arc.Core.CodeAnalysis`

Provides static code analysis for Arc.Core constructs.

#### Analyzer Rules

- **ARC001** (Error): Incorrect Command handler signature
  - Command types with `[Command]` attribute must have a `Handle` method that returns `void`, `Task`, or `Task<TResult>`
  
- **ARC002** (Error): Incorrect Query method signature on ReadModel
  - Query methods on types with `[ReadModel]` attribute must return the ReadModel type, a collection, `Task`, `IAsyncEnumerable`, or `ISubject` of the ReadModel type

- **ARC003** (Warning): Missing `[Command]` attribute on command-like type
  - Types with `Handle` methods and properties should have the `[Command]` attribute

### Chronicle.CodeAnalysis
**Assembly Name:** `Cratis.Arc.Chronicle.CodeAnalysis`

Provides static code analysis for Chronicle constructs.

#### Analyzer Rules

- **ARC005** (Error): Incorrect AggregateRoot event handler signature
  - Event handler methods (typically named `On`) on AggregateRoot types must accept an event parameter and optionally an `EventContext` parameter, and return `void` or `Task`
  - Valid signatures:
    - `void On(TEvent @event)`
    - `Task On(TEvent @event)`
    - `void On(TEvent @event, EventContext context)`
    - `Task On(TEvent @event, EventContext context)`

## Usage

The analyzers are automatically included when you reference:
- `Arc.Core` → automatically includes `Arc.Core.CodeAnalysis`
- `Chronicle` → automatically includes `Chronicle.CodeAnalysis`

No additional configuration is required. The analyzers will run automatically during build and provide diagnostics in your IDE.

## Examples

### Correct Command Handler

```csharp
[Command]
public class CreateUser
{
    public string Name { get; set; }
    public string Email { get; set; }

    // ✅ Correct - void return type
    public void Handle()
    {
        // Command logic
    }
}
```

### Incorrect Command Handler (ARC001)

```csharp
[Command]
public class CreateUser
{
    public string Name { get; set; }
    
    // ❌ ARC001 Error - must return void, Task, or Task<T>
    public string Handle()
    {
        return "not allowed";
    }
}
```

### Correct ReadModel Query

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

### Incorrect ReadModel Query (ARC002)

```csharp
[ReadModel]
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    // ❌ ARC002 Error - must return User, IEnumerable<User>, Task<User>, etc.
    public static string GetName(Guid id)
    {
        return "name";
    }
}
```

### Correct AggregateRoot Event Handler

```csharp
public class UserAggregateRoot : AggregateRoot
{
    // ✅ Correct - void On(TEvent)
    public void OnUserCreated(UserCreated @event)
    {
    }

    // ✅ Correct - Task On(TEvent)
    public Task OnUserNameChanged(UserNameChanged @event)
    {
        return Task.CompletedTask;
    }

    // ✅ Correct - void On(TEvent, EventContext)
    public void OnUserDeactivated(UserDeactivated @event, EventContext context)
    {
    }
}
```

### Incorrect AggregateRoot Event Handler (ARC005)

```csharp
public class UserAggregateRoot : AggregateRoot
{
    // ❌ ARC005 Error - wrong return type
    public string OnUserCreated(UserCreated @event)
    {
        return "not allowed";
    }

    // ❌ ARC005 Error - Task<T> return type not allowed
    public Task<int> OnUserNameChanged(UserNameChanged @event)
    {
        return Task.FromResult(42);
    }
}
```
