---
title: Pin generated values in command specs
description: Choose server-generated identifiers by name in command scenarios and inspect validated captures.
---

A command that allocates an identifier should not accept that identifier from its caller.
With a [generated Handle parameter](../commands/generated-values.md), you can keep the request
contract unchanged and choose the identifier in a spec using `PinGeneratedValue`.

Use `Cratis.Arc.Testing` and import `Cratis.Arc.Testing.Commands`. For the specifications
below, also use the packages from the [standalone testing quick start](index.md#try-a-standalone-command-spec).

## Pin the identifier before executing

These standalone Arc types allocate a cart identifier without persisting a cart or appending
an event. The explicit generated name is `cart`, rather than the C# parameter name `cartId`:

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

Using those types, add this spec to your test project:

```csharp
using System;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Xunit;

public class when_pinning_a_value : Specification
{
    readonly CommandScenario<AllocateCartId> _scenario = new();
    readonly CartId _cartId = CartId.New();
    CommandResult _result = default!;

    void Establish() => _scenario.PinGeneratedValue("cart", _cartId);
    async Task Because() => _result = await _scenario.Execute(new AllocateCartId());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_pass_the_pin_to_the_handler() =>
        ((CommandResult<CartId>)_result).Response.ShouldEqual(_cartId);
    [Fact] void should_capture_the_pin() =>
        _scenario.GeneratedValues.Last<CartId>("cart").ShouldEqual(_cartId);
}
```

Run `dotnet test`; the facts prove that the handler receives your pin and that the scenario
captures it. Pins still pass through normal concept validation. Arc captures only values
that pass that validation, not rejected or unresolved values.

Names are case-sensitive. Without an explicit attribute name, pin the Handle parameter's
C# name. Supply the actual concept, not its underlying `Guid`: an incompatible or null pin
fails execution with `PinnedGeneratedValueHasWrongType`. Its message names the command,
parameter, and expected and actual types, never the pinned value.

## Make pins recoverable into Screenplay

Arc accepts pins for string-backed concepts too. When you [generate a Screenplay](../generating-a-screenplay.md#recovering-generation-strategies),
the generator needs a concrete literal, not the factory call used by `_cartId` above. For that UUID
example, initialize the field with `new CartId(Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"))`
instead. Direct concept construction and one-hop stable fields with literal initializers are recoverable.

For a string-backed concept, construct it from literal text matching its recovered generation strategy:
NanoID's alphabet and size, ULID's uppercase Crockford encoding, TypeID's prefix and UUIDv7 suffix,
or bytes' encoding, case, length and unused trailing bits. Text stays unchanged. The issued command's
generated identifier becomes a `for "<value>"` fixture; another generated value becomes
`generated <name> = "<value>"` under `when`.

A mismatched shape, computed or repeated pin, conditional configuration, nested-command pin or
`AllowGeneration()` leaves the scenario out with `SP0039`. Its command keeps any admitted generation
contract. Non-default strategies require an ESM v15 cap or later; Screenplay v15 also rejects reuse of a
generated identifier whose event source already has facts. These recovery checks do not add strategy-shape
validation to Arc's runtime pins: runtime pins still use normal concept validation.

## Keep scenarios strict or opt into generation

Scenarios are strict by default. An unpinned generated parameter fails the command with a
`GeneratedValueNotPinned` exception outcome in `CommandResult`; `Execute` does not throw
that exception to your spec. The message identifies the command and parameter and tells
you to use `PinGeneratedValue("cart", …)` or `AllowGeneration()`.

When the exact identifier is irrelevant, allow normal generation before executing. This
is an execution fragment using `AllocateCartId` and `CartId` from above:

```csharp
using var scenario = new CommandScenario<AllocateCartId>().AllowGeneration();
var first = await scenario.Execute(new AllocateCartId());
var second = await scenario.Execute(new AllocateCartId());
first.ShouldBeSuccessful();
second.ShouldBeSuccessful();
var latest = scenario.GeneratedValues.Last<CartId>("cart");
var all = scenario.GeneratedValues.All<CartId>("cart");
all.Count.ShouldEqual(2);
latest.ShouldEqual(((CommandResult<CartId>)second).Response);
```

`AllowGeneration()` applies to unpinned values throughout the scenario, including nested
commands. Explicit pins still win. A pin supplies the same value on every execution;
it is not a queue of identifiers.

`Last<T>(name)` selects the latest capture with that name and **declared** type, failing
if none exists. `All<T>(name)` returns a read-only snapshot in resolution order across
executions, or an empty list when there are no matches. Both include validated pins and
nested-command captures. Avoid logging or tracing these values.

## Pin values for nested commands

Pins belong to a command type and name. For another command invoked through
`ICommandPipeline`, use the generic form with both the scenario's command type and the
nested command type. This setup fragment assumes `_scenario` is a
`CommandScenario<AllocateNestedCartId>` for your parent command, plus the `AllocateCartId`
and `CartId` types above:

```csharp
_scenario.PinGeneratedValue<AllocateNestedCartId, AllocateCartId>("cart", _cartId);
```

The generic form pins the child's parameter without changing a same-named parameter on
the parent. Both commands' captures appear in `GeneratedValues`, ordered as they resolve.
If several commands use the same name and declared type, `Last` and `All` combine those
captures; choose distinct names when you need to distinguish them.

## Fix the clock and tenant

Context-dependent concepts receive the command's captured time and current tenant. To test
those parts of the strategy, configure `Clock(DateTimeOffset)` and `Tenant(TenantId)`.
This setup fragment uses `AllocateOrderId` and `OrderId` from
[context-dependent generation](../commands/generated-values.md#generate-with-the-commands-time-and-tenant)
and imports `System`, `Cratis.Arc.Tenancy`, and `Cratis.Arc.Testing.Commands`:

```csharp
var instant = new DateTimeOffset(2024, 6, 15, 12, 34, 56, TimeSpan.Zero);
using var scenario = new CommandScenario<AllocateOrderId>()
    .Clock(instant)
    .Tenant(new TenantId("tenant-one"))
    .AllowGeneration();
var result = await scenario.Execute(new AllocateOrderId());
```

Configure `Clock` before the first `Execute` or `Validate` call builds the service provider.
It replaces the scenario's `TimeProvider`: `ReceivedAt`, injected `TimeProvider.GetUtcNow()`,
and `GetLocalNow()` use the same fixed instant. Timers and elapsed-time measurements remain
real; this does not replace direct calls to `DateTimeOffset.UtcNow` or `TimeProvider.System`.
`Tenant` scopes both execution and validation to the configured tenant and restores the caller's
tenant afterward. Both configurations apply to nested commands and stay local to the scenario.

A fixed clock does **not** fix the random suffix of UUIDv7, ULID, or TypeID. Assert the time
and tenant parts of the contract, or pin the whole concept when you need an exact value.
`Clock(default)` is not a request to use the system clock: unpinned context generation fails
with `MissingGenerationTime`. A pinned value does not invoke its factory.

## Finish configuration before the first execution

Once the first command executes, pins, generation policy, clock, and tenant are immutable,
even if that command fails or has no generated parameters. Later calls to either pin overload,
`AllowGeneration()`, `Clock()`, or `Tenant()` throw `GeneratedValuesMustBePinnedBeforeExecution`.
Create a new scenario when you need different configuration. Validation alone neither generates
values nor freezes pins, generation policy, or tenant selection. It does initialize the service
provider, so a later `Clock()` call throws `ScenarioClockMustBeConfiguredBeforeInitialization`
without changing the clock. Configure the clock before validation too.

Pins and captures are local to each scenario; they do not replace a production generation
strategy or modify another scenario. For existing handlers that call `New()` inside
`Handle`, follow [migrating generated identifiers](../../../upgrading/generated-values.md).
