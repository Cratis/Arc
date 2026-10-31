// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model;

public class and_the_state_is_pinned : given.a_pinned_state
{
    async Task Because() => _result = await _scenario.Execute(new(_source), global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact] async Task should_use_the_pinned_state() =>
        await _scenario.ShouldHaveAppendedEvent<Decide, Decided>(_source, fact => fact.Status == _state.Status);
}
