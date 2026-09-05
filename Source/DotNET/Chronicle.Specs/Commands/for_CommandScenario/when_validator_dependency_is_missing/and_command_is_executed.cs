// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_validator_dependency_is_missing;

public class and_command_is_executed : Specification
{
    CommandScenario<ApproveTimesheet> _scenario;
    CommandResult _result;
    TimesheetId _timesheetId;

    void Establish()
    {
        _timesheetId = new TimesheetId(Guid.NewGuid());
        _scenario = new CommandScenario<ApproveTimesheet>();
    }

    async Task Because() => _result = await _scenario.Execute(new ApproveTimesheet(_timesheetId));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_exceptions() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_include_activation_failure() => _result.ExceptionMessages.First().ShouldContain("A suitable constructor");
    [Fact] void should_tell_the_user_constructor_services_must_be_registered() => _result.ExceptionMessages.First().ShouldContain("services are registered for all parameters");
    [Fact] void should_not_have_appended_events() => _scenario.AppendedEvents.ShouldBeEmpty();
}
