// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

public class when_subscribing_to_a_collection_with_identity : given.an_observable_query_demultiplexer
{
    record Item(int id, string Name);

    readonly Subject<IEnumerable<Item>> _subject = new();
    readonly List<QueryResult> _results = [];
    given.observed_logger _observedLogger;

    void Establish()
    {
        _observedLogger = new given.observed_logger(_signals);
        _logger = _observedLogger;
        UseRealQueryContextManager();
    }

    async Task Because()
    {
        using var subscription = _hub.SubscribeToStreamingData(
            Substitute.For<IHttpRequestContext>(),
            _subject,
            "query-1",
            new PagingInfo(0, 0, 0),
            "delta",
            CorrelationId.New(),
            new ObservableQuerySubscriptionIdentity("[TestApp].[TestQuery]", QueryArguments.Empty, null),
            (result, _) =>
            {
                _results.Add(result);
                _signals.Signal();
                return Task.CompletedTask;
            },
            (_, _, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        _subject.OnNext([new Item(1, "first")]);
        await WaitFor(() => _results.Count == 1);
        _subject.OnNext([new Item(1, "updated"), new Item(2, "second")]);
        await WaitFor(() => _results.Count == 2);
    }

    [Fact] void should_send_initial_snapshot() => ((IEnumerable<Item>)_results[0].Data).Count().ShouldEqual(1);
    [Fact] void should_omit_data_on_later_delta() => _results[1].Data.ShouldBeNull();
    [Fact] void should_report_replaced_item() => _results[1].ChangeSet!.Replaced.Cast<Item>().Single().Name.ShouldEqual("updated");
    [Fact] void should_report_added_item() => _results[1].ChangeSet!.Added.Cast<Item>().Single().id.ShouldEqual(2);
    [Fact] void should_not_warn_about_identity() => _observedLogger.Entries.Count(_ => _.Level == LogLevel.Warning).ShouldEqual(0);
}
