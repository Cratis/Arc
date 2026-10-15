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
For a guarded generated append target, reusing an occupied pin rejects the command with
`ConcurrencyViolation` and `ReasonDetail = "GeneratedIdentifierCollision"`.

`Last<T>(name)` selects the latest capture with that name and **declared** type, failing if none exists.
`All<T>(name)` returns a read-only snapshot in resolution order across executions, or an empty list.
Both include validated pins and nested-command captures. Avoid logging or tracing these values.

## Prove a generated-identifier collision

Call `UseDecisionReads()` when creating the scenario, **before seeding events**. Its `Given.ForEventSource(id).Events(...)`
seeds the command's event log; default mode seeds only read-model state, so the freshness guard cannot see those events.
Pin the occupied identifier, execute once, and assert the collision detail and unchanged log tail.

This excerpt uses the `CreateSource`, `Identifier`, `Created` and `ResultShape` types from the
[fresh-identifier spec context](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Specs/Commands/for_CommandScenario_with_fresh_identifiers/given/a_fresh_identifier_scenario.cs).
Import `Cratis.Arc.Testing.Commands`, `Cratis.Arc.Chronicle.Testing.Commands`, `Cratis.Arc.Validation`
and `Cratis.Specifications`:

```csharp
await using var scenario = new CommandScenario<CreateSource>().UseDecisionReads();
var id = new Identifier(Guid.Parse("fb6dd43a-b161-44b1-9baa-7b2e4a071680"));
scenario.PinGeneratedValue("id", id);
scenario.Given.ForEventSource(id).Events(new Created("existing"));
var tailBefore = await scenario.EventLog.GetTailSequenceNumber();
var command = new CreateSource(ResultShape.Wrapper);

var result = await scenario.Execute(command);

result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
(await scenario.EventLog.GetTailSequenceNumber()).ShouldEqual(tailBefore);
command.Calls.ShouldEqual(1);
```

Default mode now fails fast with `GeneratedIdentifierCollisionRequiresDecisionReads`: "The scenario cannot
prove a generated-identifier collision without UseDecisionReads(). Create the scenario with
`new CommandScenario<TCommand>().UseDecisionReads()` before seeding events and pinning the generated value."
This setup error is thrown by `Execute` before the command enters the pipeline, not returned in `CommandResult`.
`Unguarded` pins, ids with no seeded events and decision-read mode do not trigger it.

See [Fresh identifiers](../chronicle/commands/fresh-identifiers.md) for whole-source semantics and the direct-write exclusion.

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

A literal pin does not supply a missing generation strategy. When the factory is unrecoverable
(`SP0073`), a scenario pinning that value is withheld whole with `SP0039`; no fixture is emitted against
an invented caller parameter or UUID strategy. Successful scenarios are also withheld when their command
loses an event production, including dependent event, response and route assertions. A provably independent
pre-generation rejection scenario can remain. These omissions apply in authoring-only output and under
version caps too; they do not prevent your original Arc specs from pinning a custom factory.

### Pins for validated generated concepts

Recoverable String-backed concept rules accompany their strategy in ESM v20. A pin must satisfy the
concept's rules as well as its encoding and size. When a successful Arc scenario pins a value that
violates either, recovery leaves the scenario out with located `SP0039`; it preserves the validator,
strategy and pin text rather than weakening the contract. The Screenplay reference evaluator classifies
an invalid generated fixture as `Rejected(Contract)`, never a client validation failure or a reason to
redraw the value.

Non-opaque rules that Screenplay cannot prove still accompany generation and report Information
`SP0078` (`PLAY0689`); each concrete pin is checked. Opaque validation cannot be reference-executed, so
recovery keeps the validator and withholds generation and unsupported scenarios. A V19-or-earlier cap
also withholds validated generation and its pins, without deleting rules to fit the older consumer.
Existing rule-free pins keep their previous model version and behavior. See
[Validated generation](../generating-a-screenplay.md#validated-generation-and-esm-v20).

### Pattern, random and inline-rule fixtures

ESM v21 recovery also accepts literal text pins for pattern and inline-rule generation, and integral pins
for signed random generation. A random `ConceptAs<long>` can be pinned with a direct construction such as
`new ReceiptId(4L)`; the value must lie inside its recovered inclusive range. Pattern pins must belong to
the exact pattern support, including appended checksum digits. Arc asks Screenplay's reference runner to
check these fixtures rather than sampling the factory or duplicating its pattern evaluator.

An inline rule's pin supplies the primitive result without executing the C# body; recovered concept
validation still applies. Cratis C# rule bodies use Fundamentals' native generation context, including
Tenant formatting and compound `GenerateValue.*(context)` calls. Neither a fixed clock nor a pin proves
an opaque body's entropy, so recovery never admits a rule-generated identifier.

Invalid or missing fixtures leave the successful scenario out with `SP0039`. A V20-or-earlier cap
withholds v21 generation and dependent scenarios in both output modes, keeping the validator rather than
deleting checksum rules. Your original Arc specs remain runnable; the cap limits the recovered model.

## Next steps

- [Migrate generated identifiers](../../../upgrading/generated-values.mdx) when a handler still calls `New()` inside `Handle()`.
- [Testing generated values in Fundamentals](/fundamentals/csharp/generating-values/testing/) for specs outside a command.
