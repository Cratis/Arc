---
title: Write model-bound commands in Cratis Arc
description: Write a C# model-bound command in Cratis Arc, inject a service into Handle(), and use the command pipeline to receive a typed response.
---

A model-bound command in Cratis Arc keeps an operation's input and behavior in one C# type. Mark the type with `[Command]` and give it a public instance `Handle()` method; Arc discovers the handler and runs it through the command pipeline. You can return an ordinary value without an event store.

Start with a [configured Arc backend](../../getting-started/index.md). The example below adds a cart line through an injected service, then returns its identifier to the caller. It uses in-memory storage so you can follow the command without setting up a database.

## Model cart values

Use domain-specific types so the compiler can distinguish a cart line identifier from a product code or a quantity. `ConceptAs<T>` wraps an underlying value in a named type: `CartLineId` identifies a line, `Sku` identifies a product, and `Quantity` counts units. Define these concepts in your cart feature, each in its own file:

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

Arc serializes each concept as its underlying value, so the JSON contract carries a GUID, a string, and a number without wrapper objects. The proxy generator also uses the underlying TypeScript value types. The named concepts distinguish values in your C# model; they do not become separate concept classes in TypeScript.

## A service-backed command

:::tip[Separate a decision from its effects]
The direct service call below is supported. When the command should describe work rather than perform it inside `Handle()`, prefer a [command operation](../operations/implementing.md). Its optional `Compensate()` lets Arc manage recovery; [operation specs](../../testing/command-operations.md) can test the decision and the real pipeline independently.
:::

Add these types to the same cart feature, using your application's namespace for them and the concepts above. `AddItemToCart` carries the input values, and its `Handle()` method asks `ICartService` to retain the line. The service returns a `CartLineId` for the caller; this is an ordinary response, not an event-source identity.

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

The dictionary retains each line for the lifetime of the service instance. It is not durable storage and does not separate one user's cart from another's. Add [input validation](../validation.md) to turn invalid input into friendly rejection messages, and [authorize the command](./authorization.md) before exposing a protected operation.

Register the service in your existing application's startup, with `Microsoft.Extensions.DependencyInjection` imported:

```csharp
builder.Services.AddSingleton<ICartService, InMemoryCartService>();
```

Arc discovers the command handler; the registration above supplies its `ICartService` dependency. The singleton keeps the in-memory lines across command executions.

In an async caller, inject `ICommandPipeline` from `Cratis.Arc.Commands` and await execution:

```csharp
var result = await pipeline.Execute<CartLineId>(new AddItemToCart("BOOK-1", 2));
```

For valid input, `result.IsSuccess` is `true` and `result.Response` is the new line identifier. Check `IsSuccess` before using that response: authorization, validation, or execution can still fail. The startup registration and pipeline call belong in your existing host and caller, respectively; see [programmatic execution](../command-pipeline.md) for scope and failure handling.

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

To use this pattern, follow the [complete command and custom handler example](../response-value-handlers.md#creating-custom-value-handlers). Read [how tuple alternatives are processed](../response-value-handlers.md#result-with-tuple-alternatives) when choosing which value should reach the caller.

## Dependencies and provided data

In the cart example, Arc resolves `ICartService` from the command's service scope. When a decision needs data first, add a `Provide()` method to fetch it after filters succeed and pass it to `Handle()`. This separates fetching from the decision and can make the handler easier to test. You can keep service calls in `Handle()`, as above, or return [command operations](../operations/index.md) to describe work that Arc then performs.

`CancellationToken` is special: Arc supplies the execution token rather than resolving it from dependency injection. Both `Provide()` and `Handle()` may accept it. HTTP execution uses the request-aborted token; direct callers can [pass cancellation to the pipeline](../command-pipeline.md#cancellation).

For the full `Provide()` contract, including its distinct validation control values, see [provide data to a command](../../../../scenarios/provide-data-to-a-command.mdx).

## Frontend integration

Once the backend command works, use the [proxy generator](../../proxy-generation/index.md) to generate its TypeScript command, named after the C# type. The proxy includes typed properties, supported validation rules, and response handling. Build the backend to generate it, then follow [commands in React](../../../../frontend/react/commands/index.mdx) to call the command from your UI.

[Chronicle command integration](../../chronicle/commands/index.md) is optional. Its returned-event behavior is an integration contract, not a requirement for a model-bound Arc command.
