---
uid: Arc.Chronicle.ReadModels
---
# Read Models

Read Models in Arc provide automatic dependency injection and seamless integration with Chronicle's projection system. The client automatically resolves read models based on the identity/key extracted from the command using flexible resolution strategies, with values provided through the [Command Context](../commands/command-context.md) by [Resolving EventSourceId](./resolving-event-source-id.md). If the command context also contains a Chronicle `Subject`, Arc uses it when releasing the resolved read model so `[PII]` properties decrypt under the same compliance identity that was used for the events.

## Overview

Arc automatically registers all read model types and provides them as scoped services in the dependency injection container, tied to the scope the command runs in. When a read model is requested, it uses the resolved identity from the current command context to load the appropriate projection instance. Because `CommandValidator<>`, `Provide()`, and `Handle()` resolve from the same command scope, they receive the same read model instance for the command when it exists.

A read model can also be missing. A `[Key]` or event source id tells Arc which projection instance to resolve; it does not prove that the projection instance exists. Missing projected state can be a normal business condition, such as "not registered yet", or it can be exceptional, such as a command that targets an event source whose projection is expected to exist. Declare the dependency nullable (`Customer?`, `RoleReadModel?`, and so on) when the command handles absence. Keep it non-nullable when the command requires the projection to exist; if Arc cannot resolve it, the command fails with a clear dependency-resolution error.

## Automatic Registration

Read models are automatically discovered and registered when you configure Arc. This includes:

- Read models from `IProjectionFor<T>` implementations
- Read models from model-bound projections

The system will scan for all read model types and register them with the dependency injection container.

## Taking Dependencies on Read Models

Read models can be injected into command validators, `Provide()`, and `Handle()` from the same command scope. The usual place to inject them is a `CommandValidator<>`, because validators run before the command handler and are the right place for existence checks and state-based rejection.

Use a nullable read model dependency when missing projected state is a valid business condition that the command should handle. Use a non-nullable dependency when the read model is required to exist and a missing instance should fail the command as a dependency-resolution error.

The same nullable dependency behavior applies if a command's `Provide()` or `Handle()` method takes a read model parameter. `Provide()` should still be used for acquiring data that `Handle()` consumes, not as the primary place for validation. See [Provide data to a command handler](../../scenarios/provide-data-to-a-command.md) for that pattern.

## Id/Key Resolution

The read model resolution works exactly the same way as [Aggregate Root](aggregates/aggregate-roots.md) resolution. It depends on identifying which read model instance to load from the projection store. The system supports multiple strategies for resolving this identity, with [Resolving EventSourceId](./resolving-event-source-id.md) supplying the resolved value through the [Command Context Values](../commands/command-context.md#command-context-values) pipeline. The resolution process works as follows:

1. **Identity Strategy Resolution**: The system inspects the command to determine the identity using one of the available strategies
2. **Command Context Lookup**: The resolved identity is retrieved from the current `CommandContext`
3. **Validation**: If no identity is found, an `UnableToResolveReadModelFromCommandContext` exception is thrown
4. **Projection Query**: The system queries Chronicle's projection store using `IReadModels.GetInstanceById()` with the resolved identity
5. **Subject Release**: If the command context has a resolved `Subject` and the read model exists, Arc releases the read model with that subject before returning it
6. **Instance Return**: The loaded read model instance is returned, or `null` is returned when the projection instance does not exist

### Id/Key Resolution Strategies

The system provides multiple strategies for resolving the identity used to load read models. The command can provide its identity through any of these approaches:

- **[Key] Attribute**: Mark a property with `[Key]` attribute (most explicit and recommended)
- **EventSourceId Type**: Have a property of type `EventSourceId` or a type that inherits from it
- **ICanProvideEventSourceId Interface**: Implement the interface and return the id from `GetEventSourceId()`
- **Tuple Composition**: Be part of a tuple that contains an `EventSourceId`

The `[Key]` attribute approach is the most flexible as it works with any type (Guid, string, int, etc.) and clearly communicates intent in the command definition.

## Example Usage

### Validation with Read Models

Read models are particularly useful in validators since they provide the current projected state before the command handler runs. A common case is uniqueness validation, where the read model being missing means the command can continue:

```csharp
public record RegisterCustomer([Key] Guid CustomerId, string Name);

public class RegisterCustomerValidator : CommandValidator<RegisterCustomer>
{
    public RegisterCustomerValidator(Customer? customer)
    {
        RuleFor(_ => customer)
            .Null()
            .WithMessage("Customer is already registered");
    }
}
```

For commands that target existing event sources, a missing read model might be a validation failure, but it might also indicate projection lag, a projection rebuild, or a state check that should use aggregate/event-stream state instead. Choose nullable when the command intentionally handles absence; choose non-nullable when absence should fail as required state.

When the projection is required state, inject the read model as non-nullable and write the validator against the projected state directly:

```csharp
public record SubmitOrder([Key] Guid OrderId);

public class SubmitOrderValidator : CommandValidator<SubmitOrder>
{
    public SubmitOrderValidator(OrderReadModel order)
    {
        RuleFor(_ => order.Status)
            .Equal(OrderStatus.ReadyForSubmission)
            .WithMessage("Only orders that are ready for submission can be submitted");

        RuleFor(_ => order.Lines)
            .NotEmpty()
            .WithMessage("Order must have at least one line");
    }
}
```

In this shape, a missing `OrderReadModel` is not a validation outcome. Arc fails the command with `CannotResolveValidatorDependency`, because the validator declared that the projection is required.

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
        // Use read model state as contextual information
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

Read models are particularly powerful when used in `CommandValidator<>` for complex validation scenarios. You can inject read models directly into your validator's constructor through dependency injection.

If the command intentionally handles a missing read model, make the constructor parameter nullable. This is the shape to use when absence is part of the business rule:

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
    public AssignPersonToRoleValidator(RoleReadModel? role)
    {
        RuleFor(x => x.PersonId)
            .NotEmpty()
            .WithMessage("Person ID is required");

        RuleFor(x => role)
            .NotNull()
            .WithMessage("Role does not exist");

        When(_ => role is not null, () =>
        {
            RuleFor(x => x.PersonId)
                .Must(personId => !role!.AssignedPersonIds.Contains(personId))
                .WithMessage("Person is already assigned to this role");

            RuleFor(x => x)
                .Must(_ => role!.Status == RoleStatus.Active)
                .WithMessage("Cannot assign people to inactive roles");

            RuleFor(x => x)
                .Must(_ => role!.AssignedPersonIds.Length < role.MaxAssignments)
                .WithMessage(_ => $"Role has reached maximum assignments of {role!.MaxAssignments}");
        });
    }
}
```

Key points when using read models in validators:

- **Constructor Injection**: Read models are automatically resolved and injected, just like in `Provide()` and `Handle()`
- **Nullable Means Handled Absence**: Use a nullable read model parameter when "does not exist" is a valid state that validation should handle
- **Non-Nullable Means Required**: Use a non-nullable read model parameter when the command requires the projection to exist; it fails if the read model cannot be resolved or resolves to `null`
- **Event Source ID**: The read model instance is resolved using the same event source ID from the command
- **Subject-aware release**: If the command resolved a `Subject` and the read model exists, the injected read model is released with that subject before validation runs
- **Validation Context**: Useful for validating against projected state before executing the command
- **Rich Rules**: Access to full read model state enables complex business rule validation
- **Async Validation**: Use `MustAsync` for asynchronous validation rules that need to check external systems

The Arc analyzer reports `ARC0006` when a command-scoped read model parameter is non-nullable in a validator, `Provide()`, or `Handle()`. See [ARC0006](../code-analysis/ARC0006.md) for details.

> [!NOTE]
> Read-model injection into validators works when commands run through the Arc command pipeline — minimal-API command endpoints (the default) and `ICommandPipeline` directly. It does **not** work when commands are exposed through MVC controllers, because MVC model validation runs during model binding, before the command context (and therefore the event source id) exists. In that case the validator cannot be constructed and the request fails with a `ReadModelValidatorRequiresCommandPipeline` error (or, when development-time scope validation is enabled, the underlying scoped-service resolution error). Expose the command through a minimal-API endpoint, or move the read-model based check into the command's `Handle()` method.

For more details on command validation, see the [Validation](../commands/validation.md) documentation.

## Error Handling

### UnableToResolveReadModelFromCommandContext

This exception is thrown when:

- No identity/key is available in the command context
- The resolved identity is `EventSourceId.Unspecified`

```csharp
public record InvalidCommand(string SomeProperty);
// No identity property, attribute, or interface implementation

// This will fail because no identity can be resolved
```

### CannotResolveCommandDependency and CannotResolveValidatorDependency

These exceptions are thrown when Arc needs to invoke `Provide()`, `Handle()`, or a discoverable command validator and a required, non-nullable dependency cannot be resolved or resolves to `null`.

For command-scoped read models, this usually means the read model does not exist for the resolved event source id. Make the parameter nullable if absence is part of the business rule; keep it non-nullable if absence should fail as required state:

```csharp
public class RemoveContactValidator : CommandValidator<RemoveContact>
{
    public RemoveContactValidator(Customer? customer)
    {
        RuleFor(_ => customer)
            .NotNull()
            .WithMessage("Customer is not registered");
    }
}
```

## Lifecycle Management

### Command Scope

Read models are registered as scoped services, meaning:

- The current projected state is fetched once per command and shared across the command's validator and handler
- The instance is tied to the specific identity/key resolved from the command context
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
