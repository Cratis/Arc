---
title: Migrate generated identifiers
description: Move identifier generation into generated Handle parameters and replace identity-source queues with named scenario pins.
---

Calling `CartId.New()` inside `Handle()` hides the identifier from your specs. Moving it to
a command property would expose it to clients. Instead, move generation to a
`[GeneratedValue]` Handle parameter and pin that parameter by name in command scenarios.

This guide assumes an Arc command and an identifier implementing `IGeneratable<TSelf>`
from Cratis.Fundamentals 7.23.0 or later. See [declaring a server-owned identifier](../backend/csharp/commands/generated-values.md#declare-a-server-owned-identifier)
for the complete `CartId` declaration used in the excerpts below.

## Replace generation inside Handle

Before, the command creates its identifier directly. This command excerpt uses the `CartId`
type linked above and imports `Cratis.Arc.Commands.ModelBound`:

```csharp
[Command]
public record AllocateCartId
{
    public CartId Handle() => CartId.New();
}
```

Replace that command with a generated parameter:

```csharp
[Command]
public record AllocateCartId
{
    public CartId Handle([GeneratedValue("cart")] CartId cartId) => cartId;
}
```

Arc now calls `CartId.New()`, validates the value, and supplies it to `Handle()`. The value
remains outside the command's request properties. Keep identifiers supplied by clients,
such as an existing cart to update, on command properties; they are not generated values.

## Use the editor migration

[ARC0028](../backend/csharp/code-analysis/ARC0028.md) identifies concept generation inside
`Handle()`. Use **Move to a [GeneratedValue] parameter** for a supported single expression,
then choose a stable generated name and update direct calls to the handler. The fix recognizes
`X.New()`, `GenerateValue.*(...)`, and `Guid.NewGuid()` converted or wrapped into a concept
that implements a generation contract. A raw primitive cannot become a generated parameter.

The fix does not move generation inside loops, branches, or nested functions, or discard
nonconstant strategy arguments. Migrate these deliberately: moving a per-iteration or
conditional allocation into a Handle parameter changes when and how many values are created.
Review the concept's factory too; it must own the strategy that the handler previously used.

## Move time-dependent strategies to the context contract

For concepts depending on time or tenant, use Fundamentals 7.24.0's
`IGeneratableInContext<TSelf>` and `New(GenerationContext)` instead of reading the clock
inside `New()`. Follow [context-dependent generation](../backend/csharp/commands/generated-values.md#generate-with-the-commands-time-and-tenant)
for the complete declaration. Arc passes the command's `ReceivedAt` and mapped tenant.
If both interfaces remain, Arc uses only the context form and ARC0027 warns about the plain form.
Manually constructed contexts need a received time; a default time fails, rather than using `UtcNow`.

In scenarios, use [Clock and Tenant](../backend/csharp/testing/generated-values.md#fix-the-clock-and-tenant)
before execution. Continue pinning exact identifiers when assertions depend on the whole value:
a fixed clock does not make a random suffix deterministic.

## Replace identity-source queues with named pins

Arc v22.65.3 removed `IIdentitySource` and `CommandScenario.Generate(Guid[])`. Remove the
identity-source dependency and calls from your handler, and use a generated parameter
instead. If your identifier does not yet implement `IGeneratable<TSelf>`, add that
interface and its static `New()` strategy as in the declaration linked above.

Replace queued raw Guids in specs with typed, named pins. With `AllocateCartId` and `CartId`
from above, import `Cratis.Arc.Commands`, `Cratis.Arc.Testing.Commands`, and
`Cratis.Specifications`. This is an execution fragment in an async spec:

```csharp
using var scenario = new CommandScenario<AllocateCartId>();
var cartId = CartId.New();
scenario.PinGeneratedValue("cart", cartId);
var result = await scenario.Execute(new AllocateCartId());
result.ShouldBeSuccessful();
scenario.GeneratedValues.Last<CartId>("cart").ShouldEqual(cartId);
```

The pin identifies both the command type and generated name, rather than relying on the
order in which handlers consume a queue. The same pin is reused on every execution of
that command in the scenario. Create separate scenarios when executions need different
pinned identifiers, or use `AllowGeneration()` when their exact values do not matter.

Scenarios require every generated parameter to be pinned unless you explicitly allow
generation. Configure pins and generation policy before the first execution. For multiple
parameters, nested commands, and reading all captures, follow
[pinning generated values in command specs](../backend/csharp/testing/generated-values.md).
