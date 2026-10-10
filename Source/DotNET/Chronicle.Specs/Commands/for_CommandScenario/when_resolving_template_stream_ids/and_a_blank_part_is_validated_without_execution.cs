// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_resolving_template_stream_ids;

public class and_a_blank_part_is_validated_without_execution : given.a_template_report_scenario
{
    CommandResult _result;

    void Establish() => _command = _command with { ReportingScopeId = " " };
    async Task Because() => _result = await _scenario.Validate(_command);

    [Fact] void should_have_validation_errors() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_identify_the_missing_part() => _result.ValidationResults.Single().Members.ShouldContain("reportingScopeId");
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_append_events() => _scenario.AppendedEvents.ShouldBeEmpty();
}
