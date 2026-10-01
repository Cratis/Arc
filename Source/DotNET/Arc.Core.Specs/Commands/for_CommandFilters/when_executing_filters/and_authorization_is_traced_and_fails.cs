// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Observability;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

/// <summary>
/// An authorization filter that fails because the command is invalid or because the caller gave up is not a fault
/// of the system; only a fault fails the span.
/// </summary>
public class and_authorization_is_traced_and_fails : Specification
{
    public record RegisterAuthor(string Name);

    CommandFilters _filters;
    ActivitySource _source;
    TelemetryRecorder _telemetry;
    IAuthorizationCommandFilter _authorization;
    CommandContext _validationContext;
    CommandContext _cancellationContext;
    CommandContext _errorContext;
    CancellationTokenSource _cancellation;
    List<Activity> _spans;

    void Establish()
    {
        _cancellation = new();
        _cancellation.Cancel();
        _validationContext = new CommandContext(CorrelationId.New(), typeof(RegisterAuthor), new RegisterAuthor("a name"), [], new());
        _cancellationContext = new CommandContext(CorrelationId.New(), typeof(RegisterAuthor), new RegisterAuthor("a name"), [], new(), CancellationToken: _cancellation.Token);
        _errorContext = new CommandContext(CorrelationId.New(), typeof(RegisterAuthor), new RegisterAuthor("a name"), [], new());
        _authorization = Substitute.For<IAuthorizationCommandFilter>();
        _authorization.OnExecution(_validationContext).Returns<Task<CommandResult>>(_ => throw new TheValidationFailure());
        _authorization.OnExecution(_cancellationContext).Returns<Task<CommandResult>>(_ => throw new OperationCanceledException(_cancellation.Token));
        _authorization.OnExecution(_errorContext).Returns<Task<CommandResult>>(_ => throw new InvalidOperationException("the identity store is gone"));

        _source = new ActivitySource("Cratis.Arc.Test");
        var activitySource = Substitute.For<IActivitySource<CommandFilters>>();
        activitySource.ActualSource.Returns(_source);
        _filters = new CommandFilters(new KnownInstancesOf<ICommandFilter>([_authorization]), activitySource);
        _telemetry = new TelemetryRecorder(_source);
    }

    void Destroy()
    {
        _cancellation.Dispose();
        _telemetry.Dispose();
        _source.Dispose();
    }

    async Task Because()
    {
        await _filters.OnExecution(_validationContext);
        await _filters.OnExecution(_cancellationContext);
        await _filters.OnExecution(_errorContext);
        _spans = [.. _telemetry.Activities.Where(_ => _.OperationName == WellKnownTelemetryNames.CommandAuthorizeSpan)];
    }

    Activity ValidationSpan => _spans[0];

    Activity CancellationSpan => _spans[1];

    Activity ErrorSpan => _spans[2];

    [Fact] void should_raise_a_span_for_each_run() => _spans.Count.ShouldEqual(3);
    [Fact] void should_leave_the_status_unset_for_a_validation_failure() => ValidationSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_validation_failure() => ValidationSpan.Events.Count(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).ShouldEqual(1);
    [Fact] void should_leave_the_status_unset_for_a_cancellation() => CancellationSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_cancellation() => CancellationSpan.Events.Count(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).ShouldEqual(1);
    [Fact] void should_set_the_status_to_error_for_a_fault() => ErrorSpan.Status.ShouldEqual(ActivityStatusCode.Error);

    class TheValidationFailure : Exception, IValidationFailure
    {
        public ValidationResult ValidationResult { get; } = ValidationResult.Error("missing identifier");
    }
}
