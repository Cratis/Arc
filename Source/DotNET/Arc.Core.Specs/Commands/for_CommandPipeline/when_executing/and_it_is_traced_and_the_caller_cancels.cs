// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_it_is_traced_and_the_caller_cancels : given.a_traced_command_pipeline
{
    CancellationTokenSource _cancellation;

    void Establish()
    {
        _cancellation = new();
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_ => CancelWhileHandling());
    }

    void Destroy() => _cancellation.Dispose();

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider, null, _cancellation.Token);

    async ValueTask<object?> CancelWhileHandling()
    {
        await _cancellation.CancelAsync();
        throw new OperationCanceledException(_cancellation.Token);
    }

    Activity HandleSpan => _telemetry.Span(WellKnownTelemetryNames.CommandHandleSpan);

    [Fact] void should_add_the_cancelled_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Cancelled);
    [Fact] void should_leave_the_command_status_unset() => CommandSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_leave_the_handle_status_unset() => HandleSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_cancellation_on_the_handle_span() => HandleSpan.Events.Single(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).Tags.Single().Value.ShouldEqual(typeof(OperationCanceledException).FullName);
    [Fact] void should_count_a_cancelled_outcome() => Outcomes.Single().Tags[WellKnownTelemetryNames.CommandOutcome].ShouldEqual(WellKnownOperationOutcomes.Cancelled);
}
