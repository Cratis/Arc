// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_a_seeded_guarded_pin_in_default_mode : given.a_default_mode_scenario
{
    void Establish() => _scenario.Given.ForEventSource(_id).Events(new Created("existing"));
    async Task Because() => _result = await _scenario.Execute(_command);

    [Fact] void should_reject_the_collision() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_identify_the_generated_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_handle_the_command_once() => _command.Calls.ShouldEqual(1);
    [Fact] void should_append_nothing() => _scenario.AppendedEvents.ShouldBeEmpty();
}
