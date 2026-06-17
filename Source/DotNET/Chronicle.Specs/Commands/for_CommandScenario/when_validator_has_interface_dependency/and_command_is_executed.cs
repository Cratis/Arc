// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_validator_has_interface_dependency;

public class and_command_is_executed : Specification
{
    CommandScenario<StartTimesheetReview> _scenario;
    CommandResult _result;
    TimesheetId _timesheetId;

    void Establish()
    {
        _timesheetId = new TimesheetId(Guid.NewGuid());
        _scenario = new CommandScenario<StartTimesheetReview>();
        _scenario.Services.AddScoped<ITimesheetState>(_ => new TimesheetState(TimesheetPhase.Submitted));
    }

    async Task Because() => _result = await _scenario.Execute(new StartTimesheetReview(_timesheetId));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_have_validation_errors() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] async Task should_have_appended_timesheet_review_started_event() =>
        await _scenario.ShouldHaveAppendedEvent<StartTimesheetReview, TimesheetReviewStarted>((EventSourceId)_timesheetId);
}
