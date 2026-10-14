// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_no_seeded_events_in_default_mode : given.a_default_mode_scenario
{
    void Establish() => _scenario.Given.ForEventSource(_id).Events();
    async Task Because() => _error = await Catch.Exception(async () => _result = await _scenario.Execute(_command));

    [Fact] void should_not_refuse_the_scenario() => _error.ShouldBeNull();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_handle_once() => _command.Calls.ShouldEqual(1);
}
