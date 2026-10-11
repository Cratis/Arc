// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_two_scenarios_have_different_pins : given.a_generated_value_scenario
{
    readonly given.CartId _firstId = given.CartId.New();
    readonly given.CartId _secondId = given.CartId.New();
    CommandScenario<given.AllocateCartId> _otherScenario;
    CommandResult _first;
    CommandResult _second;

    void Establish()
    {
        _scenario.PinGeneratedValue("cart", _firstId);
        _otherScenario = new CommandScenario<given.AllocateCartId>().PinGeneratedValue("cart", _secondId);
    }

    async Task Because()
    {
        _first = await _scenario.Execute(new given.AllocateCartId());
        _second = await _otherScenario.Execute(new given.AllocateCartId());
    }

    void Destroy() => _otherScenario.Dispose();

    [Fact] void should_use_the_first_scenarios_pin() => ((CommandResult<given.CartId>)_first).Response.ShouldEqual(_firstId);
    [Fact] void should_use_the_second_scenarios_pin() => ((CommandResult<given.CartId>)_second).Response.ShouldEqual(_secondId);
    [Fact] void should_keep_the_first_capture_isolated() => _scenario.GeneratedValues.All<given.CartId>("cart").ShouldContainOnly(_firstId);
    [Fact] void should_keep_the_second_capture_isolated() => _otherScenario.GeneratedValues.All<given.CartId>("cart").ShouldContainOnly(_secondId);
}
