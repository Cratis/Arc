---
title: "ARCCHR0001: Incorrect aggregate root event handler signature"
description: Aggregate root event handlers must follow one of the allowed On method signatures to be discovered.
---

## Rule

Event handler methods (typically named `On`) on aggregate roots must accept an event parameter and optionally an `EventContext` parameter, and return `void` or `Task`.

## Severity

Error

## Allowed signatures

These are signature fragments. The analyzer first filters candidates to methods with one or two parameters and a known event as the first parameter. A three-parameter method is outside that filter, so it does not receive ARCCHR0001 even though it is not a supported aggregate event handler. Verify discovery/replay in specs rather than relying on silence.

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

    // Not reported by ARCCHR0001: excluded before candidate validation
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

Arc's aggregate integration discovers event handlers from their event parameter and supported signature; names such as `On` are conventions. Standardized signatures ensure:

- Handlers are discovered consistently.
- Event processing remains predictable.
- Asynchronous handlers integrate cleanly with the runtime.

## Related Rules

- None
