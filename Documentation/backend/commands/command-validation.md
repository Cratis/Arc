# Command Validation

The Arc provides built-in support for validating commands without executing them. This enables pre-flight validation to provide early feedback to users before performing potentially expensive or state-changing operations.

## Overview

Command validation allows you to check authorization and validation rules without executing the command handler. This is useful for:

- **Early User Feedback**: Show validation errors before the user submits a form
- **UX Improvements**: Enable/disable submit buttons based on validation state
- **Authorization Checks**: Verify user permissions without side effects
- **Progressive Validation**: Validate fields as users interact with forms

For frontend usage of command validation, see:

- [Core Command Validation](../../frontend/core/command-validation.md) - TypeScript/JavaScript API
- [React Command Validation](../../frontend/react/command-validation.md) - React-specific patterns and hooks

## Backend Support

### ICommandPipeline.Validate

The `ICommandPipeline` interface provides a `Validate` method that runs only authorization and validation filters:

```csharp
public interface ICommandPipeline
{
    /// <summary>
    /// Validates the given command without executing it.
    /// </summary>
    Task<CommandResult> Validate(object command);
}
```

**Key Characteristics:**

- Runs all command filters (authorization, validation)
- Does **not** invoke the command handler
- Returns a `CommandResult` with validation and authorization status
- No side effects on the system

### Example Usage

```csharp
public class OrderService
{
    private readonly ICommandPipeline _commandPipeline;

    public OrderService(ICommandPipeline commandPipeline)
    {
        _commandPipeline = commandPipeline;
    }

    public async Task<bool> CanCreateOrder(CreateOrder command)
    {
        var result = await _commandPipeline.Validate(command);
        return result.IsSuccess;
    }

    public async Task CreateOrder(CreateOrder command)
    {
        // Optionally validate first
        var validationResult = await _commandPipeline.Validate(command);
        if (!validationResult.IsSuccess)
        {
            // Handle validation errors
            return;
        }

        // Execute the command
        var result = await _commandPipeline.Execute(command);
        // Process result...
    }
}
```

### Model-Bound Commands

For model-bound commands, validation endpoints are automatically created alongside execute endpoints:

**Execute Endpoint**: `POST /api/orders/create-order`
**Validate Endpoint**: `POST /api/orders/create-order/validate`

The validation endpoint accepts the same payload as the execute endpoint but only runs filters.

### Controller-Based Commands

For controller-based commands, validation endpoints are **automatically discovered and created** at application startup. The system scans all controller actions that:

- Are POST methods
- Have a single `[FromBody]` parameter (the command)

For each matching controller action, a corresponding `/validate` endpoint is automatically registered.

**Example Controller:**

```csharp
[Route("api/carts")]
public class Carts : ControllerBase
{
    [HttpPost("add")]
    public Task AddItemToCart([FromBody] AddItemToCart command)
    {
        // Execute the command
    }
}
```

**Automatically Created Endpoints:**

- **Execute**: `POST /api/carts/add`
- **Validate**: `POST /api/carts/add/validate` _(automatically created)_

**Key Points:**

- Validation endpoints are automatically created for all controller command actions
- No attributes or special configuration required
- The system detects commands by looking for POST actions with `[FromBody]` parameters
- The route pattern for validation is: `{controller-action-route}/validate`
- Only validation and authorization filters run; the action method is not executed

**How It Works:**

During application startup, the `CommandValidationRouteConvention` automatically:

1. Identifies command actions (POST methods that implement command patterns)
2. Creates corresponding `/validate` routes for each command action using ASP.NET Core's application model conventions
3. Routes all requests through the standard ASP.NET Core pipeline, including:
   - Authorization filters
   - Model binding
   - Command validation filters
4. The `CommandActionFilter` detects validation requests (paths ending with `/validate`) and skips action execution
5. Returns validation results without invoking the actual command handler

This approach ensures validation requests go through the exact same pipeline as execution requests, maintaining consistency in authorization, model binding, and validation behavior.

## Validation Filters

The validation pipeline runs all registered command filters:

### Built-in Filters

1. **AuthorizationFilter**: Checks user permissions
2. **DataAnnotationValidationFilter**: Validates data annotations
3. **FluentValidationFilter**: Runs FluentValidation validators

For more information, see [Command Filters](./command-filters.md).

## Best Practices

### When to Implement Validation

✅ **Good Use Cases:**

- Commands that modify critical business data
- Commands with complex authorization requirements
- Commands with expensive validation logic that benefits from early feedback
- Commands used in interactive forms

❌ **Less Beneficial:**

- Simple CRUD operations with minimal validation
- Commands only executed by background processes
- Commands with very fast execution times

### Performance Considerations

- Validation runs all filters, which may include database queries
- Optimize validator implementations for performance
- Consider caching authorization checks where appropriate
- Use appropriate indexes for validation queries

## Security Considerations

- Validation endpoints run the same authorization filters as execute endpoints
- Unauthorized users receive 401/403 responses from validation endpoints
- Validation does not expose sensitive data since handlers aren't executed
- Validation results may reveal authorization policies (by design)
- Always implement proper authorization filters for commands

## Troubleshooting

### Validation endpoint returns 404

**Cause**: The validation endpoint may not be properly registered for controller-based commands.

**Solution**: Ensure the controller action follows the pattern:

- Is a POST method
- Has a single `[FromBody]` parameter
- The validation endpoint should be automatically created at `{route}/validate`

### Validation is slow

**Cause**: Complex validation logic or database queries in validators.

**Solution**:

- Optimize validator implementations
- Add appropriate database indexes
- Consider caching validation results where appropriate
- Profile validator performance to identify bottlenecks

### Validation passes but execute fails

**Cause**: State may have changed between validate and execute calls, or the handler encountered an error.

**Solution**: This is expected behavior in concurrent systems. Validation is a pre-flight check, not a guarantee of execution success.
