// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_first_filter_returns_null : Specification
{
    CommandFilters _commandFilters;
    ICommandFilter _filter1;
    ICommandFilter _filter2;
    CommandContext _context;
    CommandResult _result;
    CommandResult _filterResult;

    void Establish()
    {
        _filter1 = Substitute.For<ICommandFilter>();
        _filter2 = Substitute.For<ICommandFilter>();
        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new());
        _filterResult = CommandResult.Error(_context.CorrelationId, "error");
        _filter1.OnExecution(_context).Returns(Task.FromResult<CommandResult>(null!));
        _filter2.OnExecution(_context).Returns(Task.FromResult(_filterResult));
        var filters = new List<ICommandFilter> { _filter1, _filter2 };
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>(filters));
    }

    async Task Because() => _result = await _commandFilters.OnExecution(_context);

    [Fact] void should_call_first_filter() => _filter1.Received(1).OnExecution(_context);
    [Fact] void should_call_second_filter() => _filter2.Received(1).OnExecution(_context);
    [Fact]
    void should_contain_errors_from_second_filter()
    {
        _result.IsSuccess.ShouldBeFalse();
        _result.ExceptionMessages.Count().ShouldEqual(1);
        _result.ExceptionMessages.First().ShouldEqual(_filterResult.ExceptionMessages.First());
    }
}
