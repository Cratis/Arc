// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model;

public class and_the_pin_is_for_another_source : given.a_pinned_state
{
    readonly EventSourceId _other = EventSourceId.New();

    void Establish() => _scenario.Given.ForEventSource(_other).Events(new StateChanged("folded"));

    async Task Because() => _result = await _scenario.Execute(new(_other), global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_fold_the_other_sources_events() =>
        await _scenario.ShouldHaveAppendedEvent<Decide, Decided>(_other, fact => fact.Status == "folded");
}
