// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_command_log_seeds : given.a_shared_command_scenario
{
    QueryResult _next = default!;

    async Task Establish()
    {
        await _commands.EventScenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(10m));
        ShareServices();
        _observation = await _scenario.Observe(nameof(ObservedAccount.All));
    }

    async Task Because()
    {
        (await _commands.Execute(new ChangeObservedAccount(_id, 42m))).IsSuccess.ShouldBeTrue();
        _next = await _observation.Next();
    }

    [Fact] void should_mirror_command_log_seeds() => ((IEnumerable<ObservedAccount>)_observation.Initial.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 10m));
    [Fact] void should_mirror_later_command_appends() => ((IEnumerable<ObservedAccount>)_next.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 42m));
}
