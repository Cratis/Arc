# Command Filters

Command filters provide a way to intercept and filter commands before they are handled in the **non-controller-based pipeline**. This allows you to implement cross-cutting concerns such as validation, authorization, logging, or custom business rules that should be applied to commands before they reach their handlers.

> **Note**: Command filters are specifically for the model-bound command pipeline. For controller-based commands, use standard ASP.NET Core filters instead.

## How It Works

Command filters are executed as part of the command pipeline, before the actual command handler is invoked. If a filter determines that a command should not proceed (e.g., validation fails), it can return an unsuccessful `CommandResult` to stop the pipeline execution.

## Implementing a Custom Command Filter

To create a custom command filter, implement the `ICommandFilter` interface:

```csharp
using Cratis.Arc.Commands;

public class MyCustomFilter : ICommandFilter
{
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        // Your filtering logic here
        
        // Return success to allow the command to continue
        return CommandResult.Success(context.CorrelationId);
        
        // Or return an error result to stop execution
        // return new CommandResult
        // {
        //     CorrelationId = context.CorrelationId,
        //     IsAuthorized = false,
        //     ValidationResults = [/* your validation errors */]
        // };
    }
}
```

The `CommandContext` provides access to:

- `CorrelationId` - The unique identifier for the command execution
- `CommandType` - The type of the command being executed
- `Command` - The actual command instance
- `Dependencies` - Any dependencies resolved for the command handler
- `Values` - Additional context values that may have been set

## Registering Custom Filters

Command filters are automatically discovered and registered through the dependency injection container. Simply ensure your filter class implements `ICommandFilter` and it will be included in the command pipeline.

## Cross-Cutting Authorization by Namespace

You can implement a command filter that applies authorization rules to all commands in a namespace instead of adding `[Authorize]` or `[Roles]` attributes to each command type.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Http;

namespace MyApp.Features.Security;

public class NamespaceAuthorizationCommandFilter(IHttpRequestContextAccessor requestContextAccessor) : ICommandFilter
{
    const string ProtectedNamespace = "MyApp.Features.Payments";
    const string RequiredRole = "Payments";

    public Task<CommandResult> OnExecution(CommandContext context)
    {
        var isProtectedCommand = context.Type.Namespace?.StartsWith(ProtectedNamespace, StringComparison.Ordinal) ?? false;
        if (!isProtectedCommand)
        {
            return Task.FromResult(CommandResult.Success(context.CorrelationId));
        }

        var hasRole = requestContextAccessor.Current?.User.IsInRole(RequiredRole) ?? false;
        return Task.FromResult(
            hasRole
                ? CommandResult.Success(context.CorrelationId)
                : CommandResult.Unauthorized(context.CorrelationId, $"Role '{RequiredRole}' is required."));
    }
}
```

This pattern is useful when you want one place to enforce authorization for a full slice or feature area.

## Built-in Filters

The following filters are provided out of the box in the `Cratis.Arc.Commands.Filters` namespace:

| Filter | Description |
|--------|-------------|
| `DataAnnotationValidationFilter` | Validates commands using data annotations (e.g., `[Required]`, `[Range]`, etc.) applied to command properties |
| `FluentValidationFilter` | Validates commands using FluentValidation validators, supporting nested object validation |
| `AuthorizationFilter` | Provides a foundation for command authorization (currently returns success by default) |

### DataAnnotationValidationFilter

This filter automatically validates commands that have properties decorated with data annotation attributes:

```csharp
[Command]
public record CreateUser(
    [Required] string Name,
    [EmailAddress] string Email,
    [Range(18, 120)] int Age);
```

If validation fails, the filter returns a `CommandResult` with validation errors, preventing the command from being handled.

### FluentValidationFilter

This filter works with FluentValidation validators that are discovered automatically. It supports recursive validation of nested objects within the command:

```csharp
public class CreateUserValidator : CommandValidator<CreateUser>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).InclusiveBetween(18, 120);
    }
}
```

### AuthorizationFilter

This filter provides the foundation for implementing command-level authorization.

With this you can leverage the `Authorize` attribute from ASP.NET Core.

```csharp
[Command]
[Authorize(Roles = "Administrator")]
public record CreateUser(
    string Name,
    string Email,
    int Age);
```

Or the convenience wrapper provided by Cratis Arc for  roles, allowing a more intuitive way of specifying multiple roles:

```csharp
[Command]
[Roles("System", "Admin")]
public record CreateUser(
    string Name,
    string Email,
    int Age);
```

## Best Practices

- Keep filters focused on a single concern (validation, authorization, etc.)
- Return meaningful error messages in `ValidationResult` objects
- Use the `CorrelationId` from the context for tracking and logging
- Consider performance implications, especially for filters that run on every command
- Test filters independently to ensure they work correctly in isolation
