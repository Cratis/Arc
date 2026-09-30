// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Observability;
using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_authorization_is_traced_and_denies : Specification
{
    public record RegisterAuthor(string Name);

    CommandFilters _filters;
    CommandContext _context;
    ActivitySource _source;
    TelemetryRecorder _telemetry;

    void Establish()
    {
        _context = new CommandContext(CorrelationId.New(), typeof(RegisterAuthor), new RegisterAuthor("a name"), [], new());
        var authorization = Substitute.For<IAuthorizationCommandFilter>();
        authorization.OnExecution(_context).Returns(CommandResult.Unauthorized(_context.CorrelationId));
        var validation = Substitute.For<ICommandFilter>();
        validation.OnExecution(_context).Returns(CommandResult.Success(_context.CorrelationId));

        _source = new ActivitySource("Cratis.Arc.Test");
        var activitySource = Substitute.For<IActivitySource<CommandFilters>>();
        activitySource.ActualSource.Returns(_source);
        _filters = new CommandFilters(new KnownInstancesOf<ICommandFilter>([validation, authorization]), activitySource);
        _telemetry = new TelemetryRecorder(_source);
    }

    void Destroy()
    {
        _telemetry.Dispose();
        _source.Dispose();
    }

    async Task Because() => await _filters.OnExecution(_context);

    Activity AuthorizeSpan => _telemetry.Span(WellKnownTelemetryNames.CommandAuthorizeSpan);

    [Fact] void should_name_the_span_after_the_command() => AuthorizeSpan.DisplayName.ShouldEqual($"authorize {nameof(RegisterAuthor)}");
    [Fact] void should_add_the_command_type() => AuthorizeSpan.GetTagItem(WellKnownTelemetryNames.CommandType).ShouldEqual(typeof(RegisterAuthor).FullName);
    [Fact] void should_leave_the_status_unset() => AuthorizeSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_add_an_authorization_denied_event() => AuthorizeSpan.Events.Count(_ => _.Name == WellKnownTelemetryNames.AuthorizationDeniedEvent).ShouldEqual(1);
    [Fact] void should_nest_it_in_the_filter_span() => AuthorizeSpan.ParentSpanId.ShouldEqual(_telemetry.Span(WellKnownTelemetryNames.CommandFilterSpan).SpanId);
    [Fact] void should_raise_one_authorize_span() => _telemetry.Activities.Count(_ => _.OperationName == WellKnownTelemetryNames.CommandAuthorizeSpan).ShouldEqual(1);
}
