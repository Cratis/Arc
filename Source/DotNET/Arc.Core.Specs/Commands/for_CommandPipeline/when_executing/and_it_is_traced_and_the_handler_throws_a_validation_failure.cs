// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_it_is_traced_and_the_handler_throws_a_validation_failure : given.a_traced_command_pipeline
{
    void Establish() => _commandHandler.Handle(Arg.Any<CommandContext>()).Returns<ValueTask<object?>>(_ => throw new UnableToResolveReadModelFromCommandContext(typeof(object)));

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    Activity HandleSpan => _telemetry.Span(WellKnownTelemetryNames.CommandHandleSpan);

    [Fact] void should_fail_validation() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_add_the_validation_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Validation);
    [Fact] void should_leave_the_command_status_unset() => CommandSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_leave_the_handle_status_unset() => HandleSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_exception_on_the_handle_span() => HandleSpan.Events.Count(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).ShouldEqual(1);
    [Fact] void should_count_a_validation_outcome() => Outcomes.Single().Tags[WellKnownTelemetryNames.CommandOutcome].ShouldEqual(WellKnownOperationOutcomes.Validation);
}
