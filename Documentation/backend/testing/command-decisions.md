---
title: Test a command's decision and its pipeline
description: Learn when to call Handle directly and when to use CommandScenario, using the same standalone Arc command.
---

A shipping quote has two different things to prove: **the calculation is right**, and **Arc validates the input and supplies the rate before calculating it**. You do not need a server to prove either one—but they are different tests.

In this lesson, write a fast direct spec for the decision, then a `CommandScenario` spec for the pipeline. Neither requires Chronicle or a database.

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

The spec passes when the decision returns `ShippingCost(10)`. There is no service container, rate-card substitute, HTTP server, or framework harness in this test. Add decision cases here when the calculation gains thresholds, rounding rules, or different tariffs.

Cratis Specifications gives the example a readable shape: `Because()` performs the action, and `should_...` names the expected behavior. Use `Establish()` for setup when a case needs it and `Destroy()` for owned resources.

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

Here the boundary matters: `Execute()` runs Arc's filters, resolves `Provide()` and its service, supplies the rate to `Handle()`, and processes the response. Register the application dependency before the first execution; the scenario builds its service provider lazily. It discovers validators rather than requiring the test to register each one.

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

You have fast tests for the decision and focused tests for Arc's composition. You have **not** tested an HTTP route, authentication middleware, or a production rate-card implementation. Test those where they are introduced rather than adding infrastructure to every arithmetic case.

For a handler that writes through a service, direct unit specs can use a substitute to inspect that call. Such a handler is testable, but it is not a pure function. The [service-backed command recipe](../../scenarios/test-a-command.md) shows the pipeline-level counterpart.

With Chronicle, a handler can instead return a domain event as its decision; a scenario must then prove how the integration appends it. Continue to [test an event-sourced command](./event-sourced-commands.md), or return to [choosing a test boundary](./index.md#choose-the-boundary-that-can-catch-the-bug).
