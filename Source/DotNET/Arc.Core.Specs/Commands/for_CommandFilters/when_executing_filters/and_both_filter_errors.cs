// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_both_filter_errors : Specification
{
    CommandFilters _commandFilters;
    ICommandFilter _filter1;
    ICommandFilter _filter2;
    CommandContext _context;
    CommandResult _result;
    CommandResult _firstFilterResult;
    CommandResult _secondFilterResult;

    void Establish()
    {
        _filter1 = Substitute.For<ICommandFilter>();
        _filter2 = Substitute.For<ICommandFilter>();
        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new());

        _firstFilterResult = new()
        {
            IsAuthorized = false
        };

        _secondFilterResult = new()
        {
            ValidationResults = [new ValidationResult(ValidationResultSeverity.Error, "error", [], null!)]
        };
        _filter1.OnExecution(_context).Returns(Task.FromResult(_firstFilterResult));
        _filter2.OnExecution(_context).Returns(Task.FromResult(_secondFilterResult));
        var filters = new List<ICommandFilter> { _filter1, _filter2 };
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>(filters));
    }

    async Task Because() => _result = await _commandFilters.OnExecution(_context);

    [Fact] void should_call_first_filter() => _filter1.Received(1).OnExecution(_context);
    [Fact] void should_call_second_filter() => _filter2.Received(1).OnExecution(_context);
    [Fact]
    void should_return_result_combining_both_filter_results()
    {
        _result.IsAuthorized.ShouldBeFalse();
        _result.IsValid.ShouldBeFalse();
        _result.ValidationResults.ShouldContain(_secondFilterResult.ValidationResults.First());
    }
}
