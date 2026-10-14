// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Events;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_a_differently_cased_seeded_id_in_default_mode : given.a_default_mode_scenario
{
    void Establish() => _scenario.Given.ForEventSource(new EventSourceId(_id.Value.ToString("D").ToUpperInvariant())).Events(new Created("existing"));
    async Task Because() => _error = await Catch.Exception(async () => _result = await _scenario.Execute(_command));

    [Fact] void should_compare_source_strings_ordinally() => _error.ShouldBeNull();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
