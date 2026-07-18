// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_a_filter_throws : Specification
{
    const string ThrownMessage = "the filter blew up";

    CommandFilters _commandFilters;
    ICommandFilter _firstFilter;
    ICommandFilter _throwingFilter;
    ICommandFilter _laterFilter;
    CommandContext _context;
    CommandResult _result;
    CommandResult _firstFilterResult;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        _firstFilter = Substitute.For<ICommandFilter>();
        _throwingFilter = Substitute.For<ICommandFilter>();
        _laterFilter = Substitute.For<ICommandFilter>();
        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new());

        _firstFilterResult = CommandResult.Success(_context.CorrelationId);
        _firstFilter.OnExecution(_context).Returns(Task.FromResult(_firstFilterResult));
        _throwingFilter.OnExecution(_context).Returns<Task<CommandResult>>(_ => throw new InvalidOperationException(ThrownMessage));

        var filters = new List<ICommandFilter> { _firstFilter, _throwingFilter, _laterFilter };
        var commandFiltersActivitySource = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        commandFiltersActivitySource.ActualSource.Returns(_activitySource);
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>(filters), commandFiltersActivitySource);
    }

    void Destroy() => _activitySource.Dispose();

    async Task Because() => _result = await _commandFilters.OnExecution(_context);

    [Fact] void should_call_the_first_filter() => _firstFilter.Received(1).OnExecution(_context);
    [Fact] void should_call_the_throwing_filter() => _throwingFilter.Received(1).OnExecution(_context);
    [Fact] void should_short_circuit_and_not_call_the_later_filter() => _laterFilter.DidNotReceive().OnExecution(_context);
    [Fact] void should_carry_the_thrown_error() => _result.ExceptionMessages.ShouldContain(ThrownMessage);
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_prior_authorized_verdict() => _result.IsAuthorized.ShouldBeTrue();
}
