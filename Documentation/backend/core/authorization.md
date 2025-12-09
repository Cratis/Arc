# Authorization

Arc.Core provides authorization capabilities through attributes that protect your commands and queries. This allows you to control access based on authentication status and user roles.

## Overview

Authorization in Arc.Core is attribute-based and supports:

- **Authentication Requirements** - Require users to be authenticated
- **Role-Based Authorization** - Restrict access to specific roles
- **Anonymous Access** - Explicitly allow unauthenticated access
- **Flexible Application** - Apply at class or method level

## Authorization Attributes

Arc.Core provides authorization through attributes:

- **`[Authorize]`** - Requires authentication and optionally specifies roles or policies
- **`[Roles]`** - Convenience attribute for role-based authorization
- **`[AllowAnonymous]`** - Explicitly allows unauthenticated access

## Basic Usage

### Requiring Authentication

Require users to be authenticated without specifying roles:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Authorization;

[Authorize]
public record UpdateProfile(string Name, string Email) : ICommand;

public class UpdateProfileHandler : ICommandHandler<UpdateProfile>
{
    public Task<CommandResult> Handle(UpdateProfile command, CommandContext context)
    {
        // Only authenticated users can reach this handler
        return Task.FromResult(CommandResult.Success);
    }
}
```

### Role-Based Authorization

Restrict access to specific roles:

```csharp
// Using Authorize attribute
[Authorize(Roles = "Admin")]
public record DeleteUser(Guid UserId) : ICommand;

// Using Roles attribute (more readable for multiple roles)
[Roles("Admin", "Manager")]
public record ApproveRequest(Guid RequestId) : ICommand;
```

### Anonymous Access

Explicitly allow anonymous access (useful when you have a fallback policy requiring authentication):

```csharp
[AllowAnonymous]
public record GetPublicData() : IQuery<PublicDataDto>;
```

## Applying Authorization

Authorization attributes can be applied at different levels:

### Class-Level Authorization

Apply to all commands or queries in a type:

```csharp
[Authorize]
public record UpdateSettings(string Key, string Value) : ICommand;

[Roles("Admin")]
public record DeleteAccount(Guid AccountId) : ICommand;
```

### Handler-Level Authorization

Apply authorization to handler classes (less common but supported):

```csharp
[Authorize]
public class SecureCommandHandler : ICommandHandler<SecureCommand>
{
    public Task<CommandResult> Handle(SecureCommand command, CommandContext context)
    {
        // Requires authentication
    }
}
```

## Role-Based Scenarios

### Single Role Requirement

```csharp
// User must have the "Admin" role
[Roles("Admin")]
public record CreateAdmin(string Username) : ICommand;
```

### Multiple Role Requirement (OR Logic)

Users need **at least one** of the specified roles:

```csharp
// User must have either "Admin" OR "Manager" role
[Roles("Admin", "Manager")]
public record ViewAuditLog() : IQuery<AuditLogDto>;
```

### Combining with Standard Authorize

You can mix `[Authorize]` and `[Roles]` if needed:

```csharp
// Requires authentication via specific scheme AND a role
[Authorize(AuthenticationSchemes = "Bearer")]
[Roles("Admin")]
public record SecureAdminCommand(string Data) : ICommand;
```

## AllowAnonymous Attribute

The `[AllowAnonymous]` attribute explicitly allows unauthenticated access:

```csharp
[AllowAnonymous]
public record GetPublicCatalog() : IQuery<CatalogDto>;

public class GetPublicCatalogHandler : IQueryHandler<GetPublicCatalog, CatalogDto>
{
    public Task<CatalogDto> Handle(GetPublicCatalog query, QueryContext context)
    {
        // Anyone can access this, even without authentication
        return Task.FromResult(new CatalogDto());
    }
}
```

## Authorization Inheritance Rules

Authorization attributes follow specific inheritance rules:

| Scenario | Result |
|----------|--------|
| `[Authorize]` on class | Requires authentication for all operations |
| `[Roles]` on class | Requires specified roles for all operations |
| `[AllowAnonymous]` on class | Allows anonymous access for all operations |
| Method attribute overrides class | Method-level attribute takes precedence |
| Both `[Authorize]` and `[AllowAnonymous]` on same target | **Error** - throws `AmbiguousAuthorizationLevel` |

### Examples

```csharp
// Class-level authorization applies to all operations
[Authorize]
public record UserOperations
{
    // Inherits [Authorize] from the record
    public record GetProfile(Guid UserId) : IQuery<ProfileDto>;
    
    // Overrides with more specific role requirement
    [Roles("Admin")]
    public record DeleteProfile(Guid UserId) : ICommand;
}

// Class-level anonymous access
[AllowAnonymous]
public record PublicQueries
{
    // Inherits [AllowAnonymous] from record
    public record GetProducts() : IQuery<ProductDto[]>;
    
    // Override to require authentication for specific operation
    [Authorize]
    public record GetUserProducts() : IQuery<ProductDto[]>;
}

// ERROR: This will throw AmbiguousAuthorizationLevel at startup
[AllowAnonymous]
[Authorize]  // Cannot have both!
public record InvalidCommand : ICommand;
```

## Working with Claims

When a request is authenticated, the `ClaimsPrincipal` is available through the command or query context:

```csharp
public class GetUserDataHandler : IQueryHandler<GetUserData, UserDataDto>
{
    public Task<UserDataDto> Handle(GetUserData query, QueryContext context)
    {
        // Access the authenticated user's claims
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = context.User.FindFirst(ClaimTypes.Name)?.Value;
        var roles = context.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        // Use claims for authorization logic
        return Task.FromResult(new UserDataDto(userId, userName, roles));
    }
}
```

## Authorization Results

When authorization fails, Arc.Core automatically returns appropriate HTTP status codes:

| Scenario | HTTP Status Code | Description |
|----------|-----------------|-------------|
| **Not Authenticated** | 401 Unauthorized | User is not authenticated |
| **Not Authorized** | 403 Forbidden | User is authenticated but doesn't have required permissions |

## Custom Authorization Logic

For more complex authorization scenarios, implement custom logic in your handlers:

```csharp
[Authorize]
public record UpdateOrder(Guid OrderId, string Data) : ICommand;

public class UpdateOrderHandler(IOrderRepository orders) 
    : ICommandHandler<UpdateOrder>
{
    public async Task<CommandResult> Handle(
        UpdateOrder command, 
        CommandContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var order = await orders.GetById(command.OrderId);

        // Custom authorization: user can only update their own orders
        if (order.UserId != userId)
        {
            return CommandResult.Forbidden(
                context.CorrelationId, 
                "You can only update your own orders");
        }

        // Process the update
        order.Update(command.Data);
        await orders.Save(order);

        return CommandResult.Success;
    }
}
```

## Policy-Based Authorization

While Arc.Core focuses on attribute-based authorization, you can implement policy-based logic in your handlers:

```csharp
[Authorize]
public record ApproveExpense(Guid ExpenseId) : ICommand;

public class ApproveExpenseHandler(
    IExpenseRepository expenses,
    IAuthorizationService authService) 
    : ICommandHandler<ApproveExpense>
{
    public async Task<CommandResult> Handle(
        ApproveExpense command, 
        CommandContext context)
    {
        var expense = await expenses.GetById(command.ExpenseId);

        // Custom policy: managers can approve up to $1000, directors unlimited
        var isManager = context.User.IsInRole("Manager");
        var isDirector = context.User.IsInRole("Director");

        if (expense.Amount > 1000 && !isDirector)
        {
            return CommandResult.Forbidden(
                context.CorrelationId,
                "Only directors can approve expenses over $1000");
        }

        if (!isManager && !isDirector)
        {
            return CommandResult.Forbidden(
                context.CorrelationId,
                "Only managers and directors can approve expenses");
        }

        // Process approval
        expense.Approve(context.User.Identity?.Name ?? "Unknown");
        await expenses.Save(expense);

        return CommandResult.Success;
    }
}
```

## Best Practices

### Secure by Default

Apply authorization at the broadest scope possible and override only when necessary:

```csharp
// Good: Secure by default, explicit opt-out
[Authorize]
public record SecureOperations
{
    public record CreateResource() : ICommand;
    public record UpdateResource(Guid Id) : ICommand;
    
    // Explicitly allow anonymous for specific operation
    [AllowAnonymous]
    public record GetPublicResources() : IQuery<ResourceDto[]>;
}
```

### Use Roles Attribute for Clarity

Use `[Roles]` for better readability when specifying multiple roles:

```csharp
// More readable
[Roles("Admin", "Manager", "Supervisor")]
public record ReviewApplication(Guid ApplicationId) : ICommand;

// Less readable
[Authorize(Roles = "Admin,Manager,Supervisor")]
public record ReviewApplication(Guid ApplicationId) : ICommand;
```

### Avoid Ambiguous Authorization

Never apply both `[Authorize]` and `[AllowAnonymous]` to the same target:

```csharp
// ERROR: Will throw AmbiguousAuthorizationLevel
[Authorize]
[AllowAnonymous]
public record AmbiguousCommand : ICommand;
```

### Document Authorization Requirements

Add XML documentation to clarify authorization requirements:

```csharp
/// <summary>
/// Deletes a user account. Requires Admin role.
/// </summary>
[Roles("Admin")]
public record DeleteUser(Guid UserId) : ICommand;
```

### Validate Claims in Handlers

For complex authorization logic, validate claims within handlers:

```csharp
public Task<CommandResult> Handle(SecureCommand command, CommandContext context)
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Task.FromResult(
            CommandResult.Unauthorized(context.CorrelationId));
    }

    // Continue with business logic
}
```

## Integration with Authentication

Authorization works hand-in-hand with authentication. See the [Authentication](authentication.md) documentation for how to implement custom authentication handlers that provide the claims used by authorization.

## Testing Authorization

When testing authorization, ensure your test setup includes proper claims:

```csharp
public class SecureCommandTests
{
    [Fact]
    public async Task should_allow_admin_users()
    {
        var handler = new SecureCommandHandler();
        var context = new CommandContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };

        var result = await handler.Handle(new SecureCommand(), context);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task should_deny_non_admin_users()
    {
        var handler = new SecureCommandHandler();
        var context = new CommandContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Role, "User")
            }, "Test"))
        };

        var result = await handler.Handle(new SecureCommand(), context);

        result.IsSuccess.ShouldBeFalse();
    }
}
```

## Next Steps

- [Authentication](authentication.md) - Implement custom authentication handlers
- [Getting Started](getting-started.md) - Learn more about Arc.Core basics
- [Identity](../identity.md) - Integrate with Arc's identity system
- [Commands](../commands/) - Learn about command patterns and authorization
- [Queries](../queries/) - Discover query features with authorization
