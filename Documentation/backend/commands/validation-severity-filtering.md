# Validation Severity Filtering

Validation severity filtering allows commands to specify which validation result severity levels should block execution. This enables flexible validation workflows where warnings and informational messages can be shown to users without preventing command execution.

## Overview

Validation results have different severity levels that indicate the importance of the validation issue:

```csharp
public enum ValidationResultSeverity
{
    /// <summary>
    /// The validation result is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The validation result is informational.
    /// </summary>
    Information = 1,

    /// <summary>
    /// The validation result is a warning.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// The validation result is an error.
    /// </summary>
    Error = 3
}
```

By default, only **Error** severity results block command execution. Warnings and Information results are filtered out and don't prevent execution.

## Purpose

Severity filtering enables:

- **User-Friendly Workflows**: Show warnings to users without blocking operations
- **Confirmable Warnings**: Allow users to review and acknowledge warnings before proceeding
- **Flexible Validation**: Apply different validation strictness based on context
- **Progressive Execution**: Validate strictly first, then allow controlled overrides

## How It Works

### Request Flow

1. Client sends command with optional `X-Allowed-Severity` HTTP header
2. `CommandEndpointMapper` reads the header and parses severity value
3. `CommandPipeline` executes with `allowedSeverity` parameter
4. Validation filters run and return validation results
5. `FilterValidationResults` filters based on allowed severity
6. Only validation results with severity > `allowedSeverity` block execution

### CommandContext

The `CommandContext` includes the allowed severity:

```csharp
public record CommandContext(
    CorrelationId CorrelationId,
    Type Type,
    object Command,
    IEnumerable<object> Dependencies,
    CommandContextValues Values,
    ValidationResultSeverity? AllowedSeverity = default,
    object? Response = default);
```

### ICommandPipeline

The `ICommandPipeline` interface accepts an optional `allowedSeverity` parameter:

```csharp
public interface ICommandPipeline
{
    /// <summary>
    /// Executes the given command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="serviceProvider">The service provider scoped to the current request.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A CommandResult representing the result of executing the command.</returns>
    Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default);

    /// <summary>
    /// Validates the given command without executing it.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="serviceProvider">The service provider scoped to the current request.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A CommandResult representing the validation result.</returns>
    Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default);
}
```

## Implementation

### CommandPipeline

The `CommandPipeline` filters validation results after filters run:

```csharp
public async Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default)
{
    var correlationId = GetCorrelationId();
    var result = CommandResult.Success(correlationId);
    
    try
    {
        handlerProviders.TryGetHandlerFor(command, out var commandHandler);
        if (commandHandler is null)
        {
            return CommandResult.MissingHandler(correlationId, command.GetType());
        }

        var dependencies = commandHandler.Dependencies.Select(serviceProvider.GetRequiredService);
        var commandContext = new CommandContext(
            correlationId,
            command.GetType(),
            command,
            dependencies,
            contextValuesBuilder.Build(command),
            allowedSeverity);  // Pass allowed severity to context
            
        contextModifier.SetCurrent(commandContext);
        result = await commandFilters.OnExecution(commandContext);
        
        // Filter validation results based on allowed severity
        result = FilterValidationResults(result, allowedSeverity);
        
        if (!result.IsSuccess)
        {
            return result;
        }

        var response = await commandHandler.Handle(commandContext);
        // Process response...
    }
    catch (Exception ex)
    {
        result.MergeWith(CommandResult.Error(correlationId, ex));
    }

    return result;
}
```

### FilterValidationResults

The filtering logic:

```csharp
/// <summary>
/// Filters validation results based on the allowed severity level.
/// </summary>
/// <param name="result">The command result to filter. This method modifies the ValidationResults property.</param>
/// <param name="allowedSeverity">The maximum allowed severity level. Results with higher severity will be kept.</param>
/// <returns>The modified command result.</returns>
/// <remarks>
/// When allowedSeverity is null, only errors block execution (warnings and information are filtered out).
/// When allowedSeverity is specified, only validation results with severity > allowedSeverity block execution.
/// </remarks>
CommandResult FilterValidationResults(CommandResult result, ValidationResultSeverity? allowedSeverity)
{
    if (allowedSeverity is null)
    {
        // Default behavior: only errors block execution (warnings and information are filtered out)
        result.ValidationResults = result.ValidationResults.Where(v => v.Severity == ValidationResultSeverity.Error).ToArray();
    }
    else
    {
        // Filter out validation results with severity <= allowedSeverity
        result.ValidationResults = result.ValidationResults.Where(v => v.Severity > allowedSeverity).ToArray();
    }

    return result;
}
```

### CommandEndpointMapper

The `CommandEndpointMapper` reads the `X-Allowed-Severity` header from requests:

```csharp
ValidationResultSeverity? allowedSeverity = default;
if (context.Headers.TryGetValue("X-Allowed-Severity", out var severityHeader) &&
    int.TryParse(severityHeader, out var severityValue))
{
    allowedSeverity = (ValidationResultSeverity)severityValue;
}

commandResult = validateOnly
    ? await commandPipeline.Validate(command, context.RequestServices, allowedSeverity)
    : await commandPipeline.Execute(command, context.RequestServices, allowedSeverity);
```

## Creating Warnings in Validators

### FluentValidation

Use the custom `WithSeverity` method or leverage FluentValidation's built-in severity:

```csharp
public class CreateOrderValidator : CommandValidator<CreateOrder>
{
    public CreateOrderValidator()
    {
        // Critical validation - Error severity (default)
        RuleFor(c => c.OrderNumber)
            .NotEmpty()
            .WithMessage("Order number is required");

        // Warning - soft validation
        RuleFor(c => c.Quantity)
            .GreaterThan(0)
            .WithMessage("Order quantity is very low")
            .WithSeverity(Severity.Warning);
            
        // Information - helpful message
        RuleFor(c => c.DeliveryDate)
            .GreaterThan(DateTime.UtcNow.AddDays(7))
            .WithMessage("Orders placed more than 7 days in advance may be eligible for free shipping")
            .WithSeverity(Severity.Info);
    }
}
```

**Note**: You'll need to configure the mapping from FluentValidation's `Severity` to Arc's `ValidationResultSeverity`:

```csharp
// In your FluentValidationFilter or custom implementation
var severity = validationFailure.Severity switch
{
    Severity.Error => ValidationResultSeverity.Error,
    Severity.Warning => ValidationResultSeverity.Warning,
    Severity.Info => ValidationResultSeverity.Information,
    _ => ValidationResultSeverity.Error
};
```

### Custom Validation Results

Create validation results with specific severity directly:

```csharp
[Command]
public record CreateOrder(string OrderNumber, int Quantity)
{
    public (ValidationResult[], Order?) Handle(IInventoryService inventoryService)
    {
        var validationResults = new List<ValidationResult>();
        
        // Check inventory
        var stock = inventoryService.GetStock(OrderNumber);
        
        if (stock == 0)
        {
            // Critical error - cannot proceed
            validationResults.Add(new ValidationResult(
                ValidationResultSeverity.Error,
                "Product is out of stock",
                [nameof(OrderNumber)],
                null));
        }
        else if (stock < Quantity)
        {
            // Warning - user can override
            validationResults.Add(new ValidationResult(
                ValidationResultSeverity.Warning,
                $"Only {stock} units available. Order will be partially fulfilled.",
                [nameof(Quantity)],
                new { AvailableStock = stock }));
        }
        else if (stock < 10)
        {
            // Information - just FYI
            validationResults.Add(new ValidationResult(
                ValidationResultSeverity.Information,
                "Stock is running low. Consider ordering soon.",
                [nameof(OrderNumber)],
                null));
        }
        
        // If only warnings/info, return them along with the order
        if (validationResults.Any() && validationResults.All(v => v.Severity < ValidationResultSeverity.Error))
        {
            var order = new Order { OrderNumber = OrderNumber, Quantity = Quantity };
            return (validationResults.ToArray(), order);
        }
        
        // If errors, return only validation results
        if (validationResults.Any(v => v.Severity == ValidationResultSeverity.Error))
        {
            return (validationResults.ToArray(), null);
        }
        
        // All good
        var successOrder = new Order { OrderNumber = OrderNumber, Quantity = Quantity };
        return ([], successOrder);
    }
}
```

## Programmatic Usage

### Direct Pipeline Usage

You can use the pipeline directly with severity filtering:

```csharp
public class OrderService
{
    private readonly ICommandPipeline _commandPipeline;

    public OrderService(ICommandPipeline commandPipeline)
    {
        _commandPipeline = commandPipeline;
    }

    public async Task<CommandResult> CreateOrderStrictly(CreateOrder command)
    {
        // Default behavior - only errors block
        return await _commandPipeline.Execute(command, serviceProvider);
    }

    public async Task<CommandResult> CreateOrderAllowingWarnings(CreateOrder command)
    {
        // Allow warnings to pass through
        return await _commandPipeline.Execute(
            command, 
            serviceProvider, 
            ValidationResultSeverity.Warning);
    }

    public async Task<CommandResult> CreateOrderWithConfirmation(CreateOrder command, bool userConfirmedWarnings)
    {
        // First attempt - strict validation
        var result = await _commandPipeline.Execute(command, serviceProvider);
        
        if (!result.IsSuccess && !result.HasExceptions)
        {
            // Check if only warnings
            var hasOnlyWarnings = result.ValidationResults.All(v => v.Severity == ValidationResultSeverity.Warning);
            
            if (hasOnlyWarnings && userConfirmedWarnings)
            {
                // User confirmed - allow warnings
                result = await _commandPipeline.Execute(
                    command, 
                    serviceProvider, 
                    ValidationResultSeverity.Warning);
            }
        }
        
        return result;
    }
}
```

### Integration Testing

Test severity filtering in integration tests:

```csharp
[Fact]
public async Task should_block_execution_with_error_severity()
{
    var command = new CreateOrder("INVALID", 1);
    
    var result = await _commandPipeline.Execute(command, _serviceProvider);
    
    result.IsSuccess.ShouldBeFalse();
    result.ValidationResults.ShouldNotBeEmpty();
    result.ValidationResults.ShouldAllBe(v => v.Severity == ValidationResultSeverity.Error);
}

[Fact]
public async Task should_block_execution_with_warning_when_not_allowed()
{
    var command = new CreateOrder("LOW-STOCK", 1);
    
    // Don't allow warnings
    var result = await _commandPipeline.Execute(command, _serviceProvider);
    
    result.IsSuccess.ShouldBeFalse();
    result.ValidationResults.ShouldContain(v => v.Severity == ValidationResultSeverity.Warning);
}

[Fact]
public async Task should_allow_execution_with_warning_when_allowed()
{
    var command = new CreateOrder("LOW-STOCK", 1);
    
    // Allow warnings
    var result = await _commandPipeline.Execute(
        command, 
        _serviceProvider, 
        ValidationResultSeverity.Warning);
    
    result.IsSuccess.ShouldBeTrue();
}
```

## Best Practices

### When to Use Different Severities

**Error Severity** - Use for:
- Required field validations
- Data format errors
- Business rule violations
- Authorization failures
- Data integrity issues

**Warning Severity** - Use for:
- Soft business rules that can be overridden
- Potential issues that don't prevent operation
- Non-critical recommendations
- Edge cases requiring user acknowledgment

**Information Severity** - Use for:
- Helpful tips and suggestions
- Status information
- Performance recommendations
- Optional improvements

### Security Considerations

- **Never** use Warning severity for security validations
- **Always** use Error severity for:
  - Authorization checks
  - Authentication failures
  - Security policy violations
  - Critical business rules
- Don't rely solely on client-side severity filtering
- Server always validates with the same severity logic

### Performance Tips

- Severity filtering adds minimal overhead
- Same validators run regardless of allowed severity
- Consider validator performance separately
- Use appropriate indexes for validation queries

## Troubleshooting

### Warnings Not Filtered

**Cause**: Validators might be using Error severity instead of Warning.

**Solution**: Check validator implementations and ensure they use appropriate severity:

```csharp
// Wrong - using default Error severity
RuleFor(c => c.Quantity)
    .GreaterThan(0)
    .WithMessage("Quantity should be positive");

// Correct - using Warning severity
RuleFor(c => c.Quantity)
    .GreaterThan(0)
    .WithMessage("Quantity should be positive")
    .WithSeverity(Severity.Warning);
```

### Errors Allowed Through

**Cause**: Incorrectly configured severity or using Error severity in `allowedSeverity` parameter.

**Solution**:
- Never pass `ValidationResultSeverity.Error` as the allowed severity
- Verify validators are using Error severity for critical issues
- Check that custom validation code sets correct severity

### Client and Server Results Differ

**Cause**: Client-side and server-side validators may have different implementations.

**Solution**:
- Server validation is authoritative
- Ensure client validators match server rules
- Use FluentValidation with proxy generation for consistency
- Test both client and server validation

## Related Documentation

- [Command Validation](./command-validation.md) - Pre-flight validation
- [Validation](./validation.md) - Validation configuration
- [Command Filters](./command-filters.md) - Validation pipeline
- [Frontend Validation Severity Filtering](../../frontend/core/validation-severity-filtering.md) - Client usage
