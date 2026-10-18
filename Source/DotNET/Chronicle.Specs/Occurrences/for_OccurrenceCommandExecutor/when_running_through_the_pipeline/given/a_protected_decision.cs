// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline.given;

public class a_protected_decision : an_in_process_executor
{
    async Task Establish() => (await _store.EventLog.Append("read-source", new EffectHappened("before"), correlationId: CorrelationId.New())).IsSuccess.ShouldBeTrue();

    protected void ChangeReadBeforeCommit() => _counter.AfterRead = async () =>
    {
        Task append;
        using (ExecutionContext.SuppressFlow())
        {
            append = Task.Run(async () => (await _store.EventLog.Append("read-source", new EffectHappened("changed"), correlationId: CorrelationId.New())).IsSuccess.ShouldBeTrue());
        }
        await append;
    };
}
