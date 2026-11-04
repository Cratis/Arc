// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_a_silent_enumerable_subscription_is_unsubscribed : given.a_scoped_subscription_container
{
    TaskCompletionSource<bool> _waiting;
    bool _pendingBeforeUnsubscribe;

    void Establish()
    {
        _waiting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _result = () =>
        {
            var result = QueryResult.Success(CorrelationId.New());
            result.Data = new ObservableEnumerable<IEnumerable<string>>(_subject, _waiting);
            return result;
        };
    }

    async Task Because() => await RunConnection(async () =>
    {
        _pendingBeforeUnsubscribe = await _waiting.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var probe = _resolvedDuringPerform.Single();
        await Unsubscribe(FirstQueryId);
        await WaitFor(() => probe.Disposals > 0);
    });

    [Fact] void should_have_a_pending_move_before_unsubscribe() => _pendingBeforeUnsubscribe.ShouldBeTrue();
    [Fact] void should_dispose_the_subscription_scope_once() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);

    sealed class ObservableEnumerable<T>(IObservable<T> source, TaskCompletionSource<bool> waiting) : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new WaitingEnumerator<T>(source, waiting, cancellationToken);
    }

    sealed class WaitingEnumerator<T>(IObservable<T> source, TaskCompletionSource<bool> waiting, CancellationToken token) : IAsyncEnumerator<T>
    {
        readonly ObservableAsyncEnumerator<T> _enumerator = new(source, token);

        public T Current => _enumerator.Current;

        public ValueTask DisposeAsync() => _enumerator.DisposeAsync();

        public ValueTask<bool> MoveNextAsync()
        {
            var move = _enumerator.MoveNextAsync();
            waiting.TrySetResult(!move.IsCompleted);

            return move;
        }
    }
}
