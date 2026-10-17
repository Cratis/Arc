// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_pipeline_throws : given.an_occurrence_command_executor
{
    CorrelationId _previous;
    CorrelationId _current;
    CorrelationId _duringPipeline;
    Exception _failure;

    void Establish()
    {
        CreateExecutor();
        _previous = CorrelationId.New();
        _current = _previous;
        ((ICorrelationIdAccessor)_correlationIdModifier).Current.Returns(_ => _current);
        _correlationIdModifier.When(_ => _.Modify(Arg.Any<CorrelationId>())).Do(_ => _current = _.Arg<CorrelationId>());
        _failure = new Exception("pipeline failed");
        _duringExecution = () =>
        {
            _duringPipeline = _current;
            throw _failure;
        };
    }

    async Task Because() => _exception = await Catch.Exception(() => _executor.Execute(_command, _occurrence, "send-reminder"));

    [Fact] void should_propagate_the_pipeline_failure() => _exception.ShouldBeSame(_failure);
    [Fact] void should_run_with_a_fresh_correlation_id() => _duringPipeline.ShouldNotEqual(_previous);
    [Fact] void should_restore_the_previous_correlation_id() => _current.ShouldEqual(_previous);
    [Fact] void should_not_leave_an_occurrence_execution_current() => OccurrenceExecution.Current.ShouldBeNull();
    [Fact] async Task should_not_record_a_receipt() => (await StoredReceipt).ShouldBeNull();
}
