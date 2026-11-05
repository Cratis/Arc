---
title: Cratis Arc command testing from decision to pipeline
description: Test a Cratis Arc command's calculation directly, then use CommandScenario to check validation and dependency resolution without Chronicle.
---

Test a Cratis Arc command's decision by calling `Handle()` directly. Use `CommandScenario` when you also need to prove that Arc validates the input and supplies the handler's dependencies. Neither approach requires Cratis Chronicle, a database, or an HTTP server.

This lesson uses a shipping quote to show the difference. First check the calculation with a supplied rate. Then run the same command through Arc to check rate acquisition and rejection of an invalid weight.

## Create the projects

Use the latest .NET SDK and current Cratis packages. In a new working directory:

```bash
dotnet new classlib -n Shipping
dotnet add Shipping package Cratis.Arc.Core
dotnet new xunit -n Shipping.Specs
dotnet add Shipping.Specs reference Shipping/Shipping.csproj
dotnet add Shipping.Specs package Cratis.Specifications.XUnit
dotnet add Shipping.Specs package Cratis.Arc.Testing
dotnet add Shipping.Specs package NSubstitute
```

Remove the template's `Class1.cs` and `UnitTest1.cs` if present. Keep application types in `Shipping` and specs in `Shipping.Specs`. These are dedicated test projects; their tests need no `#if DEBUG` wrapper. The commands below run them explicitly by project.

## Keep acquiring data separate from deciding

Create `Shipping/QuoteShipping.cs`:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Shipping;

public record ParcelWeight(decimal Value) : ConceptAs<decimal>(Value)
{
    public static readonly ParcelWeight NotSet = new(0m);
}

public record RatePerKilogram(decimal Value) : ConceptAs<decimal>(Value);

public record ShippingCost(decimal Value) : ConceptAs<decimal>(Value);

public interface IRateCard
{
    Task<RatePerKilogram> GetCurrentRate();
}

[Command]
public record QuoteShipping(ParcelWeight Weight)
{
    public Task<RatePerKilogram> Provide(IRateCard rates) => rates.GetCurrentRate();

    public ShippingCost Handle(RatePerKilogram rate) => new(Weight.Value * rate.Value);
}

public class ParcelWeightValidator : ConceptValidator<ParcelWeight>
{
    public ParcelWeightValidator() => RuleFor(weight => weight.Value)
        .GreaterThan(0m)
        .WithMessage("Parcel weight must be positive.");
}
```

`ParcelWeight` is measured in kilograms; the rate and cost use the same configured currency. They have separate types so a weight, rate, and cost cannot accidentally trade places in C#. Shared concepts normally get their own files in the feature; they are together here to make the lesson easy to follow.

`Provide()` acquires the rate. `Handle()` only multiplies the supplied values: the same weight and rate produce the same cost, with no I/O, clock, randomness, or mutable external state. That makes this handler a **pure function**. Arc does not require every handler to be pure; this separation is useful when acquiring data would otherwise obscure a decision.

`ParcelWeightValidator` expresses an invariant of the value, not just this command. Arc discovers it and applies it when validating commands carrying that concept. Calling `Handle()` yourself does not run validation or `Provide()`.

## Specify the decision directly

Supply the rate yourself so this spec can fail only on the calculation, not on rate acquisition or framework configuration.

Create `Shipping.Specs/for_QuoteShipping/when_quoting_shipping_directly.cs`:

```csharp
using Cratis.Specifications;
using Shipping;
using Xunit;

namespace Shipping.Specs;

public class when_quoting_shipping_directly : Specification
{
    ShippingCost _cost = null!;

    void Because() => _cost = new QuoteShipping(new ParcelWeight(2.5m))
        .Handle(new RatePerKilogram(4m));

    [Fact] void should_quote_ten_currency_units() =>
        _cost.ShouldEqual(new ShippingCost(10m));
}
```

Run:

```bash
dotnet test Shipping.Specs --filter FullyQualifiedName~when_quoting_shipping_directly
```

The expected result is `ShippingCost(10)`: 2.5 kilograms at four currency units per kilogram. Add cases alongside this one when the calculation gains thresholds, rounding rules, or different tariffs. Keep rate acquisition out of those cases so a failure points to the decision.

Cratis Specifications gives the example a readable shape: `Because()` performs the action, and `should_...` names the expected behavior. Use `Establish()` for setup when a case needs it and `Destroy()` for owned resources.

For event-producing decisions, use [command and validator assertions](./command-assertions.md): `ShouldBeEvent<TEvent>()` checks a returned event, `ShouldBeRejected(reason)` checks an exact rejection message, and `ShouldAccept` / `ShouldReject` specify validators directly. These assertions do not execute Arc's pipeline or prove an append.

## Prove that Arc connects the pieces

Create `Shipping.Specs/for_QuoteShipping/when_quoting_shipping_through_arc.cs`:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shipping;
using Xunit;

namespace Shipping.Specs;

public class when_quoting_shipping_through_arc : Specification
{
    readonly CommandScenario<QuoteShipping> _scenario = new();
    IRateCard _rates = null!;
    CommandResult _result = null!;

    void Establish()
    {
        _rates = Substitute.For<IRateCard>();
        _rates.GetCurrentRate().Returns(Task.FromResult(new RatePerKilogram(4m)));
        _scenario.Services.AddSingleton(_rates);
    }

    async Task Because() =>
        _result = await _scenario.Execute(new QuoteShipping(new ParcelWeight(2.5m)));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact] async Task should_acquire_the_rate_once() =>
        await _rates.Received(1).GetCurrentRate();

    [Fact] void should_return_the_calculated_cost() =>
        ((CommandResult<ShippingCost>)_result).Response.ShouldEqual(new ShippingCost(10m));

    void Destroy() => _scenario.Dispose();
}
```

`Execute()` now checks the connection the direct spec left out. It runs Arc's filters, resolves the rate-card service for `Provide()`, supplies the returned rate to `Handle()`, and processes the response. Register the substitute before the first execution because the scenario builds its service provider on first use. Arc discovers the concept validator; the spec does not register it.

The cast uses the handler's actual response type. `CommandScenario.Execute()` returns the non-generic `CommandResult`; it does not have its own `Execute<TResponse>()` overload. An unsuccessful result need not contain a typed response. See [the scenario reference](./command-scenario.md#package-and-api).

## Prove rejected input does not reach the rate card

Create `Shipping.Specs/for_QuoteShipping/when_quoting_an_unset_weight.cs`:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shipping;
using Xunit;

namespace Shipping.Specs;

public class when_quoting_an_unset_weight : Specification
{
    readonly CommandScenario<QuoteShipping> _scenario = new();
    IRateCard _rates = null!;
    CommandResult _result = null!;

    void Establish()
    {
        _rates = Substitute.For<IRateCard>();
        _scenario.Services.AddSingleton(_rates);
    }

    async Task Because() =>
        _result = await _scenario.Execute(new QuoteShipping(ParcelWeight.NotSet));

    [Fact] void should_reject_the_weight() =>
        _result.ShouldHaveValidationErrorFor("Parcel weight must be positive.");

    [Fact] async Task should_not_acquire_a_rate() =>
        await _rates.DidNotReceive().GetCurrentRate();

    void Destroy() => _scenario.Dispose();
}
```

Run all the lesson's specs:

```bash
dotnet test Shipping.Specs
```

The six facts cover three different questions: the calculation, successful pipeline composition, and rejection before acquisition. Checking the specific message and the untouched collaborator prevents an unrelated failure from masquerading as the expected rejection.

## What you have proved—and what comes next

The direct spec checks the calculation; the scenario specs check validation and dependency resolution. HTTP routing, authentication middleware, and the production rate-card implementation still need tests at their own boundaries. They need not be part of every arithmetic case.

For inline service side effects, prefer [command operations](../commands/operations/index.md). `Handle()` can return the work it intends to perform, and Arc manages execution and optional compensation. [Test command operations](./command-operations.md) extends this lesson with direct declaration specs and real `CommandScenario` recovery tests.

A handler that writes directly through a service remains supported; unit specs can use a substitute to inspect the call. Such a handler is testable, but it is not a pure function. The [service-backed command recipe](../../../scenarios/test-a-command.mdx) shows the pipeline-level counterpart.

If your command uses Chronicle state, keep the same split: call `Handle(readModel)` directly for the decision, then seed state in a scenario to test the pipeline. [Give a protected read model its state](./chronicle.md#give-a-protected-read-model-its-state) shows how to supply a plain read-model parameter without configuring decision tokens or competing writes.

For a handler that returns a domain event, continue to [test an event-sourced command](./event-sourced-commands.md). That lesson checks the separate append contract. Return to [choosing a test boundary](./index.md#choose-the-boundary-that-can-catch-the-bug) if the behavior you need to prove belongs elsewhere.
