// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

public class when_subscribing_to_an_interface_typed_collection : given.an_observable_query_demultiplexer
{
    interface IHasIdentity
    {
        string Id { get; }
    }

    interface IReadModel : IHasIdentity;

    record Item(string Id) : IReadModel;

    readonly Subject<IEnumerable<IReadModel>> _subject = new();
    readonly List<QueryResult> _results = [];
    given.observed_logger _observedLogger;
    int _warningsAfterEmptyEmission;

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

        _subject.OnNext([]);
        await WaitFor(() => _results.Count == 1);
        _warningsAfterEmptyEmission = _observedLogger.Entries.Count(_ => _.Level == LogLevel.Warning);

        _subject.OnNext([]);
        await WaitFor(() => _results.Count == 2);
        _subject.OnNext([new Item("item-a")]);
        await WaitFor(() => _results.Count == 3);
        _subject.OnNext([new Item("item-a"), new Item("item-b")]);
        await WaitFor(() => _results.Count == 4);
    }

    [Fact] void should_not_warn_for_an_empty_first_emission() => _warningsAfterEmptyEmission.ShouldEqual(0);
    [Fact] void should_not_warn_when_items_have_identity() => _observedLogger.Entries.Count(_ => _.Level == LogLevel.Warning).ShouldEqual(0);
    [Fact] void should_send_the_empty_initial_snapshot() => ((IEnumerable<IReadModel>)_results[0].Data).Count().ShouldEqual(0);
    [Fact] void should_use_inherited_interface_identity_for_the_second_empty_delta() => _results[1].ChangeSet.ShouldNotBeNull();
    [Fact] void should_send_deltas_for_later_items() => _results[2].ChangeSet!.Added.Count().ShouldEqual(1);
    [Fact] void should_send_only_the_new_item_in_the_next_delta() => _results[3].ChangeSet!.Added.Count().ShouldEqual(1);
    [Fact] void should_omit_full_data_on_later_emissions() => _results[3].Data.ShouldBeNull();
}
