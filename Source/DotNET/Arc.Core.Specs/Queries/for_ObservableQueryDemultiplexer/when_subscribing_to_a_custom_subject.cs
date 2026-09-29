// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

/// <summary>
/// A query may return its own <see cref="ISubject{T}"/> implementation rather than one of the Rx subjects; the element
/// type is then found from the interface the class implements.
/// </summary>
public class when_subscribing_to_a_custom_subject : given.an_observable_query_demultiplexer
{
    readonly Relay _subject = new();
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

        _subject.OnNext(new("first"));
        await WaitFor(() => _results.Count == 1);
    }

    void Destroy() => _subscription?.Dispose();

    [Fact] void should_return_a_subscription() => _subscription.ShouldNotBeNull();
    [Fact] void should_send_the_emitted_value() => ((Item)_results[0].Data).Name.ShouldEqual("first");

    record Item(string Name);

    sealed class Relay : ISubject<Item>, IDisposable
    {
        readonly Subject<Item> _inner = new();

        public void OnCompleted() => _inner.OnCompleted();
        public void OnError(Exception error) => _inner.OnError(error);
        public void OnNext(Item value) => _inner.OnNext(value);
        public IDisposable Subscribe(IObserver<Item> observer) => _inner.Subscribe(observer);
        public void Dispose() => _inner.Dispose();
    }
}
