# Model Bound Commands

For a more lightweight approach; commands can be their own handlers.
This is achieved by adorning your command with the `[Command]` attribute
and implementing a `Handle()` method.

```csharp
[Command]  // The command attribute is needed
public record AddItemToCart(string Sku, int Quantity)
{
    public void Handle()
    {
        // Handle the command
    }
}
```

> **Note**: If you're using the Cratis ApplicationModel [proxy generator](../../proxy-generation/index.md), the name of the type
> will become the name of the command for the generated TypeScript file and class.

If your handler has side-effects expressed in the return value, the
command pipeline has an [extensibility point for return values](../response-value-handlers.md).

You can then return anything you know there is a handler for.

## Discriminated Union

Your return type can leverage a discriminated union with [`OneOf<>`](https://github.com/mcintyre321/OneOf) to
return different types of values depending on the situation, like for instance an explicit validation error.

As long as there are [response handlers](../response-value-handlers.md) for any of the types of the discriminated union, your value
will be handled.

```csharp
using Cratis.Arc.Validation;
using OneOf;

[Command]
public record AddItemToCart(string Sku, int Quantity)
{
    public Result<ValidationResult, Guid> Handle()
    {
        if( /* code that checks if product is carried */ )
        {
            var cartLineIdentifier = Guid.NewGuid();

            // Logic for handling the actual adding...

            return cartLineIdentifier;
        }

        return ValidationResult.Error("Product is not carried anymore");
    }
}
```

### Result with Tuple Alternatives

You can also combine `Result` with tuples, allowing different alternatives to return different structures. This is useful when some code paths need to return a response with side effects (events, notifications), while others just return a simple value or error.

```csharp
using Cratis.Arc.Validation;
using OneOf;

[Command]
public record CreateOrder(string CustomerId, List<OrderItem> Items)
{
    public Result<ValidationResult, (OrderId, OrderCreated)> Handle()
    {
        if (!IsValidOrder())
        {
            return ValidationResult.Error("Invalid order");
        }

        var orderId = OrderId.New();
        
        // Create the order...

        // Return tuple with response (OrderId) and event (OrderCreated)
        return (orderId, new OrderCreated(orderId, CustomerId, Items));
    }
}
```

In this example:

- When validation fails, the `ValidationResult` is returned and processed by the validation handler
- When successful, the tuple `(OrderId, OrderCreated)` is returned:
  - `OrderCreated` is processed by its response value handler (e.g., Chronicle event handler)
  - `OrderId` becomes the command response (available in `CommandResult<OrderId>`)

This pattern is particularly powerful when you want to:

- Return different types based on business logic outcomes
- Combine response values with side effects in success scenarios
- Keep error handling separate from success handling

## Tuple

Sometimes you want to return a value that is part of the `CommandResult` and returned to the
caller that invoked the command. By returning a tuple, the command pipeline will intelligently
process each value to determine which should be the response and which should be processed by
[response value handlers](../response-value-handlers.md).

### How Tuple Processing Works

The command pipeline processes tuples as follows:

1. **Checks each value** against available response value handlers using their `CanHandle` method
2. **Values with handlers** are processed by their respective response value handlers
3. **Values without handlers** are considered potential response values
4. **If exactly one value has no handler**, it becomes the response in the `CommandResult`
5. **If multiple values have no handlers**, a `MultipleUnhandledTupleValuesException` is thrown
6. **If all values have handlers**, no response value is set

### Simple Tuple (2 values)

```csharp
using Cratis.Arc.Validation;

[Command]
public record AddItemToCart(string Sku, int Quantity)
{
    public (Guid, ItemAddedToCart) Handle()
    {
        var cartLineIdentifier = Guid.NewGuid();

        // Logic for handling the actual adding...

        // Return the identifier and the consequence, in this case an event handled by Chronicle.
        return (cartLineIdentifier, new ItemAddedToCart(...));
    }
}
```

In this example, if `ItemAddedToCart` has a response value handler but `Guid` doesn't, then the `Guid` becomes the response.

### Multi-dimensional Tuples (3+ values)

The system supports tuples with any number of values:

```csharp
[Command]
public record ProcessOrder(string OrderId)
{
    public (Guid, OrderProcessed, ValidationResult, NotificationSent) Handle()
    {
        var confirmationId = Guid.NewGuid();
        
        // Processing logic...
        
        return (
            confirmationId,           // Response (if no handler exists for Guid)
            new OrderProcessed(...),  // Event (handled by event handler)
            validationResult,         // Validation (handled by validation handler)
            new NotificationSent(...) // Notification (handled by notification handler)
        );
    }
}
```

In this example:

- `OrderProcessed`, `ValidationResult`, and `NotificationSent` would be processed by their respective handlers
- `Guid` (confirmationId) would become the response value
- If multiple values lack handlers, an exception would be thrown

### Error Scenarios

If your tuple contains multiple values that don't have corresponding response value handlers, the system will throw a `MultipleUnhandledTupleValuesException` with details about which values couldn't be handled:

```csharp
// This would throw an exception if neither string nor int have handlers
public (string, int, SomeEvent) Handle() => ("response1", 42, new SomeEvent());
```

### Tuples with Result Values

Tuples can also contain `Result` values. The command pipeline will unwrap the `Result` and process the inner value:

```csharp
using Cratis.Arc.Validation;
using OneOf;

[Command]
public record ProcessPayment(string OrderId, decimal Amount)
{
    public (OrderId, Result<PaymentFailed, PaymentSucceeded>) Handle()
    {
        var orderId = new OrderId(OrderId);
        
        if (ProcessPaymentWithProvider())
        {
            return (orderId, new PaymentSucceeded(orderId, Amount));
        }
        
        return (orderId, new PaymentFailed(orderId, "Insufficient funds"));
    }
}
```

In this example:

- `OrderId` becomes the response (assuming no handler exists for it)
- The `Result` value is unwrapped, and the inner value (`PaymentSucceeded` or `PaymentFailed`) is processed by its respective handler

## Dependencies

Your command handler method can also take dependencies to any services configured in the
service collection. This is done by just specifying your dependencies on the methods signature:

```csharp
using Cratis.Arc.Validation;

[Command]
public record AddItemToCart(string Sku, int Quantity)
{
    public void Handle(ICartService carts)
    {
        carts.AddItemToCart(Sku, Quantity);
    }
}
```

## Frontend Integration

Model-bound commands work seamlessly with the [proxy generator](../../proxy-generation/index.md), which automatically creates TypeScript proxies for your commands. The generated proxies provide:

- Strong typing for command properties
- Automatic validation integration
- React hooks for easy frontend integration
- Consistent error handling and response processing
- Authorization status handling in command results
