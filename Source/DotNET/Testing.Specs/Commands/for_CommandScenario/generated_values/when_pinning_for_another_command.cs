// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_pinning_for_another_command : Specification
{
    readonly given.CartId _parentId = given.CartId.New();
    readonly given.CartId _childId = given.CartId.New();
    CommandScenario<given.AllocateNestedCartId> _scenario;
    CommandResult _result;

    void Establish()
    {
        _scenario = new();
        _scenario.PinGeneratedValue("cart", _parentId);
        _scenario.PinGeneratedValue<given.AllocateNestedCartId, given.AllocateCartId>("cart", _childId);
    }

    async Task Because() => _result = await _scenario.Execute(new given.AllocateNestedCartId());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_use_command_specific_pins() => ((CommandResult<given.CartId[]>)_result).Response.ShouldContainOnly(_parentId, _childId);
    [Fact] void should_capture_nested_values_in_resolution_order() => _scenario.GeneratedValues.All<given.CartId>("cart").ShouldContainOnly(_parentId, _childId);
    [Fact] void should_read_the_nested_value_as_the_latest() => _scenario.GeneratedValues.Last<given.CartId>("cart").ShouldEqual(_childId);
}
