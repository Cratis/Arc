// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_the_event_store_is_pending : given.a_startup
{
    readonly TaskCompletionSource<IDecisionReads?> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _requested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _startCompleted;
    bool _checkPending;

    void Establish()
    {
        _startupReads.GetHandler = _ =>
        {
            _requested.TrySetResult();
            return _pending.Task;
        };
        Configure(typeof(OnCommand));
    }

    async Task Because()
    {
        var start = _startup.StartAsync(CancellationToken.None);
        _startCompleted = start.IsCompletedSuccessfully;
        await start;
        await SignalApplicationStarted();
        await _requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _checkPending = !_startup.CheckTask.IsCompleted;
        _pending.SetResult(_reader);
        await _startup.CheckTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_finish_start_before_the_event_store_is_ready() => _startCompleted.ShouldBeTrue();
    [Fact] void should_keep_the_background_check_pending() => _checkPending.ShouldBeTrue();
    [Fact] void should_check_admission_after_the_event_store_arrives() => _lifetime.Received(1).StopApplication();
}
