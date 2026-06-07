# Command Response Examples

This document provides examples of how command responses work with the automatic response handling feature.

## Simple Value Response

When a command handler returns a simple value without a corresponding value handler:

```csharp
[Command]
public record CreateUser(string Name, string Email)
{
    public UserId Handle(IUserRepository userRepository)
    {
        var userId = new UserId(Guid.NewGuid());
        var user = new User(userId, Name, Email);
        userRepository.Save(user);
        
        // Since no value handler exists for UserId, 
        // this automatically becomes CommandResult<UserId>
        return userId;
    }
}
```

**Result**: `CommandResult<UserId>` with the UserId as the Response property.

## OneOf Response

When a command returns a OneOf with values that may or may not have handlers:

```csharp
[Command]
public record ValidateAndCreateUser(string Name, string Email)
{
    public OneOf<UserId, ValidationResult> Handle(IUserRepository userRepository, IUserValidator validator)
    {
        var validationResult = validator.Validate(Name, Email);
        if (!validationResult.IsValid)
        {
            // ValidationResult has a built-in handler, so this affects command success
            return OneOf<UserId, ValidationResult>.FromT1(validationResult);
        }
        
        var userId = new UserId(Guid.NewGuid());
        var user = new User(userId, Name, Email);
        userRepository.Save(user);
        
        // No handler for UserId, so this becomes CommandResult<UserId>
        return OneOf<UserId, ValidationResult>.FromT0(userId);
    }
}
```

**Result**: Either a failed `CommandResult` (for validation errors) or `CommandResult<UserId>` (for success).

## Tuple Response with Mixed Handlers

When a command returns a tuple with some values having handlers and others not:

```csharp
[Command]
public record ProcessOrder(string OrderId)
{
    public (OrderConfirmation, OrderProcessed, ValidationResult, AuditLog) Handle(
        IOrderService orderService,
        IOrderValidator validator,
        IAuditService auditService)
    {
        var validation = validator.Validate(OrderId);
        var confirmation = orderService.ProcessOrder(OrderId);
        var orderEvent = new OrderProcessed(OrderId, DateTime.UtcNow);
        var auditLog = auditService.CreateLog("Order processed", OrderId);
        
        return (confirmation, orderEvent, validation, auditLog);
    }
}
```

Assuming the following handlers exist:

- `OrderProcessed` → handled by event handler
- `ValidationResult` → handled by validation handler  
- `AuditLog` → handled by audit handler
- `OrderConfirmation` → **no handler**

**Result**: `CommandResult<OrderConfirmation>` with the confirmation as the Response property, plus any side effects from the other handlers.

## Tuple with Multiple Unhandled Values (Error)

This scenario throws an exception:

```csharp
[Command]
public record BadCommand()
{
    public (string, int, SomeEvent) Handle()
    {
        // If no handlers exist for string and int, but SomeEvent has a handler
        return ("response1", 42, new SomeEvent());
    }
}
```

**Result**: `MultipleUnhandledTupleValues` because both `string` and `int` lack handlers.

## Custom Value Handler Example

Creating a handler to process specific values instead of making them responses:

```csharp
public class OrderConfirmationHandler : ICommandResponseValueHandler
{
    private readonly IEmailService _emailService;
    
    public OrderConfirmationHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public bool CanHandle(CommandContext commandContext, object value)
    {
        return value is OrderConfirmation;
    }
    
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var confirmation = (OrderConfirmation)value;
        
        // Send confirmation email as a side effect
        await _emailService.SendConfirmationEmail(confirmation);
        
        // Don't affect the command result
        return CommandResult.Success(commandContext.CorrelationId);
    }
}
```

With this handler in place, `OrderConfirmation` values would be processed (email sent) rather than becoming responses.

## Migration from Previous Versions

### Before (Required Value Handlers)

```csharp
// Previously, this would require a value handler for UserId
public UserId Handle() => new UserId(Guid.NewGuid());

// You had to create a handler like this:
public class UserIdHandler : ICommandResponseValueHandler
{
    public bool CanHandle(CommandContext context, object value) => value is UserId;
    public Task<CommandResult> Handle(CommandContext context, object value)
    {
        var userId = (UserId)value;
        // Set the response manually...
        return Task.FromResult(new CommandResult<UserId>(userId));
    }
}
```

### After (Automatic Responses)

```csharp
// Now this automatically becomes CommandResult<UserId>
public UserId Handle() => new UserId(Guid.NewGuid());

// No handler needed unless you want side effects
```

This reduces boilerplate while maintaining the same functionality.
