// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Commands.Filters;
using Cratis.Execution;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_availability_is_unavailable_before_authorization : Specification
{
    CommandFilters _commandFilters;
    RecordingAuthorizationFilter _authorizationFilter;
    ServiceProvider _services;
    CommandContext _context;
    CommandResult _result;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        var gate = Substitute.For<IAvailabilityGate>();
        gate.IsGated(typeof(Command)).Returns(true);
        gate.Decide(typeof(Command), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(new AvailabilityDecision(AvailabilityVerdict.Unavailable, ["for Plus"]));
        _services = new ServiceCollection().AddSingleton(gate).BuildServiceProvider();
        _context = new CommandContext(CorrelationId.New(), typeof(Command), new Command(), [], new(), null, null, _services);

        // Authorization is discovered first, yet availability must still be decided before it.
        _authorizationFilter = new RecordingAuthorizationFilter();
        var filters = new List<ICommandFilter> { _authorizationFilter, new AvailabilityFilter() };
        var activitySource = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        activitySource.ActualSource.Returns(_activitySource);
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>(filters), activitySource);
    }

    void Destroy()
    {
        _services.Dispose();
        _activitySource.Dispose();
    }

    async Task Because() => _result = await _commandFilters.OnExecution(_context);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_report_the_not_available_reason() => _result.AuthorizationFailureReason.ShouldEqual(AvailabilityReason.NotAvailable);
    [Fact] void should_not_evaluate_authorization() => _authorizationFilter.Evaluated.ShouldBeFalse();

    public record Command;

    class RecordingAuthorizationFilter : IAuthorizationCommandFilter
    {
        public bool Evaluated { get; private set; }

        public Task<CommandResult> OnExecution(CommandContext context)
        {
            Evaluated = true;
            return Task.FromResult(CommandResult.Success(context.CorrelationId));
        }
    }
}
