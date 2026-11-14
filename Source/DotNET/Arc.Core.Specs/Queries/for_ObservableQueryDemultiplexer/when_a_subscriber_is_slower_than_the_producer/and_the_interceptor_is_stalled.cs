// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_a_subscriber_is_slower_than_the_producer;

public class and_the_interceptor_is_stalled : given.a_guarded_websocket_connection
{
    const int Capacity = 3;

    TaskCompletionSource _release;
    Subject<IEnumerable<string>> _stalledSubject;
    Subject<IEnumerable<string>> _healthySubject;
    int _interceptorEntries;
    int _performs;
    bool _healthyStillObserved;
    bool _stalledStillObserved;

    void Establish()
    {
        _queryIds = [FirstQueryId, SecondQueryId];
        _arcOptions.Value.Query.MaxQueuedEmissionsPerSubscription = Capacity;
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _stalledSubject = new();
        _healthySubject = new();

        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var result = QueryResult.Success(CorrelationId.New());
                result.Data = Interlocked.Increment(ref _performs) == 1 ? _stalledSubject : _healthySubject;
                return Task.FromResult(result);
            });

        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call =>
            {
                var items = call.ArgAt<IEnumerable<object>>(1);
                if (!items.Contains("stall"))
                {
                    return Task.FromResult(items);
                }

                Interlocked.Increment(ref _interceptorEntries);
                _signals.Signal();
                return _release.Task.ContinueWith(_ => items, TaskScheduler.Default);
            });
    }

    async Task Because() => await RunConnection(async () =>
    {
        await WaitFor(() => _stalledSubject.HasObservers && _healthySubject.HasObservers);
        _stalledSubject.OnNext(["stall"]);
        await WaitFor(() => _interceptorEntries == 1);

        for (var index = 0; index < 10; index++)
        {
            _stalledSubject.OnNext(["stall"]);
        }

        await WaitFor(() => _unregisteredCount == 1);
        _stalledStillObserved = _stalledSubject.HasObservers;
        _healthySubject.OnNext(["healthy"]);
        await WaitFor(() => _sentMessages.Any(_ => _.Type == ObservableQueryHubMessageType.QueryResult));
        _healthyStillObserved = _healthySubject.HasObservers;
        _release.SetResult();
    });

    [Fact] void should_enter_the_interceptor_once() => _interceptorEntries.ShouldEqual(1);
    [Fact] void should_detach_the_stalled_subscription() => _stalledStillObserved.ShouldBeFalse();
    [Fact] void should_leave_the_other_subscription_attached() => _healthyStillObserved.ShouldBeTrue();
    [Fact] void should_deliver_to_the_other_subscription() => _sentMessages.Count(_ => _.Type == ObservableQueryHubMessageType.QueryResult).ShouldEqual(1);
    [Fact] void should_tell_the_client_why() => _sentMessages.Any(_ => _.Type == ObservableQueryHubMessageType.Error).ShouldBeTrue();
}
