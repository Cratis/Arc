// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_it_is_traced_and_the_append_is_rejected : given.a_traced_command_pipeline
{
    const string ConstraintName = "UniqueAuthorName";

    void Establish()
    {
        var rejected = CommandResult.Success(_correlationId);
        rejected.ValidationResults = [ValidationResult.Error($"'{SecretName}' is already taken", ["name"], reason: ValidationResultReason.ConstraintViolation, reasonDetail: ConstraintName)];
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(rejected);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    ActivityEvent Event => CommandSpan.Events.Single(_ => _.Name == WellKnownTelemetryNames.ValidationFailedEvent);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_leave_the_status_unset() => CommandSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_add_the_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.AppendRejected);
    [Fact] void should_name_the_reason() => Event.Tags.Single(_ => _.Key == WellKnownTelemetryNames.ValidationReason).Value.ShouldEqual("constraintViolation");
    [Fact] void should_name_the_constraint() => Event.Tags.Single(_ => _.Key == WellKnownTelemetryNames.ValidationReasonDetail).Value.ShouldEqual(ConstraintName);
    [Fact] void should_count_an_append_rejected_outcome() => Outcomes.Single().Tags[WellKnownTelemetryNames.CommandOutcome].ShouldEqual(WellKnownOperationOutcomes.AppendRejected);
    [Fact] void should_not_record_the_value() => _telemetry.AnyTagContains(SecretName).ShouldBeFalse();
}
