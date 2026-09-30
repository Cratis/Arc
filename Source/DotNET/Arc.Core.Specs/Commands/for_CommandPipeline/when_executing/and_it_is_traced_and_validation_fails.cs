// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_it_is_traced_and_validation_fails : given.a_traced_command_pipeline
{
    ActivityEvent _event;

    void Establish()
    {
        var failed = CommandResult.Success(_correlationId);
        failed.ValidationResults = [ValidationResult.Error($"'{SecretName}' is already taken", ["name"])];
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(failed);
    }

    async Task Because()
    {
        _result = await _commandPipeline.Execute(_command, _serviceProvider);
        _event = CommandSpan.Events.Single(_ => _.Name == WellKnownTelemetryNames.ValidationFailedEvent);
    }

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_leave_the_status_unset() => CommandSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_add_the_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Validation);
    [Fact] void should_name_the_member_that_failed() => ((string[])_event.Tags.Single(_ => _.Key == WellKnownTelemetryNames.ValidationMembers).Value!).ShouldContainOnly("name");
    [Fact] void should_name_the_reason() => _event.Tags.Single(_ => _.Key == WellKnownTelemetryNames.ValidationReason).Value.ShouldEqual("rule");
    [Fact] void should_add_the_severity() => _event.Tags.Single(_ => _.Key == WellKnownTelemetryNames.ValidationSeverity).Value.ShouldEqual(nameof(ValidationResultSeverity.Error));
    [Fact] void should_not_run_the_handler() => _telemetry.Activities.Any(_ => _.OperationName == WellKnownTelemetryNames.CommandHandleSpan).ShouldBeFalse();
    [Fact] void should_count_a_validation_outcome() => Outcomes.Single().Tags[WellKnownTelemetryNames.CommandOutcome].ShouldEqual(WellKnownOperationOutcomes.Validation);
    [Fact] void should_not_record_the_message_or_the_value() => _telemetry.AnyTagContains(SecretName).ShouldBeFalse();
}
