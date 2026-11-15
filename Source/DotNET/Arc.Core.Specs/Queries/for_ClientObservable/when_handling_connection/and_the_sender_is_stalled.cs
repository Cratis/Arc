// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_the_sender_is_stalled : given.a_guarded_client_observable
{
    TaskCompletionSource _release;
    int _sendsStarted;
    bool _stillObserved;

    void Establish()
    {
        _arcOptions.Query.MaxQueuedEmissionsPerSubscription = 3;
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _emissionGuards.HasGuards.Returns(false);
        _webSocketConnectionHandler
            .SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                Interlocked.Increment(ref _sendsStarted);
                await _release.Task;
                return null;
            });
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext("first");
        await WaitFor(() => _sendsStarted == 1);

        for (var index = 0; index < 10; index++)
        {
            _subject.OnNext($"queued-{index}");
        }

        await WaitFor(() => !_subject.HasObservers);
        _stillObserved = _subject.HasObservers;
        _release.SetResult();
    });

    [Fact] void should_start_at_most_one_send() => _sendsStarted.ShouldEqual(1);
    [Fact] void should_detach_from_the_producer_while_the_sender_is_stalled() => _stillObserved.ShouldBeFalse();
}
