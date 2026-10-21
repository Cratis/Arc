// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_a_seeded_unguarded_pin_in_default_mode : given.a_default_mode_scenario
{
    CommandScenario<CreateUnguardedSource> _unguardedScenario;

    void Establish()
    {
        _unguardedScenario = new CommandScenario<CreateUnguardedSource>().PinGeneratedValue("id", _id);
        _unguardedScenario.Given.ForEventSource(_id).Events(new Created("existing"));
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _unguardedScenario.Execute(new CreateUnguardedSource()));
    async Task Destroy() => await _unguardedScenario.DisposeAsync();

    [Fact] void should_not_refuse_the_scenario() => _error.ShouldBeNull();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_the_event() => _unguardedScenario.AppendedEvents.ShouldContainSingleItem();
}
