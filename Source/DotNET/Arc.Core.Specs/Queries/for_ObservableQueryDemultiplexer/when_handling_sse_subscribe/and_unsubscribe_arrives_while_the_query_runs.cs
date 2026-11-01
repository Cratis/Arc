// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_unsubscribe_arrives_while_the_query_runs : given.a_scoped_subscription_container
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _exception;

    void Establish() => _performResult = async _ =>
    {
        await _release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var result = QueryResult.Success(CorrelationId.New());
        result.Data = _subject;
        return result;
    };

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        var subscribing = _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId));
        await WaitFor(() => !_resolvedDuringPerform.IsEmpty);
        await Unsubscribe(FirstQueryId);
        _release.SetResult();
        _exception = await Catch.Exception(() => subscribing);
        await WaitFor(() => _resolvedDuringPerform.Single().Disposals > 0);

        await _connectionCancellation.CancelAsync();
        await connectionTask;
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_dispose_the_probe_exactly_once() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
}
