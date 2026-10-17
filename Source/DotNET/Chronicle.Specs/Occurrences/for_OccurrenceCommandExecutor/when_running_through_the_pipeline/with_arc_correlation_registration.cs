// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_arc_correlation_registration : given.an_in_process_executor
{
    CorrelationId _previous;
    CorrelationId _afterExecution;
    CorrelationId _eventCorrelation;

    async Task Because()
    {
        var accessor = _services.GetRequiredService<ICorrelationIdAccessor>();
        _previous = CorrelationId.New();
        CorrelationIdAccessor.SetCurrent(_previous);
        try
        {
            _result = await Execute(new RecordEffect("production-registration"));
            _afterExecution = accessor.Current;
            _eventCorrelation = (await _store.EventLog.GetFromSequenceNumber(0)).Single().Context.CorrelationId;
        }
        finally
        {
            CorrelationIdAccessor.SetCurrent(CorrelationId.NotSet);
        }
    }

    [Fact] void should_not_require_a_separate_modifier_registration() => _services.GetService<ICorrelationIdModifier>().ShouldBeNull();
    [Fact] void should_record_the_unrecorded_effect() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Recorded);
    [Fact] void should_run_the_handler() => _counter.Calls.ShouldEqual(1);
    [Fact] void should_give_the_handler_a_fresh_correlation_id() => _counter.CorrelationId.ShouldNotEqual(_previous);
    [Fact] void should_give_the_handler_a_set_correlation_id() => _counter.CorrelationId.ShouldNotEqual(CorrelationId.NotSet);
    [Fact] void should_use_the_same_correlation_in_the_pipeline() => _result.CommandResult!.CorrelationId.ShouldEqual(_counter.CorrelationId);
    [Fact] void should_use_the_same_correlation_for_the_append() => _eventCorrelation.ShouldEqual(_counter.CorrelationId);
    [Fact] void should_restore_the_callers_correlation_id() => _afterExecution.ShouldEqual(_previous);
}
