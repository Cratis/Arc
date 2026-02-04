# Validation

Commands can be validated by either using [FluentValidation](https://docs.fluentvalidation.net/en/latest/index.md) or
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

When using [FluentValidation](https://docs.fluentvalidation.net/en/latest/index.md) you get more control
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
