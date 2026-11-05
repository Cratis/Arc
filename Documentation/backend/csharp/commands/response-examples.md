---
title: Cratis Arc command response examples
description: Read Cratis Arc command responses, handle validation failures, and resolve tuple ambiguity while checking the final result before using a value.
---

A Cratis Arc command response is the value left for the caller after response handlers have processed the handler's return value. Await execution and check `IsSuccess` before using it: a value returned by `Handle()` can still be cleared if a later step fails.

Start with the [service-backed command](./model-bound/index.md#a-service-backed-command), whose `AddItemToCart.Handle()` returns a `CartLineId` concept. For a tuple that combines response data with a server-side action, use the [custom value handler example](./response-value-handlers.md#creating-custom-value-handlers).

The snippets below are caller fragments for those definitions, with `ICommandPipeline pipeline` supplied through dependency injection. They are not independent programs.

## Simple value response

```csharp
var result = await pipeline.Execute<CartLineId>(new AddItemToCart("BOOK-1", 2));
if (result.IsSuccess)
{
    Console.WriteLine(result.Response);
}
```

`Execute<CartLineId>` returns `CommandResult<CartLineId>`. The concept needs no custom response handler and is serialized as its underlying `Guid`. A primitive value can also be a response, as the identifier-allocation example below shows. Returning either kind of value does not make it an event-source identity.

## Result and tuple response

```csharp
var result = await pipeline.Execute<Guid>(new AllocateIdentifier("invoice"));
if (result.IsSuccess)
{
    Console.WriteLine($"Allocated {result.Response}");
}
```

The active success alternative of `Result<(Guid, AllocationLog), ValidationResult>` contains a tuple. `AllocationLogHandler` consumes the log value; the `Guid` becomes the response. Without that handler, both values are unhandled and the pipeline reports an exception outcome.

## Singular validation result

```csharp
var result = await pipeline.Execute<Guid>(new AllocateIdentifier(""));
foreach (var failure in result.ValidationResults)
{
    Console.WriteLine(failure.Message);
}
```

This prints `A label is required`. Arc's `Cratis.Arc.Validation.ValidationResult` represents one failed rule and has no `IsValid` property. `CommandResult.IsValid` checks the command result's collection of failures; it does not establish authorization or the absence of exceptions. FluentValidation's separate `FluentValidation.Results.ValidationResult` is an aggregate with `IsValid` and `Errors`.

Use a discovered `CommandValidator<T>` for FluentValidation rules so Arc converts its failures. Do not return FluentValidation's aggregate or an Arc validation array from `Handle()` expecting the singular Arc response handler to interpret it. See [validation](./validation.md) and the [response contract](./response-value-handlers.md#automatic-response-handling).

## Multiple unhandled values

This complete command is a deliberately invalid response-contract example. With no handlers for `string` or `int`, both values compete to be the response:

```csharp
using Cratis.Arc.Commands.ModelBound;

[Command]
public record AmbiguousResponse()
{
    public (string, int) Handle() => ("first", 42);
}
```

Awaiting `pipeline.Execute(new AmbiguousResponse())` returns an exception outcome (`HasExceptions == true`) carrying `MultipleUnhandledTupleValues`. Wrap related response fields in one response record, or provide a handler for the value you intend to consume server-side.

## Failure and absent responses

When no response was selected, the typed overload supplies `default(TResult)`: `null` for a reference type, `Guid.Empty` for `Guid`, and `0` for `int`.

Failed execution clears an already selected response to the default of its concrete response type. Requesting that same type yields `default(TResult)`, including `Guid.Empty`. Adapting a cleared value-type response to `object` or a compatible interface can preserve its boxed default instead of null. For example, `Execute<object>` can return a boxed `Guid.Empty` after scope completion fails. A non-null response is therefore not evidence of success; check `IsSuccess`.

Likewise, naming an alternative `PaymentFailed` in a `Result` does not make it a failure: unless a handler interprets it, it is ordinary response data.

See [typed command results](./command-pipeline.md#typed-command-results) for assignable response types and type mismatches, and [execution scopes](./command-execution-scopes.md) for failures after response processing.
