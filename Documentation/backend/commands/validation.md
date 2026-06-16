# Validation

Commands can be validated by either using [FluentValidation](https://docs.fluentvalidation.net/en/latest/) or
the attribute based validators found in the `System.ComponentModel.DataAnnotations` namespace.

The validators are performed before the commands handler method is invoked. If any validators cause a
validation error, it will not invoke the command handler and just return a `CommandResult` with the errors
in it.

> **💡 Client-Side Validation**: When using FluentValidation, validation rules are automatically extracted by the [ProxyGenerator](../proxy-generation/validation.md) and run on the client before server calls. This provides immediate feedback to users and reduces unnecessary server requests.

## Data Annotations

Depending on how you like to do your validation, with data annotations you can adorn a value on
a command directly. This can be helpful if you're trying to keep things lightweight and very
cohesive.

```csharp
[Command]
public record AddItemToCart(
    [Required] string Sku,
    int Quantity)
{
    public void Handle()
    {
        // Handle the command
    }
}
```

The code adds the `[Required]` attribute to the `sku` property.

> Note: The required attribute can also take a specific error message.

## Fluent Validation

When using [FluentValidation](https://docs.fluentvalidation.net/en/latest/) you get more control
of an flexibility the flow of validation. For instance, the validator can take dependencies and
with it you can call other systems that has the required knowledge for the validation rules you
want for your commands.

Given the same sample as for Data Annotation, this would be like the following:

```csharp
[Command]
public record AddItemToCart(
    string Sku,
    int Quantity)
{
    public void Handle()
    {
        // Handle the command
    }
}


public class AddItemToCartValidators : CommandValidator<AddItemToCart>
{
    public AddItemToCartValidators()
    {
        RuleFor(c => c.Sku).NotEmpty().WithMessage("You have to provide a Sku");
    }
}
```

The code shows the `AddItemToCartValidators` class implementing the `CommandValidator<>`,
which is required for validating commands. It makes the validator discoverable by the system
and you don't have to register it anywhere.

### Validator Dependencies

Command validators can take dependencies through their constructors. Arc resolves those dependencies from the same command scope used by `Provide()` and `Handle()`, so validators can check current state before the command handler runs.

```csharp
public class RemoveContactValidator : CommandValidator<RemoveContact>
{
    public RemoveContactValidator(Customer? customer)
    {
        RuleFor(_ => customer)
            .NotNull()
            .WithMessage("Customer is not registered");

        When(_ => customer is not null, () =>
        {
            RuleFor(command => command.ContactId)
                .Must(contactId => customer!.Contacts.Contains(contactId))
                .WithMessage("Contact is not assigned to this customer");
        });
    }
}
```

Nullable dependency parameters are allowed. If the dependency cannot be resolved, or resolves to `null`, Arc injects `null` into a nullable parameter. Use this when missing state is part of the command's valid behavior, including read-model existence checks that should become validation messages.

Non-nullable dependency parameters are treated as required. If Arc cannot resolve the dependency, or the resolved value is `null`, it throws a clear dependency-resolution exception instead of constructing the validator with an invalid value. For Chronicle read models, this is a deliberate choice that the projection is required to exist for the command.

```csharp
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

The same dependency behavior applies to dependencies on `Provide()` and `Handle()` parameters. For Chronicle read models, the analyzer rule [ARC0006](../code-analysis/ARC0006.md) warns when a command-scoped read model parameter is non-nullable in a validator, `Provide()`, or `Handle()` so the required-state choice is explicit.
