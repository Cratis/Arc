// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_allowing_generation_with_a_pin : given.a_generated_value_scenario
{
    readonly given.CartId _cartId = given.CartId.New();
    CommandResult _result;

    void Establish() => _scenario.PinGeneratedValue("cart", _cartId).AllowGeneration();
    async Task Because() => _result = await _scenario.Execute(new given.AllocateCartId());

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_use_the_pin_instead_of_generating() => ((CommandResult<given.CartId>)_result).Response.ShouldEqual(_cartId);
}
