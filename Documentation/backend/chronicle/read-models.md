---
uid: Arc.Chronicle.ReadModels
---
# Read Models

Read Models in Arc provide automatic dependency injection and seamless integration with Chronicle's projection system. The client automatically resolves read models based on the event source ID from the [Command Context](../../commands/command-context.md) values provided by the [Event Source Values Provider](../event-source-values-provider.md).

## Overview

Arc automatically registers all read model types and provides them as transient services in the dependency injection container. When a read model is requested, it uses the event source ID from the current command context to load the appropriate projection instance.

## Automatic Registration

Read models are automatically discovered and registered when you configure Arc. This includes:

- Read models from `IProjectionFor<T>` implementations
- Read models from model-bound projections

The system will scan for all read model types and register them with the dependency injection container.

## Taking Dependencies on Read Models

You can inject read models directly into your commands through the Handle method signature, just like aggregate roots:

```csharp
public record UpdateUserProfileCommand([Key] Guid UserId, string DisplayName, string Bio)
{
    public object Handle(UserProfile profile, ILogger<UpdateUserProfileCommand> logger)
    {
        // The 'profile' read model is automatically loaded using the UserId
        // from the command as the event source ID
        
        logger.LogInformation(
            "Updating profile for {Email} from {OldName} to {NewName}",
            profile.Email,
            profile.DisplayName,
            DisplayName);

        // You can use the read model for validation or context
        if (profile.Status == UserStatus.Suspended)
        {
            throw new CannotUpdateSuspendedUserProfile(UserId);
        }

        return new UpdateUserProfileCommand { DisplayName = DisplayName, Bio = Bio };
    }
}
```

## Event Source ID Resolution

The read model resolution works exactly the same way as [Aggregate Root](../aggregates/aggregate-roots.md) resolution. It depends entirely on the event source ID being available in the command context. The [Event Source Values Provider](../event-source-values-provider.md) supplies this value through the [Command Context Values](../../commands/command-context.md#command-context-values) pipeline. The resolution process works as follows:

1. **Command Context Lookup**: The system retrieves the event source ID from the current `CommandContext`
2. **Validation**: If no event source ID is found, an `UnableToResolveReadModelFromCommandContext` exception is thrown
3. **Projection Query**: The system queries Chronicle's projection store using `IProjections.GetInstanceById()` with the resolved event source ID
4. **Instance Return**: The loaded read model instance is returned

### Event Source ID Requirements

For read model resolution to work, the command must provide an event source ID through one of these methods:

- Implement `ICanProvideEventSourceId`
- Have a property of type `EventSourceId`
- Have a property marked with `[Key]` attribute
- Be part of a tuple that contains an `EventSourceId`

## Example Usage

### Validation with Read Models

Read models are particularly useful for validation since they provide the current projected state:

```csharp
public record PlaceOrderCommand([Key] Guid OrderId, Guid CustomerId, OrderLine[] Items)
{
    public OrderPlaced Handle(OrderReadModel order)
    {
        // Use the read model to validate business rules
        if (order.Status != OrderStatus.Draft)
        {
            throw new CannotModifyNonDraftOrder(OrderId);
        }

        if (order.LineItems.Length + Items.Length > 100)
        {
            throw new TooManyOrderLines();
        }

        return new OrderPlaced
        {
            CustomerId = CustomerId,
            Items = Items
        };
    }
}
```

### Combining Read Models and Aggregate Roots

You can inject both read models and aggregate roots in the same handler:

```csharp
public record AddItemToCartCommand([Key] Guid CartId, Guid ProductId, int Quantity)
{
    public ItemAddedToCart Handle(
        ShoppingCart cart,              // Aggregate root
        ShoppingCartSummary summary,    // Read model
        ILogger<AddItemToCartCommand> logger)
    {
        // Use read model for display/validation
        logger.LogInformation(
            "Adding item to cart with {ItemCount} items totaling {Total}",
            summary.TotalItems,
            summary.TotalAmount);

        // Use aggregate root for command
        cart.AddItem(ProductId, Quantity);

        return new ItemAddedToCart { ProductId = ProductId, Quantity = Quantity };
    }
}
```

### Read-Only Commands

For commands that don't modify state, you can use only read models:

```csharp
public record GetUserStatsCommand([Key] Guid UserId)
{
    public UserStatsResponse Handle(UserStatistics stats)
    {
        // Just query the read model and return data
        return new UserStatsResponse
        {
            TotalPosts = stats.PostCount,
            TotalLikes = stats.LikeCount,
            MemberSince = stats.CreatedDate
        };
    }
}
```

### Using Read Models in Validators

Read models are particularly powerful when used in `CommandValidator<>` for complex validation scenarios. You can inject read models directly into your validator's constructor through dependency injection:

```csharp
public record AssignPersonToRoleCommand([Key] Guid RoleId, Guid PersonId)
{
    public PersonAssignedToRole Handle(Role role)
    {
        role.AssignPerson(PersonId);
        
        return new PersonAssignedToRole { PersonId = PersonId };
    }
}

public class AssignPersonToRoleValidator : CommandValidator<AssignPersonToRoleCommand>
{
    public AssignPersonToRoleValidator(RoleReadModel role)
    {
        RuleFor(x => x.PersonId)
            .NotEmpty()
            .WithMessage("Person ID is required");

        RuleFor(x => x.PersonId)
            .Must(personId => !role.AssignedPersonIds.Contains(personId))
            .WithMessage("Person is already assigned to this role");

        RuleFor(x => x)
            .Must(cmd => role.Status == RoleStatus.Active)
            .WithMessage("Cannot assign people to inactive roles");

        RuleFor(x => x)
            .Must(cmd => role.AssignedPersonIds.Length < role.MaxAssignments)
            .WithMessage($"Role has reached maximum assignments of {role.MaxAssignments}");
    }
}
```

Key points when using read models in validators:

- **Constructor Injection**: Read models are automatically resolved and injected, just like in command handlers
- **Event Source ID**: The read model instance is resolved using the same event source ID from the command
- **Validation Context**: Perfect for validating against current state before executing the command
- **Rich Rules**: Access to full read model state enables complex business rule validation
- **Async Validation**: Use `MustAsync` for asynchronous validation rules that need to check external systems

For more details on command validation, see the [Validation](../../commands/validation.md) documentation.

## Error Handling

### UnableToResolveReadModelFromCommandContext

This exception is thrown when:

- No event source ID is available in the command context
- The event source ID is `EventSourceId.Unspecified`

```csharp
public record InvalidCommand(string SomeProperty);
// No event source ID property or interface implementation

// This will fail because no event source ID can be resolved
```

## Lifecycle Management

### Transient Scope

Read models are registered as transient services, meaning:

- The current projected state is fetched for each request
- The instance is tied to the specific event source ID from the command context
- Read models are read-only snapshots of the current projection state
- The read model is automatically disposed after the command completes

### Read-Only Nature

Important characteristics of read models:

- **Read models are immutable** - Changes made to a read model instance are not persisted
- **For writes, use aggregate roots** - Only aggregate roots can emit events and change state
- **Eventually consistent** - Read models reflect the state as of the last processed event
- **Current as of query time** - The projection is fetched at the time of the command

## When to Use Read Models vs Aggregate Roots

Use **Read Models** when:
- You need to validate business rules against current state
- You need contextual information for logging
- You're implementing read-only queries
- You need denormalized or computed data
- You're validating commands with `CommandValidator<>`

Use **Aggregate Roots** when:
- You need to modify state and emit events
- You're implementing business logic that changes the domain
- You need transactional consistency within an aggregate boundary

Use **Both** when:
- You need to validate against current state (read model) before making changes (aggregate root)
- You want rich logging context while executing commands
- You need to perform validation in both the validator and the handler
