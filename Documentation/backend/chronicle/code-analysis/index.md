# Chronicle Code Analysis

Chronicle includes Roslyn analyzers that provide compile-time validation of Chronicle constructs to help catch errors early and enforce best practices.

## Overview

The Chronicle.CodeAnalysis project (`Cratis.Arc.Chronicle.CodeAnalysis`) provides static code analysis for:
- AggregateRoot event handler (On method) signatures

The analyzers are automatically included when you reference Chronicle - no additional configuration is required.

## Analyzer Rules

### ARCCHR0001 - Incorrect AggregateRoot Event Handler Signature

**Severity:** Error

Event handler methods (typically named `On`) on AggregateRoot types must accept an event parameter and optionally an `EventContext` parameter, and return `void` or `Task`.

**Valid signatures:**
- `void On(TEvent @event)`
- `Task On(TEvent @event)`
- `void On(TEvent @event, EventContext context)`
- `Task On(TEvent @event, EventContext context)`

**Example - Correct:**

```csharp
public class UserAggregateRoot : AggregateRoot
{
    // ✅ Correct - void On(TEvent)
    public void OnUserCreated(UserCreated @event)
    {
        // Handle event
    }

    // ✅ Correct - Task On(TEvent)
    public Task OnUserNameChanged(UserNameChanged @event)
    {
        // Handle event asynchronously
        return Task.CompletedTask;
    }

    // ✅ Correct - void On(TEvent, EventContext)
    public void OnUserDeactivated(UserDeactivated @event, EventContext context)
    {
        // Handle event with context
    }

    // ✅ Correct - Task On(TEvent, EventContext)
    public Task OnUserReactivated(UserReactivated @event, EventContext context)
    {
        // Handle event asynchronously with context
        return Task.CompletedTask;
    }
}
```

**Example - Incorrect:**

```csharp
public class UserAggregateRoot : AggregateRoot
{
    // ❌ ARCCHR0001 Error - wrong return type
    public string OnUserCreated(UserCreated @event)
    {
        return "not allowed";
    }

    // ❌ ARCCHR0001 Error - Task<T> return type not allowed
    public Task<int> OnUserNameChanged(UserNameChanged @event)
    {
        return Task.FromResult(42);
    }

    // ❌ ARCCHR0001 Error - too many parameters
    public void OnUserUpdated(UserUpdated @event, EventContext context, string extra)
    {
        // Only event and optionally EventContext are allowed
    }
}
```

## Event Handler Discovery

Chronicle automatically discovers event handler methods in your aggregate roots based on:
- Methods named starting with `On`
- Instance methods (not static)
- Methods with 1-2 parameters where the first is an event type

The analyzer validates that these discovered methods have correct signatures to prevent runtime errors.

## How to Use

The analyzers work automatically when you reference Chronicle in your project:

```xml
<ItemGroup>
    <ProjectReference Include="../Chronicle/Chronicle.csproj" />
</ItemGroup>
```

The analyzers will:
- Run during build
- Provide real-time diagnostics in your IDE
- Show errors in the Error List
- Prevent builds when errors are present (unless suppressed)

## Best Practices

1. **Use descriptive event handler names** - While methods just need to start with `On`, use meaningful names like `OnUserCreated` rather than just `On`.

2. **Keep handlers focused** - Each event handler should handle one type of event and perform focused state updates.

3. **Use async when needed** - If your event handler needs to perform asynchronous operations, use `Task` return type. Otherwise, use `void` for simplicity.

4. **Leverage EventContext** - When you need access to event metadata (timestamp, causation, correlation), include the `EventContext` parameter.
