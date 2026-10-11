---
title: Generated values
description: Give command handlers server-owned identifiers without adding them to the request contract.
---

An identifier created inside `Handle()` belongs to the server, but a test cannot choose it.
Putting that identifier on the command instead lets clients choose it too. Declare a
`[GeneratedValue]` Handle parameter to keep it out of the request and give Arc responsibility
for creating, validating, and capturing it.

## Declare a server-owned identifier

In an existing Arc application, define an identifier that implements `IGeneratable<TSelf>`
from Cratis.Fundamentals 7.23.0 or later. Its static `New()` method owns the generation strategy.
These declarations allocate an identifier; they do not persist a cart or append an event:

```csharp
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Concepts;

public record CartId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<CartId>
{
    public static CartId New() => new(GenerateValue.Uuid());
}

[Command]
public record AllocateCartId
{
    public CartId Handle([GeneratedValue("cart")] CartId cartId) => cartId;
}
```

Arc calls the concept's strategy before invoking `Handle()`. The `cartId` parameter is
not a command property, so it does not appear in the request body or generated client
inputs. The handler returns the allocated identifier as its ordinary response.

Use `GenerateValue.UuidV7()` instead when time-ordered UUIDs suit your domain. UUIDv7 reveals
creation time and does not guarantee monotonic order within a millisecond. Both strategies
create UUIDs, not proof that a value is absent from your storage.

## Names and resolution

`[GeneratedValue]` uses the C# parameter name. `[GeneratedValue("cart")]` supplies an
explicit name that remains stable if you rename the parameter. Names are case-sensitive
and must be non-empty and unique within the command.

Arc generates values only after `Provide()` control results allow the command to continue.
A value returned by `Provide()`, or registered in dependency injection, never overrides a
generated parameter. Unmarked parameters retain their normal Provide and dependency
injection behavior.

The Arc source generator registers a typed factory for each accessible generatable type.
Commands compiled without it can use a cached reflection fallback under the JIT. When
dynamic code is unavailable, a missing factory fails at handler construction with
`MissingGeneratedValueFactory`, naming the command and parameter. This factory path alone
does not make the whole application NativeAOT-compatible.

## Validation and captures

Arc resolves the concept's discoverable validator from the command's scoped provider and
runs it asynchronously with the command's cancellation token. Error failures reject the
generated value with `GeneratedValueRejected`: this is a server defect, not invalid request
input, so the command result contains an exception rather than a 400 validation failure.
Warnings and information allow execution; warning logs contain names and types only.

`CommandContext.GeneratedValues` exposes validated captures in parameter resolution order.
Each entry has a zero-based `Order`, a `Name`, its declared `Type`, and its `Value`.
`Get<CartId>("cart")` retrieves the latest matching capture and fails if none exists.
Context copies retain the same captures.

Do not log or trace captured values. Validator failure text can interpolate a value;
`GeneratedValueRejected.Failures` keeps that text separate from the safe exception message.
Rejected values are not captured.

## When to use it

Use generated parameters for new identifiers owned by the server. Keep existing identifiers,
client-chosen references, and business input on command properties. Use ordinary Handle
dependencies for services and read models, not `[GeneratedValue]`.

Only non-nullable types implementing `IGeneratable<TSelf>` for themselves are valid. A raw
`Guid`, a nullable concept, duplicate names, or a generated parameter on `Provide()` fails
at handler construction. Generation has no freshness guard or tenant, clock, or pattern
context in this phase.

## Analyzers

Arc checks generated declarations in the IDE and at compile time:
[ARC0022](../code-analysis/ARC0022.md) rejects invalid or nullable types,
[ARC0023](../code-analysis/ARC0023.md) rejects attributes outside command Handle parameters,
[ARC0024](../code-analysis/ARC0024.md) rejects empty or duplicate names, and
[ARC0025](../code-analysis/ARC0025.md) warns when `Provide()` supplies a generated type.
Startup validation remains in place for commands compiled without the analyzers.

## Testing

Inspect captures through the [command context](command-context.md), or
[pin generated values in command specs](../testing/generated-values.md) with
`CommandScenario<TCommand>`. Scenarios require named pins by default; use `AllowGeneration()`
when the exact value is irrelevant.
