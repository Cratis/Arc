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
