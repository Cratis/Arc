// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_pinning_the_wrong_type : given.a_generated_value_scenario
{
    readonly Guid _pin = Guid.NewGuid();
    CommandResult _result;

    void Establish() => _scenario.PinGeneratedValue("cart", _pin);
    async Task Because() => _result = await _scenario.Execute(new given.AllocateCartId());

    [Fact] void should_fail() => _result.ShouldNotBeSuccessful();
    [Fact] void should_name_the_expected_and_actual_types() => _result.ExceptionMessages.ShouldContain(new PinnedGeneratedValueHasWrongType(typeof(given.AllocateCartId), "cart", typeof(given.CartId), typeof(Guid)).Message);
    [Fact] void should_not_disclose_the_pin() => _result.ExceptionMessages.Any(message => message.Contains(_pin.ToString(), StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_capture_the_wrong_value() => _scenario.GeneratedValues.All<given.CartId>("cart").ShouldBeEmpty();
}
