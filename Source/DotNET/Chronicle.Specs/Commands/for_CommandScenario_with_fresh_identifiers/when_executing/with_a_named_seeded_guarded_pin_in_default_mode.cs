// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_a_named_seeded_guarded_pin_in_default_mode : given.a_default_mode_scenario
{
    CommandScenario<CreateNamedSource> _namedScenario;
    CreateNamedSource _namedCommand;

    void Establish()
    {
        _namedScenario = new CommandScenario<CreateNamedSource>().PinGeneratedValue("sourceId", _id);
        _namedScenario.Given.ForEventSource(_id).Events(new Created("existing"));
        _namedCommand = new();
    }

    async Task Because() => _result = await _namedScenario.Execute(_namedCommand);
    async Task Destroy() => await _namedScenario.DisposeAsync();

    [Fact] void should_use_the_attribute_name() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_identify_the_generated_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_handle_the_command_once() => _namedCommand.Calls.ShouldEqual(1);
}
