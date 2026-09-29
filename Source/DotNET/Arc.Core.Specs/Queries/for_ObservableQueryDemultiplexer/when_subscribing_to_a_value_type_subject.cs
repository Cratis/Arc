// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

/// <summary>
/// The subscription path is closed over the subject's element type at runtime. A value type element exercises that
/// closing for a type that cannot share the reference-type instantiation, and the replay of a
/// <see cref="BehaviorSubject{T}"/> happens synchronously from inside the subscribe call.
/// </summary>
public class when_subscribing_to_a_value_type_subject : given.an_observable_query_demultiplexer
{
    readonly BehaviorSubject<int> _subject = new(42);
    readonly List<QueryResult> _results = [];
    IDisposable _subscription;

    async Task Because()
    {
        _subscription = _hub.SubscribeToStreamingData(
            Substitute.For<IHttpRequestContext>(),
            _subject,
            "query-1",
            new PagingInfo(0, 0, 0),
            null,
            CorrelationId.New(),
            new ObservableQuerySubscriptionIdentity("[TestApp].[TestQuery]", QueryArguments.Empty, null),
            (result, _) =>
            {
                lock (_results)
                {
                    _results.Add(result);
                }
                _signals.Signal();
                return Task.CompletedTask;
            },
            (_, _, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        await WaitFor(() => _results.Count == 1);
        _subject.OnNext(43);
        await WaitFor(() => _results.Count == 2);
        _subscription.Dispose();
        _subject.OnNext(44);
        await Task.Delay(50);
    }

    [Fact] void should_return_a_subscription() => _subscription.ShouldNotBeNull();
    [Fact] void should_replay_the_current_value_first() => _results[0].Data.ShouldEqual(42);
    [Fact] void should_send_the_next_value() => _results[1].Data.ShouldEqual(43);
    [Fact] void should_not_carry_a_change_set_for_a_single_value() => _results[1].ChangeSet.ShouldBeNull();
    [Fact] void should_stop_sending_after_the_subscription_is_disposed() => _results.Count.ShouldEqual(2);
}
