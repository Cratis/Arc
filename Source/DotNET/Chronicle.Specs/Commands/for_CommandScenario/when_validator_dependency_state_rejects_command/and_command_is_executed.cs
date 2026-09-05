// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_validator_dependency_state_rejects_command;

public class and_command_is_executed : Specification
{
    CommandScenario<ApproveTimesheet> _scenario;
    CommandResult _result;
    TimesheetId _timesheetId;

    void Establish()
    {
        _timesheetId = new TimesheetId(Guid.NewGuid());
        _scenario = new CommandScenario<ApproveTimesheet>();
        _scenario.Services.AddSingleton(new TimesheetState(TimesheetPhase.Draft));
    }

    async Task Because() => _result = await _scenario.Execute(new ApproveTimesheet(_timesheetId));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_validation_errors() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_validation_error_from_validator() => _result.ValidationResults.First().Message.ShouldEqual("Timesheet must be submitted before it can be approved.");
    [Fact] void should_not_have_appended_events() => _scenario.AppendedEvents.ShouldBeEmpty();
}
