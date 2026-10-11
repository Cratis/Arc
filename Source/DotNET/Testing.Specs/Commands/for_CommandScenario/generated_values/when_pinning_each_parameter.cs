// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_pinning_each_parameter : Specification
{
    readonly given.CartId _first = given.CartId.New();
    readonly given.CartId _second = given.CartId.New();
    CommandScenario<given.AllocateTwoCartIds> _scenario;
    CommandResult _result;

    void Establish()
    {
        _scenario = new();
        _scenario.PinGeneratedValue("first", _first).PinGeneratedValue("second", _second);
    }

    async Task Because() => _result = await _scenario.Execute(new given.AllocateTwoCartIds());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_resolve_each_pin_by_its_parameter_name() => ((CommandResult<given.CartId[]>)_result).Response.ShouldContainOnly(_first, _second);
    [Fact] void should_capture_the_first_name() => _scenario.GeneratedValues.Last<given.CartId>("first").ShouldEqual(_first);
    [Fact] void should_capture_the_second_name() => _scenario.GeneratedValues.Last<given.CartId>("second").ShouldEqual(_second);
}
