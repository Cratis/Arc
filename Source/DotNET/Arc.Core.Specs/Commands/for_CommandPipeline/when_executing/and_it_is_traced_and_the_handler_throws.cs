// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_it_is_traced_and_the_handler_throws : given.a_traced_command_pipeline
{
    void Establish() => _commandHandler.Handle(Arg.Any<CommandContext>()).Returns<ValueTask<object?>>(_ => throw new InvalidOperationException($"Could not register {SecretName}"));

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    Activity HandleSpan => _telemetry.Span(WellKnownTelemetryNames.CommandHandleSpan);

    ActivityEvent ExceptionEvent => CommandSpan.Events.Single(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent);

    [Fact] void should_fail() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_set_the_command_status_to_error() => CommandSpan.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_describe_the_status_as_an_error() => CommandSpan.StatusDescription.ShouldEqual(WellKnownOperationOutcomes.Error);
    [Fact] void should_add_the_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Error);
    [Fact] void should_record_the_exception_type() => ExceptionEvent.Tags.Single(_ => _.Key == WellKnownTelemetryNames.ExceptionType).Value.ShouldEqual(typeof(InvalidOperationException).FullName);
    [Fact] void should_record_only_the_exception_type() => ExceptionEvent.Tags.Count().ShouldEqual(1);
    [Fact] void should_set_the_handle_status_to_error() => HandleSpan.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_count_an_error_outcome() => Outcomes.Single().Tags[WellKnownTelemetryNames.CommandOutcome].ShouldEqual(WellKnownOperationOutcomes.Error);
    [Fact] void should_not_record_the_exception_message() => _telemetry.AnyTagContains(SecretName).ShouldBeFalse();
}
