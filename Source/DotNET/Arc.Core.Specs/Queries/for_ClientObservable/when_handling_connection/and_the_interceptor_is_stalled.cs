// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_the_interceptor_is_stalled : given.a_guarded_client_observable
{
    TaskCompletionSource _release;
    int _entries;
    bool _stillObserved;

    void Establish()
    {
        _arcOptions.Query.MaxQueuedEmissionsPerSubscription = 3;
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _emissionGuards.HasGuards.Returns(false);
        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call =>
            {
                Interlocked.Increment(ref _entries);
                return _release.Task.ContinueWith(_ => call.ArgAt<IEnumerable<object>>(1), TaskScheduler.Default);
            });
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext("first");
        await WaitFor(() => _entries == 1);

        for (var index = 0; index < 10; index++)
        {
            _subject.OnNext($"queued-{index}");
        }

        await WaitFor(() => !_subject.HasObservers);
        _stillObserved = _subject.HasObservers;
        _release.SetResult();
    });

    [Fact] void should_enter_the_interceptor_once() => _entries.ShouldEqual(1);
    [Fact] void should_detach_from_the_producer_while_the_interceptor_is_stalled() => _stillObserved.ShouldBeFalse();
}
