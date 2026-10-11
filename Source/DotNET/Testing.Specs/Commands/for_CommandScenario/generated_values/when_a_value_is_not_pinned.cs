// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_a_value_is_not_pinned : given.a_generated_value_scenario
{
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(new given.AllocateCartId());

    [Fact] void should_fail_the_command() => _result.ShouldNotBeSuccessful();
    [Fact] void should_report_an_exception_not_a_validation_failure() => _result.ShouldHaveExceptions();
    [Fact] void should_name_the_command() => _result.ExceptionMessages.Any(message => message.Contains(typeof(given.AllocateCartId).FullName!, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_name_the_parameter() => _result.ExceptionMessages.Any(message => message.Contains("'cart'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_explain_how_to_pin_it() => _result.ExceptionMessages.Any(message => message.Contains("PinGeneratedValue(\"cart\",", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_explain_how_to_allow_generation() => _result.ExceptionMessages.Any(message => message.Contains("AllowGeneration()", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_capture_an_unresolved_value() => _scenario.GeneratedValues.All<given.CartId>("cart").ShouldBeEmpty();
}
