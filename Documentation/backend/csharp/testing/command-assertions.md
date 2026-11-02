---
uid: Arc.Testing.CommandAssertions
title: Assert command decisions and validators
description: Assert returned events, rejections, and validator messages in plain specs without running the command pipeline.
---

Call `Handle()` directly to specify a command's decision, and run a validator directly to specify its rules. The helpers on this page work in plain `Specification` classes: no scenario, host, service container, or Chronicle kernel is needed.

These assertions do **not** run authorization, discovery, `Provide()`, or the command pipeline. A returned event is not proof that it was appended. Use [CommandScenario](./command-scenario.md) for pipeline behavior and [Chronicle append assertions](./chronicle.md#testing-commands-that-use-eventforeventsourceid) for actual appends.

## Specify a returned decision

For example, an application can define this command, event, concept, and validator (normally in separate files):

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using Cratis.Monads;
using FluentValidation;

namespace Inventory;

public record Quantity(int Value) : ConceptAs<int>(Value);
public record ItemAdded(Quantity Quantity);

[Command]
public record AddItem(Quantity Quantity)
{
    public Result<ItemAdded, ValidationResult> Handle() => Quantity.Value > 0
        ? new ItemAdded(Quantity)
        : ValidationResult.Error("Quantity must be positive");
}

public class AddItemValidator : CommandValidator<AddItem>
{
    public AddItemValidator() => RuleFor(command => command.Quantity.Value)
        .GreaterThan(0).WithMessage("Quantity must be positive");
}
```

A spec asserts the returned event's exact type and values. Import `Cratis.Arc.Testing.Commands` from `Cratis.Arc.Testing`:

```csharp
using Cratis.Arc.Testing.Commands;
using Cratis.Monads;
using Cratis.Arc.Validation;
using Cratis.Specifications;
using Inventory;
using Xunit;

namespace Inventory.Specs;

public class when_adding_items : Specification
{
    Result<ItemAdded, ValidationResult> _decision = null!;

    void Because() => _decision = new AddItem(new Quantity(2)).Handle();

    [Fact] void should_return_the_requested_quantity() =>
        _decision.ShouldBeEvent<ItemAdded>(item => item.Quantity.Value == 2);
}
```

For a rejection, assert its exact message or its machine-readable reason:

```csharp
var decision = new AddItem(new Quantity(0)).Handle();
decision.ShouldBeRejected("Quantity must be positive");
decision.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.Rule);
```

| Helper | Contract |
| ------ | -------- |
| `ShouldBeEvent<TEvent>(predicate?)` | Exactly one returned value of the exact type; unwraps the active `Result` branch, but refuses tuples and collections |
| `ShouldBeEventAssignableTo<TEvent>(predicate?)` | The same single-value check, explicitly accepting derived types or interfaces |
| `ShouldContainEvent<TEvent>(predicate?)` | First exact-type match, recursively searching active `Result` branches, tuples, and collections |
| `ShouldContainEventAssignableTo<TEvent>(predicate?)` | The same containment search, explicitly accepting assignable types |
| `ShouldBeAccepted()` | Non-null accepted `Result` value; returns that value |
| `ShouldBeRejected()` | Non-null `ValidationResult` rejection; returns it, regardless of severity |
| `ShouldBeRejected(reason)` | Rejection message equals `reason` using ordinal, case-sensitive comparison |
| `ShouldHaveValidationErrorBecauseOf(reason)` | Rejection has that `ValidationResultReason`; returns the rejection |

Event helpers return the matching event, so you can make more assertions on it. Null never matches. Strings remain one value, not a collection of characters. Containment returns the first match in order, not a uniqueness assertion. Failed predicates include their call-site expression; failed containment lists the candidates, including null entries. Exceptions from predicates propagate.

Decision failures throw `CommandDecisionAssertionException`. These helpers never apply `ICommandResultAssertionPolicy`. Existing `CommandResult` helpers remain separate: for example, `ShouldHaveValidationErrorFor(message)` uses substring matching, whereas `ShouldBeRejected(reason)` requires equality.

## Specify a validator directly

Import `Cratis.Arc.Testing.Validation`. The receiver is `IValidator<T>`, so the same helpers work for command validators, concept validators, and plain FluentValidation validators:

```csharp
using Cratis.Arc.Testing.Validation;
using Cratis.Specifications;
using Inventory;
using Xunit;

namespace Inventory.Specs;

public class when_validating_an_invalid_quantity : Specification
{
    AddItemValidator _validator = new();
    AddItem _command = new(new Quantity(0));
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() =>
        _validator.ShouldReject(_command, "Quantity must be positive"));

    [Fact] void should_reject_with_the_quantity_message() => _error.ShouldBeNull();
}
```

`ShouldAccept(command)` requires zero failures. `ShouldReject(command, because)` requires at least one failure with an exactly equal, ordinal, case-sensitive message—not a substring. Warning and informational failures count too. Null commands throw `ArgumentNullException`.

These helpers create a command validation context and call `SetCommand()`, so `WhenCommand` rules run. They do not discover validators or substitute `ValidatorFailed` for validator exceptions. A failure throws `ValidatorAssertionException` and lists property names, messages, and severities.

For validators with asynchronous rules, use `await validator.ShouldAcceptAsync(command, cancellationToken)` or `await validator.ShouldRejectAsync(command, because, cancellationToken)`. The token reaches FluentValidation. Synchronous helpers never block on async rules: they throw `ValidatorAssertionException` directing you to the corresponding async helper. Other validator exceptions propagate.

## Assert a returned event's target

With `Cratis.Arc.Chronicle.Testing`, import both helper namespaces to select the routed overloads:

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;
using Inventory;

EventSourceId source = "inventory";
var routed = new EventForEventSourceId(source, new ItemAdded(new Quantity(2)));
routed.ShouldTarget(source).ShouldBeEvent<ItemAdded>(item => item.Quantity.Value == 2);

Result<IEnumerable<EventForEventSourceId>, ValidationResult> decision =
    Result<IEnumerable<EventForEventSourceId>, ValidationResult>.Success([routed]);
decision.ShouldBeAccepted().ShouldContainEvent<ItemAdded>(source);
```

`ShouldTarget(id)` returns the routed wrapper. Its `ShouldBeEvent<TEvent>()` asserts the payload, not the wrapper. For a returned `IEnumerable<EventForEventSourceId>`, `ShouldContainEvent<TEvent>(id, predicate?)` requires the source and exact payload type to match; omit `id` to search across sources. `ShouldBeEventAssignableTo<TEvent>()` and `ShouldContainEventAssignableTo<TEvent>(id, predicate?)` explicitly accept derived payloads. Null collections, entries, and payloads cannot match.

The general object helpers deliberately do not unwrap Chronicle wrappers. Keep the routed static type and import the Chronicle testing namespace. For a `Result`, chain `ShouldBeAccepted()` first. These checks prove the **returned target**, never an append: keep [scenario append assertions](./chronicle.md#testing-commands-that-use-eventforeventsourceid) when persistence is part of the behavior.
