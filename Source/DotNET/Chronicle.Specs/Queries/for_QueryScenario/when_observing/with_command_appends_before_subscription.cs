// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_command_appends_before_subscription : given.a_shared_command_scenario
{
    async Task Establish()
    {
        _commands.Given.ForEventSource(_id).Events(new ObservedAccountChanged(10m));
        (await _commands.Execute(new ChangeObservedAccount(_id, 42m))).IsSuccess.ShouldBeTrue();
        ShareServices();
    }

    async Task Because() => _observation = await _scenario.Observe(nameof(ObservedAccount.All));

    [Fact] void should_replay_the_seed_and_command_history_in_order() => ((IEnumerable<ObservedAccount>)_observation.Initial.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 42m));
}
