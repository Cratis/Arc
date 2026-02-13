# ARCCHR0001: Incorrect aggregate root event handler signature

## Rule

Event handler methods (typically named `On`) on aggregate roots must accept an event parameter and optionally an `EventContext` parameter, and return `void` or `Task`.

## Severity

Error

## Allowed Signatures

```csharp
void On(TEvent @event)
Task On(TEvent @event)
void On(TEvent @event, EventContext context)
Task On(TEvent @event, EventContext context)
```

## Example

### Violation

```csharp
public class UserAggregateRoot : AggregateRoot
{
    // ARCCHR0001: Invalid return type
    public string OnUserCreated(UserCreated @event)
    {
        return "not allowed";
    }

    // ARCCHR0001: Task<T> is not allowed
    public Task<int> OnUserNameChanged(UserNameChanged @event)
    {
        return Task.FromResult(42);
    }

    // ARCCHR0001: Too many parameters
    public void OnUserUpdated(UserUpdated @event, EventContext context, string extra)
    {
    }
}
```

### Fix

```csharp
public class UserAggregateRoot : AggregateRoot
{
    public void OnUserCreated(UserCreated @event)
    {
        // Handle event.
    }

    public Task OnUserNameChanged(UserNameChanged @event)
    {
        return Task.CompletedTask;
    }

    public void OnUserUpdated(UserUpdated @event, EventContext context)
    {
        // Handle event with context.
    }
}
```

## Why This Rule Exists

Chronicle discovers event handler methods based on naming and signature conventions. Standardized signatures ensure:
- Handlers are discovered consistently.
- Event processing remains predictable.
- Asynchronous handlers integrate cleanly with the runtime.

## Related Rules

- None

