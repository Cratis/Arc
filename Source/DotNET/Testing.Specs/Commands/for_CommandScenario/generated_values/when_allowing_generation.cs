// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_allowing_generation : given.a_generated_value_scenario
{
    CommandResult _first;
    CommandResult _second;
    IReadOnlyList<given.CartId> _snapshot;

    void Establish() => _scenario.AllowGeneration();

    async Task Because()
    {
        _first = await _scenario.Execute(new given.AllocateCartId());
        _snapshot = _scenario.GeneratedValues.All<given.CartId>("cart");
        _second = await _scenario.Execute(new given.AllocateCartId());
    }

    [Fact] void should_succeed_on_the_first_execution() => _first.ShouldBeSuccessful();
    [Fact] void should_succeed_on_the_second_execution() => _second.ShouldBeSuccessful();
    [Fact] void should_capture_values_in_execution_order() => _scenario.GeneratedValues.All<given.CartId>("cart").ShouldContainOnly(((CommandResult<given.CartId>)_first).Response, ((CommandResult<given.CartId>)_second).Response);
    [Fact] void should_read_the_latest_value() => _scenario.GeneratedValues.Last<given.CartId>("cart").ShouldEqual(((CommandResult<given.CartId>)_second).Response);
    [Fact] void should_keep_previous_snapshots_unchanged() => _snapshot.ShouldContainOnly(((CommandResult<given.CartId>)_first).Response);
    [Fact] void should_match_names_case_sensitively() => _scenario.GeneratedValues.All<given.CartId>("Cart").ShouldBeEmpty();
    [Fact] void should_match_the_declared_type() => _scenario.GeneratedValues.All<Guid>("cart").ShouldBeEmpty();
}
