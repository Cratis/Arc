// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_all_filters_succeed : Specification
{
    CommandFilters _commandFilters;
    ICommandFilter _filter1;
    ICommandFilter _filter2;
    CommandContext _context;
    CommandResult _result;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        _filter1 = Substitute.For<ICommandFilter>();
        _filter2 = Substitute.For<ICommandFilter>();
        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new());

        _filter1.OnExecution(_context).Returns(Task.FromResult<CommandResult>(null!));
        _filter2.OnExecution(_context).Returns(Task.FromResult(CommandResult.Success(_context.CorrelationId)));

        var filters = new List<ICommandFilter> { _filter1, _filter2 };
        var commandFiltersActivitySource = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        commandFiltersActivitySource.ActualSource.Returns(_activitySource);
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>(filters), commandFiltersActivitySource);
    }

    void Destroy() => _activitySource.Dispose();

    async Task Because() => _result = await _commandFilters.OnExecution(_context);

    [Fact] void should_call_the_first_filter() => _filter1.Received(1).OnExecution(_context);
    [Fact] void should_call_the_second_filter() => _filter2.Received(1).OnExecution(_context);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
}
