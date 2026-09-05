# Command Context

The `CommandContext` is a core component of the non-controller-based command pipeline in Cratis Arc. It provides contextual information and values that are available throughout the command execution lifecycle.

## Overview

The `CommandContext` is a record that encapsulates all the necessary information about a command being executed:

```csharp
public record CommandContext(
    CorrelationId CorrelationId, 
    Type Type, 
    object Command, 
    IEnumerable<object?> Dependencies,
    CommandContextValues Values,
    ValidationResultSeverity? AllowedSeverity = default,
    object? Response = default,
    IServiceProvider? ServiceProvider = default,
    CancellationToken CancellationToken = default);
```

### Properties

- **CorrelationId**: A unique identifier for tracking the command execution across the system
- **Type**: The type of the command being executed
- **Command**: The actual command instance
- **Dependencies**: The resolved dependencies required to handle the command
- **Values**: A collection of key-value pairs providing additional context
- **AllowedSeverity**: The highest validation severity the caller allows before the command short-circuits
- **Response**: The response, **if any**, that is returned as part of the command result
- **ServiceProvider**: The scoped service provider used for command execution
- **CancellationToken**: The cancellation token for the command execution

## Command Context Values

The `Values` property is a `CommandContextValues` instance that acts as a case-insensitive dictionary of contextual information. These values are populated through implementations of `ICommandContextValuesProvider`.

### How Values Are Populated

The command pipeline uses the `CommandContextValuesBuilder` to collect values from all registered `ICommandContextValuesProvider` implementations.
Each provider receives the command instance being executed and contributes its values. If there are overlapping keys, the last provider's value takes precedence.

## Extending with Custom Values

To add your own values to the command context, implement the `ICommandContextValuesProvider` interface:

```csharp
public interface ICommandContextValuesProvider
{
    CommandContextValues Provide(object command);
}
```

The `command` parameter provides access to the command instance being executed, allowing providers to customize their values based on the specific command type or content.

### Example Implementation

Here's an example of a custom provider that adds audit tracking information:

```csharp
public class AuditContextValuesProvider : ICommandContextValuesProvider
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditContextValuesProvider(IDateTimeProvider dateTimeProvider, IHttpContextAccessor httpContextAccessor)
    {
        _dateTimeProvider = dateTimeProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public CommandContextValues Provide(object command)
    {
        var values = new CommandContextValues();
        
        values["ExecutedAt"] = _dateTimeProvider.UtcNow;
        values["ExecutedBy"] = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
        values["TraceId"] = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
        
        // Example of using command information
        values["CommandType"] = command.GetType().Name;
        
        return values;
    }
}
```

### Registration

The provider will be automatically discovered and registered by the dependency injection system if it's in the application's assembly, or you can register it manually:

```csharp
services.AddSingleton<ICommandContextValuesProvider, AuditContextValuesProvider>();
```

## Accessing Command Context

Within command handlers, filters, or other components in the command pipeline, you can access the current command context through the `ICommandContextAccessor`:

```csharp
[Command]
public record MyCommand(string SomeProperty)
{
    public object Handle(ICommandContextAccessor contextAccessor)
    {
        // Access values from the current context
        var context = contextAccessor.Current;
        if (context.Values.TryGetValue("ExecutedBy", out var executedBy))
        {
            // Use the execution user information for logging or business logic
        }
        
        return new { Success = true };
    }
}
```

## Non-Controller-Based Pipeline

The `CommandContext` is specifically designed for the non-controller-based command pipeline. In this pipeline:

1. Commands are executed through the `ICommandPipeline`
2. The context is created automatically with resolved dependencies
3. Values are populated from all registered providers
4. The context is made available throughout the execution chain
5. Filters can inspect and modify the execution based on context values
6. Response value handlers can use context information for processing results

This differs from the controller-based approach where ASP.NET Core's built-in dependency injection and model binding handle much of the context management.

## Best Practices

- Keep value providers lightweight and fast
- Use descriptive keys for your context values
- Avoid storing large objects in context values
- Consider the lifetime of your providers (typically singleton)
- Handle cases where expected values might not be present
- Use the context values for cross-cutting concerns like auditing, logging, and authorization
