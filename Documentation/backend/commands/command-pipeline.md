# Command Pipeline

The `ICommandPipeline` service provides a way to execute commands programmatically, bypassing the HTTP layer. This is useful for scenarios where you need to execute commands from within your application code rather than through HTTP requests.

## When to Use ICommandPipeline

The command pipeline is particularly useful for:

- **Background services or scheduled tasks** - Execute commands as part of scheduled jobs
- **Event handlers** - React to events by executing commands
- **Internal service-to-service communication** - Execute commands between services without HTTP overhead
- **Testing scenarios** - Execute commands directly in integration tests
- **Saga or workflow orchestration** - Coordinate multiple commands as part of a larger workflow

## Basic Usage

`ICommandPipeline` provides two forms for every operation: a **scope-free** form that creates its own service scope automatically, and a **scope-explicit** form where you supply the `IServiceProvider` yourself.

### Without a service provider (recommended for most cases)

Inject `ICommandPipeline` and call `Execute` directly. The pipeline creates and disposes a dedicated service scope for each call — no manual scope management needed:

```csharp
public class OrderProcessingService
{
    readonly ICommandPipeline _commandPipeline;

    public OrderProcessingService(ICommandPipeline commandPipeline)
    {
        _commandPipeline = commandPipeline;
    }

    public async Task ProcessOrder(Order order)
    {
        var result = await _commandPipeline.Execute(new ProcessOrderCommand(order.Id, order.Items));

        if (result.IsSuccess)
        {
            // Command executed successfully
        }
        else
        {
            foreach (var error in result.ValidationResults)
            {
                // Process validation errors
            }
        }
    }
}
```

This is the right choice for background services, scheduled tasks, and any code that does not live inside an existing DI scope.

### With a service provider (share an existing scope)

If you are already inside a scoped lifetime — for example a Reactor, an event handler, or an HTTP endpoint — pass the current `IServiceProvider` so handler dependencies share the same scope as the caller:

```csharp
public class OrderCreatedReactor
{
    readonly ICommandPipeline _commandPipeline;
    readonly IServiceProvider _serviceProvider;

    public OrderCreatedReactor(ICommandPipeline commandPipeline, IServiceProvider serviceProvider)
    {
        _commandPipeline = commandPipeline;
        _serviceProvider = serviceProvider;
    }

    public async Task Handle(OrderCreated @event)
    {
        var result = await _commandPipeline.Execute(
            new SendOrderConfirmation(@event.OrderId, @event.CustomerEmail),
            _serviceProvider);
    }
}
```

## Command Results

The `ICommandPipeline.Execute()` method returns a `CommandResult` with comprehensive information about the execution:

```csharp
var result = await _commandPipeline.Execute(command);

if (!result.IsAuthorized)
{
    // Handle unauthorized access — the command was not executed
}

if (result.IsSuccess)
{
    // Command executed successfully
}
else
{
    // Handle validation errors
    foreach (var validationResult in result.ValidationResults)
    {
        // Process each validation error
    }
}
```

### CommandResult Properties

| Property | Type | Description |
| -------- | ---- | ----------- |
| `IsSuccess` | `bool` | Whether the command executed successfully |
| `IsAuthorized` | `bool` | Whether the user was authorized to execute the command |
| `IsValid` | `bool` | Whether the command passed validation |
| `HasExceptions` | `bool` | Whether any exceptions occurred during execution |
| `ValidationResults` | `IEnumerable<ValidationResult>` | Validation errors if the command failed validation |
| `ExceptionMessages` | `IEnumerable<string>` | Exception messages if exceptions occurred |
| `CorrelationId` | `CorrelationId` | The correlation ID for tracking the command |

When using the generic `Execute<TResult>` overload, the returned `CommandResult<TResult>` adds one more property:

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Response` | `TResult?` | The typed value returned by the command handler, or `null` if the command did not succeed or returned no value |

## Exception Handling

When using `ICommandPipeline` programmatically, exceptions in the command handler are caught and returned as part of the `CommandResult`:

```csharp
var result = await _commandPipeline.Execute(command);

if (result.HasExceptions)
{
    // An exception was thrown during command execution
    foreach (var message in result.ExceptionMessages)
    {
        _logger.LogError("Command failed: {Message}", message);
    }
}
```

## Validation Without Execution

The `Validate` method runs authorization and validation filters without invoking the command handler. It follows the same two forms as `Execute`.

**Without a service provider:**

```csharp
var validationResult = await _commandPipeline.Validate(command);

if (validationResult.IsValid)
{
    var result = await _commandPipeline.Execute(command);
}
```

**With a service provider (to share an existing scope):**

```csharp
var validationResult = await _commandPipeline.Validate(command, _serviceProvider);

if (validationResult.IsValid)
{
    var result = await _commandPipeline.Execute(command, _serviceProvider);
}
```

This is useful for pre-flight validation before committing to command execution.

## Context and Authentication

When executing commands programmatically, the current execution context (including user identity and claims) is automatically used. The command pipeline respects:

- **Correlation ID** - Automatically tracked for request tracing
- **User context** - The current user's identity and claims are used for authorization
- **Tenant context** - Multi-tenancy context is preserved

If you need to execute commands under a different context, you'll need to manage the authentication context appropriately in your application.

## Background Service Example

Here's an example of using `ICommandPipeline` in a background service:

```csharp
public class OrderExpirationService : BackgroundService
{
    readonly ICommandPipeline _commandPipeline;
    readonly IOrderRepository _orderRepository;
    readonly ILogger<OrderExpirationService> _logger;

    public OrderExpirationService(
        ICommandPipeline commandPipeline,
        IOrderRepository orderRepository,
        ILogger<OrderExpirationService> logger)
    {
        _commandPipeline = commandPipeline;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var expiredOrders = await _orderRepository.GetExpiredOrders();

            foreach (var order in expiredOrders)
            {
                // Each Execute call creates and disposes its own service scope
                var result = await _commandPipeline.Execute(new ExpireOrder(order.Id));

                if (!result.IsSuccess)
                {
                    _logger.LogWarning(
                        "Failed to expire order {OrderId}: {Errors}",
                        order.Id,
                        string.Join(", ", result.ValidationResults.Select(v => v.Message)));
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## Event Handler Example

Using `ICommandPipeline` in an event handler:

```csharp
public class OrderCreatedEventHandler
{
    readonly ICommandPipeline _commandPipeline;

    public OrderCreatedEventHandler(ICommandPipeline commandPipeline)
    {
        _commandPipeline = commandPipeline;
    }

    public async Task Handle(OrderCreated @event)
    {
        // Send confirmation email when an order is created
        var command = new SendOrderConfirmation(@event.OrderId, @event.CustomerEmail);
        var result = await _commandPipeline.Execute(command);
        
        if (!result.IsSuccess)
        {
            // Handle failure - maybe queue for retry
        }
    }
}
```

## Typed Command Results

When a command handler returns a value, use the generic `Execute<TResult>` overload to get back a `CommandResult<TResult>` with a strongly-typed `Response` property instead of working with `object?`. Both scope forms are available:

```csharp
[Command]
public record CreateOrder(IEnumerable<OrderItem> Items)
{
    public OrderId Handle(IOrderService orderService)
    {
        return orderService.CreateOrder(Items);
    }
}

// Without a service provider — pipeline creates its own scope
var result = await _commandPipeline.Execute<OrderId>(new CreateOrder(items));

// With a service provider — share the caller's scope
var result = await _commandPipeline.Execute<OrderId>(new CreateOrder(items), _serviceProvider);

if (result.IsSuccess)
{
    // response is strongly typed — no cast required
    OrderId orderId = result.Response!;
    await NotifyCustomer(orderId);
}
```

The generic overload covers all the same failure paths as the non-generic one. When the command is unauthorized, fails validation, has no handler, or throws an exception, the result is still a valid `CommandResult<TResult>` — `Response` is just `default`:

```csharp
var result = await _commandPipeline.Execute<OrderId>(new CreateOrder(items));

if (!result.IsAuthorized)
{
    // result.Response is null — command was never executed
}

if (!result.IsValid)
{
    // result.Response is null — validation failed before execution
}

if (result.HasExceptions)
{
    // result.Response is null — an exception was thrown during execution
}
```

If the handler returns a different type from what you requested, an `InvalidCastException` is thrown. This is a programmer error — the type you pass to `Execute<TResult>` must match the type the command handler returns.

If the handler returns no value at all (a `void`-equivalent handler), `Response` is `null`. The non-generic `Execute` overload is equally valid in this case:

```csharp
// Fine when you don't need a typed response
var result = await _commandPipeline.Execute(new CancelOrder(orderId));
```

