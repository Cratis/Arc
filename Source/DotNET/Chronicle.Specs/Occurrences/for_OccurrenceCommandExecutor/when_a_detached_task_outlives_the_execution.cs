// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_a_detached_task_outlives_the_execution : given.an_occurrence_command_executor
{
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _detached;
    IUnitOfWork _laterUnit;
    bool _executionStillCurrent;

    void Establish()
    {
        CreateExecutor();
        _duringExecution = () =>
        {
            _detached = Task.Run(async () =>
            {
                await _resume.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
                _executionStillCurrent = OccurrenceExecution.Current is not null;
                _laterUnit = NewUnit(1);
                OccurrenceExecution.Current?.Enroll(_laterUnit);
                await _laterUnit.Commit();
            });
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        try
        {
            _result = await _executor.Execute(_command, _occurrence, "send-reminder");
        }
        finally
        {
            _resume.TrySetResult();
        }
        await _detached.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact] void should_settle_the_original_effect() => _result.IsSettled.ShouldBeTrue();
    [Fact] void should_end_the_execution_in_the_copied_context() => _executionStillCurrent.ShouldBeFalse();
    [Fact] void should_not_enroll_the_original_effect_key_in_the_later_unit() => _laterUnit.EffectReceipt.ShouldBeNull();
}
