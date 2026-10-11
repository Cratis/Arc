// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_pinning_after_validation : given.a_generated_value_scenario
{
    readonly given.CartId _cartId = given.CartId.New();
    CommandResult _result;

    async Task Establish() => await _scenario.Validate(new given.AllocateCartId());

    async Task Because()
    {
        _scenario.PinGeneratedValue("cart", _cartId);
        _result = await _scenario.Execute(new given.AllocateCartId());
    }

    [Fact] void should_not_freeze_pins_during_validation() => _result.ShouldBeSuccessful();
    [Fact] void should_use_the_pin() => ((CommandResult<given.CartId>)_result).Response.ShouldEqual(_cartId);
    [Fact] void should_capture_only_the_execution() => _scenario.GeneratedValues.All<given.CartId>("cart").ShouldContainOnly(_cartId);
}
