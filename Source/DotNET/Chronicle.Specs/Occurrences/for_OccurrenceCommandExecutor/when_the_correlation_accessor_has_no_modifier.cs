// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_correlation_accessor_has_no_modifier : given.an_occurrence_command_executor
{
    CorrelationId _previous;
    CorrelationId _duringPipeline;
    CorrelationId _afterExecution;

    void Establish()
    {
        CreateExecutor();
        var accessor = Substitute.For<ICorrelationIdAccessor>();
        accessor.Current.Returns(_ => new CorrelationIdAccessor().Current);
        _services.GetService(typeof(ICorrelationIdAccessor)).Returns(accessor);
        _duringExecution = () =>
        {
            _duringPipeline = accessor.Current;
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        _previous = CorrelationId.New();
        CorrelationIdAccessor.SetCurrent(_previous);
        try
        {
            _result = await _executor.Execute(_command, _occurrence, "send-reminder");
            _afterExecution = new CorrelationIdAccessor().Current;
        }
        finally
        {
            CorrelationIdAccessor.SetCurrent(CorrelationId.NotSet);
        }
    }

    [Fact] void should_settle_the_execution() => _result.IsSettled.ShouldBeTrue();
    [Fact] void should_give_the_pipeline_a_fresh_correlation_id() => _duringPipeline.ShouldNotEqual(_previous);
    [Fact] void should_give_the_pipeline_a_set_correlation_id() => _duringPipeline.ShouldNotEqual(CorrelationId.NotSet);
    [Fact] void should_restore_the_callers_correlation_id() => _afterExecution.ShouldEqual(_previous);
}
