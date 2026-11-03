// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_releasing_without_dynamic_code;

public class and_release_completes_later_under_a_single_threaded_context : given.a_read_model_resolution
{
    TaskCompletionSource<TestReadModel> _completion;
    ManualResetEventSlim _releaseCalled;
    object _result;
    bool _blockedBeforeCompletion;
    bool _finished;

    void Establish()
    {
        GivenReadModelExists();
        _releasedReadModel = new TestReadModel("decrypted");
        _completion = new TaskCompletionSource<TestReadModel>();
        _releaseCalled = new ManualResetEventSlim();
        _readModels.Release(_readModel).Returns(_ =>
        {
            _releaseCalled.Set();
            return _completion.Task;
        });
    }

    void Because()
    {
        // The releasing thread blocks on the task while a context that never runs posted work is current, the way a
        // single-threaded synchronization context does while its only thread waits.
        var releasing = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new BlockedSynchronizationContext());
            _result = Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ReleaseReadModelWithoutDynamicCode(_readModels, typeof(TestReadModel), _readModel);
        })
        {
            IsBackground = true
        };
        releasing.Start();

        // Complete the release only once the releasing thread is blocked waiting for the result.
        _blockedBeforeCompletion = _releaseCalled.Wait(TimeSpan.FromSeconds(10)) &&
            SpinWait.SpinUntil(() => releasing.ThreadState.HasFlag(ThreadState.WaitSleepJoin), TimeSpan.FromSeconds(10));
        _completion.SetResult(_releasedReadModel);
        _finished = releasing.Join(TimeSpan.FromSeconds(10));
    }

    void Destroy() => _releaseCalled.Dispose();

    [Fact] void should_block_on_the_incomplete_task_before_it_completes() => _blockedBeforeCompletion.ShouldBeTrue();
    [Fact] void should_finish_releasing() => _finished.ShouldBeTrue();
    [Fact] void should_return_what_release_returned() => _result.ShouldEqual(_releasedReadModel);

    sealed class BlockedSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
        }
    }
}
