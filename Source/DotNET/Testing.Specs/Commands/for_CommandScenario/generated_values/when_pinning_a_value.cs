// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_pinning_a_value : given.a_generated_value_scenario
{
    readonly given.CartId _cartId = given.CartId.New();
    CommandResult _result;

    void Establish() => _scenario.PinGeneratedValue("cart", _cartId);
    async Task Because() => _result = await _scenario.Execute(new given.AllocateCartId());

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_pass_the_pin_to_the_handler() => ((CommandResult<given.CartId>)_result).Response.ShouldEqual(_cartId);
    [Fact] void should_capture_the_pin() => _scenario.GeneratedValues.Last<given.CartId>("cart").ShouldEqual(_cartId);
    [Fact] void should_capture_it_once() => _scenario.GeneratedValues.All<given.CartId>("cart").ShouldContainOnly(_cartId);
}
