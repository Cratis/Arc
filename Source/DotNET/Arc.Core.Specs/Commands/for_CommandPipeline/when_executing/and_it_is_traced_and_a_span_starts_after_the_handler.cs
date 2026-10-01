// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

/// <summary>
/// The handle span stops when the handler returns. Whatever the pipeline does next - completing the execution
/// scopes here - must be parented to the command span, and the caller's current span must be left as it was.
/// </summary>
public class and_it_is_traced_and_a_span_starts_after_the_handler : given.a_traced_command_pipeline
{
    const string AfterTheHandler = "after the handler";

    Activity? _ambient;
    Activity? _currentWhileCompleting;
    Activity? _currentAfterThePipeline;

    void Establish() => _executionScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(_ =>
    {
        _currentWhileCompleting = Activity.Current;
        _activitySource.StartActivity(AfterTheHandler)?.Dispose();
        return Task.CompletedTask;
    });

    async Task Because()
    {
        _ambient = _activitySource.StartActivity("ambient");
        _result = await _commandPipeline.Execute(_command, _serviceProvider);
        _currentAfterThePipeline = Activity.Current;
    }

    void Destroy() => _ambient?.Dispose();

    Activity AfterSpan => _telemetry.Span(AfterTheHandler);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_run_the_handler_in_a_handle_span() => _telemetry.Span(WellKnownTelemetryNames.CommandHandleSpan).ParentSpanId.ShouldEqual(CommandSpan.SpanId);
    [Fact] void should_parent_a_span_started_after_the_handler_to_the_command_span() => AfterSpan.ParentSpanId.ShouldEqual(CommandSpan.SpanId);
    [Fact] void should_not_leave_the_stopped_handle_span_current() => _currentWhileCompleting.ShouldEqual(CommandSpan);
    [Fact] void should_leave_the_callers_span_current() => _currentAfterThePipeline.ShouldEqual(_ambient);
}
