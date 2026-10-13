---
title: Pin generated values in command specs
description: Choose server-generated identifiers by name in command scenarios, fix the clock and tenant, and inspect validated captures.
---

A spec for `PlaceOrder` cannot assert on an order identifier that the server invents at random. It also
should not make the caller supply one just so the spec can. With a
[generated Handle parameter](../commands/generated-values.md), the request contract stays unchanged and the
spec chooses the value with `PinGeneratedValue`.

The examples use the shop from the [Generated Values Shop sample](https://github.com/Cratis/Samples/tree/main/GeneratedValues/Shop),
specs from its `Shop.Specs` project. Use `Cratis.Arc.Testing` and import `Cratis.Arc.Testing.Commands`; the
shop's specs also import `Cratis.Arc.Chronicle.Testing.Commands` for `ShouldHaveAppendedEvent`. For a
standalone setup, follow the [testing quick start](index.md#try-a-standalone-command-spec).

## Pin the identifier before executing

`PlaceOrder` has a generated `orderId` parameter. The spec pins it by that name, executes the command and
checks that the handler received the pin:

```csharp title="Shop.Specs/Orders/Placing/for_PlaceOrder/when_placing_an_order/with_a_pinned_order_id.cs"
using Shop.Orders.Placing;

namespace Shop.Specs.Orders.Placing.for_PlaceOrder.when_placing_an_order;

public class with_a_pinned_order_id : Specification
{
    readonly CommandScenario<PlaceOrder> _scenario = new();
    readonly CustomerId _customerId = new("cus_01m4t7txm0hrn5rwemy2dkwtgq");
    readonly OrderId _orderId = new("order_01m4t7txm0fgx9p7tehmp6mprg");
    CommandResult _result = null!;

    void Establish() => _scenario.PinGeneratedValue("orderId", _orderId);
    async Task Because() => _result = await _scenario.Execute(new PlaceOrder(_customerId));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_return_the_pinned_order_id() => ((CommandResult<OrderId>)_result).Response.ShouldEqual(_orderId);
    [Fact] Task should_place_the_order_on_the_pinned_id() => _scenario.ShouldHaveAppendedEvent<PlaceOrder, OrderPlaced>(_orderId, @event => @event.CustomerId == _customerId);
}
```

Run `dotnet test`. The facts prove that the handler receives your pin and that the event lands on it.
The pin does not call `OrderId.New()` at all, and it still passes through normal concept validation. Arc
captures only values that pass that validation, not rejected or unresolved values.

Pin names are case-sensitive and match the attribute name, `orderId` here, or the C# parameter name when
the attribute has none. Supply the actual concept, not its underlying string: an incompatible or null pin
fails execution with `PinnedGeneratedValueHasWrongType`. Its message names the command, parameter, and
expected and actual types, never the pinned value.

## Keep scenarios strict or opt into generation

Scenarios are strict by default. A spec that forgets the pin fails, and the failure says how to fix it:

```csharp title="Shop.Specs/Orders/Placing/for_PlaceOrder/when_placing_an_order/without_a_pin.cs"
public class without_a_pin : Specification
{
    readonly CommandScenario<PlaceOrder> _scenario = new();
    CommandResult _result = null!;

    async Task Because() => _result = await _scenario.Execute(new PlaceOrder(new CustomerId("cus_01m4t7txm0hrn5rwemy2dkwtgq")));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_fail_the_command() => _result.ShouldNotBeSuccessful();
    [Fact] void should_report_an_exception_not_a_validation_failure() => _result.ShouldHaveExceptions();
    [Fact] void should_explain_how_to_pin_it() => _result.ExceptionMessages.Any(message => message.Contains("PinGeneratedValue(\"orderId\",", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_capture_a_value() => _scenario.GeneratedValues.All<OrderId>("orderId").ShouldBeEmpty();
}
```

An unpinned generated parameter fails the command with a `GeneratedValueNotPinned` exception outcome in
`CommandResult`; `Execute` does not throw it to your spec. This is deliberate: a spec that silently accepts
random values would pass for the wrong reasons.

When the exact identifier is irrelevant, call `AllowGeneration()` before executing. It applies to unpinned
values throughout the scenario, including nested commands, and explicit pins still win. This fragment uses
`PlaceOrder` and `OrderId`:

```csharp
using var scenario = new CommandScenario<PlaceOrder>().AllowGeneration();
var customerId = new CustomerId("cus_01m4t7txm0hrn5rwemy2dkwtgq");
var first = await scenario.Execute(new PlaceOrder(customerId));
var second = await scenario.Execute(new PlaceOrder(customerId));
var all = scenario.GeneratedValues.All<OrderId>("orderId");
all.Count.ShouldEqual(2);
scenario.GeneratedValues.Last<OrderId>("orderId").ShouldEqual(((CommandResult<OrderId>)second).Response);
```

A pin supplies the same value on every execution; it is not a queue of identifiers.
`Last<T>(name)` selects the latest capture with that name and **declared** type, failing if none exists.
`All<T>(name)` returns a read-only snapshot in resolution order across executions, or an empty list.
Both include validated pins and nested-command captures. Avoid logging or tracing these values.

## Fix the clock and tenant

`OrderId` takes its time from the command, so its time part is something a spec can assert. Configure the
scenario's clock and tenant, allow generation, and read the time back out of the generated TypeID:

```csharp title="Shop.Specs/Orders/Placing/for_PlaceOrder/when_placing_an_order/with_a_fixed_clock_and_tenant.cs"
using Cratis.Arc.Tenancy;
using Shop.Orders.Placing;

namespace Shop.Specs.Orders.Placing.for_PlaceOrder.when_placing_an_order;

public class with_a_fixed_clock_and_tenant : Specification
{
    readonly DateTimeOffset _receivedAt = new(2026, 10, 13, 9, 0, 0, TimeSpan.Zero);
    readonly CustomerId _customerId = new("cus_01m4t7txm0hrn5rwemy2dkwtgq");
    CommandScenario<PlaceOrder> _scenario = null!;
    CommandResult _result = null!;
    OrderId _orderId = null!;

    void Establish() => _scenario = new CommandScenario<PlaceOrder>()
        .Clock(_receivedAt)
        .Tenant(new TenantId("north"))
        .AllowGeneration();

    async Task Because()
    {
        _result = await _scenario.Execute(new PlaceOrder(_customerId));
        _orderId = ((CommandResult<OrderId>)_result).Response!;
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_generate_an_order_type_id() => _orderId.Value.StartsWith("order_", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_received_time_as_the_time_part() => TypeIdTime.Of(_orderId.Value).ShouldEqual(_receivedAt);
    [Fact] void should_capture_the_generated_order_id() => _scenario.GeneratedValues.Last<OrderId>("orderId").ShouldEqual(_orderId);
}
```

`TypeIdTime.Of` is a small helper in the sample's `Shop.Specs` project, not an Arc API. It decodes the
48-bit Unix millisecond timestamp at the start of the TypeID's UUIDv7 suffix, so the spec asserts the
time part without comparing the random bits.

`Clock(DateTimeOffset)` replaces the scenario's `TimeProvider`: `ReceivedAt`, injected
`TimeProvider.GetUtcNow()` and `GetLocalNow()` use the same fixed instant. Timers and elapsed-time
measurements remain real, and direct calls to `DateTimeOffset.UtcNow` or `TimeProvider.System` are not
replaced. `Tenant(TenantId)` scopes both execution and validation to that tenant and restores the caller's
tenant afterwards. Both apply to nested commands and stay local to the scenario.

The fixed tenant is part of the context but not of this `OrderId`: a TypeID has no tenant slot, so the
tenant only scopes the execution. A concept that builds the tenant into its value is the case where a
tenant assertion is worth writing.

A fixed clock does **not** fix the random suffix of UUIDv7, ULID or TypeID. Assert the time and tenant
parts, or pin the whole concept when you need an exact value. `Clock(default)` is not a request to use the
system clock: unpinned context generation fails with `MissingGenerationTime`.

## Assert that a secret stays off the causation chain

`ApiKey` is marked `[NotAudited]`. The shop proves it by executing `RevokeApiKey`, which takes the key as a
command property, and reading the causation of the appended event:

```csharp title="Shop.Specs/Customers/ApiKeys/for_RevokeApiKey/when_revoking_an_api_key.cs"
using Cratis.Chronicle.EventSequences;
using Shop.Customers.ApiKeys;

namespace Shop.Specs.Customers.ApiKeys.for_RevokeApiKey;

public class when_revoking_an_api_key : Specification
{
    readonly CommandScenario<RevokeApiKey> _scenario = new();
    readonly CustomerId _customerId = new("cus_01m4t7txm0hrn5rwemy2dkwtgq");
    readonly ApiKey _apiKey = new("AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA");
    CommandResult _result = null!;
    IDictionary<string, string> _causation = null!;

    async Task Because()
    {
        _result = await _scenario.Execute(new RevokeApiKey(_customerId, _apiKey));
        var appended = (List<AppendedEventWithResult>)_scenario.Context[ChronicleCommandScenarioExtender.AppendedEventsKey];
        _causation = appended.Single().Event.Context.Causation.Last().Properties;
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_record_the_customer_on_the_causation() => _causation["customerId"].ShouldEqual(_customerId.Value);
    [Fact] void should_leave_the_api_key_off_the_causation() => _causation.ContainsKey("apiKey").ShouldBeFalse();
    [Fact] void should_not_write_the_api_key_anywhere_on_the_causation() => _causation.Values.Any(value => value.Contains(_apiKey.Value, StringComparison.Ordinal)).ShouldBeFalse();
}
```

This checks the causation chain only, which is all `[NotAudited]` covers. It says nothing about event
payloads, responses or logs.

## Pin values for nested commands

Pins belong to a command type and a name. For another command invoked through `ICommandPipeline`, use the
generic form with both the scenario's command type and the nested command type. This fragment assumes `_scenario` is a
`CommandScenario<ParentCommand>` and the nested command is `PlaceOrder`:

```csharp
_scenario.PinGeneratedValue<ParentCommand, PlaceOrder>("orderId", _orderId);
```

The generic form pins the child's parameter without changing a same-named parameter on the parent. Both
commands' captures appear in `GeneratedValues`, ordered as they resolve. If several commands use the same
name and declared type, `Last` and `All` combine those captures; choose distinct names when you need to
tell them apart.

## Finish configuration before the first execution

Once the first command executes, pins, generation policy, clock and tenant are immutable, even if that
command fails or has no generated parameters. Later calls to either pin overload, `AllowGeneration()`,
`Clock()` or `Tenant()` throw `GeneratedValuesMustBePinnedBeforeExecution`. Create a new scenario when you
need different configuration.

Validation alone neither generates values nor freezes pins, generation policy or tenant selection. It does
initialize the service provider, so a later `Clock()` call throws
`ScenarioClockMustBeConfiguredBeforeInitialization` without changing the clock. Configure the clock before
validation too. Pins and captures are local to each scenario.

## Make pins recoverable into Screenplay

When you [generate a Screenplay](../generating-a-screenplay.md#recovering-generation-strategies), the
generator turns pins into `for "<value>"` fixtures or `generated <name> = "<value>"` lines, and it needs a
concrete literal for that. The shop's specs use literals for this reason: `new OrderId("order_01m4t7txm0fgx9p7tehmp6mprg")`
and `new CustomerId("cus_01m4t7txm0hrn5rwemy2dkwtgq")`. A pin built from `X.New()` cannot be recovered.

Direct concept construction and one-hop stable fields with literal initializers are recoverable. Text must
match the recovered strategy: NanoID's alphabet and size, ULID's uppercase Crockford encoding, TypeID's
prefix and UUIDv7 suffix, or bytes' encoding, case, length and unused trailing bits. For a UUID-backed
concept such as `CartId`, construct it from `Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee")`, as the
shop's `when_starting_a_cart` spec does.

A mismatched shape, computed or repeated pin, conditional configuration, nested-command pin or
`AllowGeneration()` leaves the scenario out with `SP0039`, and its command keeps any admitted generation
contract. Non-default strategies need an ESM v15 cap or later, and Screenplay v15 rejects reuse of a
generated identifier whose event source already has facts. These checks do not add strategy-shape
validation to Arc's runtime pins, which use normal concept validation.

## Next steps

- [Migrate generated identifiers](../../../upgrading/generated-values.mdx) when a handler still calls `New()` inside `Handle()`.
- [Testing generated values in Fundamentals](/fundamentals/csharp/generating-values/testing/) for specs outside a command.
