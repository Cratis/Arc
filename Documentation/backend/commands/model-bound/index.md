---
title: Model-bound commands
description: Define standalone Arc commands with service dependencies and ordinary response values.
---

When an operation needs an HTTP endpoint and a typed client, put its input and behavior together. Arc discovers a `[Command]` type with a public instance `Handle()` method and calls that method through the command pipeline. No event store is required.

## Model cart values

Give cart values names that express their purpose: `CartLineId` identifies a line, `Sku` identifies a product, and `Quantity` counts units. These types make service signatures readable and let the C# compiler catch accidental swaps with unrelated identifiers or values. Define these shared concepts once in your cart feature, each in its own file:

```csharp
using System;
using Cratis.Concepts;

public record CartLineId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly CartLineId NotSet = new(Guid.Empty);
    public static CartLineId New() => new(Guid.NewGuid());
    public static implicit operator CartLineId(Guid value) => new(value);
}

public record Sku(string Value) : ConceptAs<string>(Value)
{
    public static readonly Sku NotSet = new(string.Empty);
    public static implicit operator Sku(string value) => new(value);
}

public record Quantity(int Value) : ConceptAs<int>(Value)
{
    public static readonly Quantity NotSet = new(0);
    public static implicit operator Quantity(int value) => new(value);
}
```

Arc serializes concepts as their underlying values; the proxy generator maps them to TypeScript value types, not nominal concept classes. The named types protect the C# domain model without adding a wrapper object to the wire contract.

## A service-backed command

:::tip[Separate a decision from its effects]
The direct service call below is supported. When the command should describe work rather than perform it inside `Handle()`, prefer a [command operation](../operations/implementing.md). Its optional `Compensate()` lets Arc manage recovery; [operation specs](../../testing/command-operations.md) can test the decision and the real pipeline independently.
:::

The following application types reuse the shared concepts above and store cart lines **in memory for demonstration**, not in durable storage. Add them to your Arc application's namespace and register `ICartService` as shown below. Declaration snippets omit that application-specific namespace; startup and caller fragments are labeled separately. The returned `CartLineId` is an ordinary response, not an event-source identity.

```csharp
using System;
using System.Collections.Concurrent;
using Cratis.Arc.Commands.ModelBound;

public record CartLine(Sku Sku, Quantity Quantity);

public interface ICartService
{
    CartLineId AddItem(Sku sku, Quantity quantity);
}

public class InMemoryCartService : ICartService
{
    readonly ConcurrentDictionary<CartLineId, CartLine> _lines = new();

    public CartLineId AddItem(Sku sku, Quantity quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku.Value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity.Value);
        var id = CartLineId.New();
        if (!_lines.TryAdd(id, new CartLine(sku, quantity)))
        {
            throw new InvalidOperationException("Could not allocate a cart line identifier.");
        }

        return id;
    }
}

[Command]
public record AddItemToCart(Sku Sku, Quantity Quantity)
{
    public CartLineId Handle(ICartService carts) => carts.AddItem(Sku, Quantity);
}
```

The store actually retains each line for the lifetime of its service instance. This intentionally small sample has no multi-user cart or authorization model; it is not a production shopping-cart implementation. Add [input validation](../validation.md) for friendly rejection messages and [authorization](./authorization.md) before exposing protected operations.

Registration fragment in your existing Arc application's startup, with `Microsoft.Extensions.DependencyInjection` imported:

```csharp
builder.Services.AddSingleton<ICartService, InMemoryCartService>();
```

At the execution checkpoint, inject `ICommandPipeline` into your caller and await it:

```csharp
var result = await pipeline.Execute<CartLineId>(new AddItemToCart("BOOK-1", 2));
```

For valid input, `result.IsSuccess` is `true` and `result.Response` is the new line identifier. The registration and call are host fragments, not separate programs. See [programmatic execution](../command-pipeline.md) for scope and failure handling.

## Return types

| Handler return | Pipeline behavior |
| --- | --- |
| `void` or `Task` | No response value. Arc awaits a returned task before completing the command. |
| `T` or `Task<T>` | A value with no matching response handler becomes the response. |
| `Result<TSuccess, TError>` | Arc processes the active alternative; its name alone does not make it a failure. |
| `ICommandOperation` implementation | Arc executes the returned operation on the server; it is not the response. |
| `CommandOperations` | Explicit ordered batch of zero-to-many operations. |
| Tuple | Operations and other handled values are processed on the server; at most one unhandled value becomes the response. |

`Result` belongs to **`Cratis.Monads`**, not the `OneOf` namespace. Arc also understands `OneOf` wrappers. A singular `Cratis.Arc.Validation.ValidationResult` has a built-in handler; an arbitrary error record does not automatically mark the command unsuccessful.

Do not return several unhandled tuple values: the pipeline reports `MultipleUnhandledTupleValues` as an exception-bearing command result.

## Result with tuple alternatives

Use a tuple alternative when success needs both a caller response and a server-side handled value. In `Result<(Guid, AllocationLog), ValidationResult>`, Arc processes the active success tuple by returning the `Guid` and handling the `AllocationLog`. An active `ValidationResult` error instead records a validation failure without processing the success tuple.

For the complete standalone command and required custom handler, see the [canonical example](../response-value-handlers.md#creating-custom-value-handlers). The [response value handling overview](../response-value-handlers.md#result-with-tuple-alternatives) explains this pattern.

## Dependencies and provided data

Arc resolves `Handle()` service parameters from the command's service scope. A `Provide()` method can fetch data after filters succeed and pass it to `Handle()`. Keep fetching separate when that makes the handler easier to test; a service-backed `Handle()` is equally valid. For the write side of that separation, return [command operations](../operations/index.md): `Provide()` acquires inputs, `Handle()` decides, and the operations perform the declared work.

`CancellationToken` is special: Arc supplies the execution token rather than resolving it from DI. Both `Provide()` and `Handle()` may accept it. HTTP execution uses the request-aborted token; direct callers can [pass cancellation to the pipeline](../command-pipeline.md#cancellation).

For the full `Provide()` contract, including its distinct validation control values, see [provide data to a command](../../../scenarios/provide-data-to-a-command.md).

## Frontend integration

The [proxy generator](../../proxy-generation/index.md) uses the command type name for the generated TypeScript command. It generates typed properties, supported validation rules, and response handling. Build the backend before consuming the proxy; use [commands in React](../../../frontend/react/commands/index.md) to call it.

[Chronicle command integration](../../chronicle/commands/index.md) is optional. Its returned-event behavior is an integration contract, not a requirement for a model-bound Arc command.
