// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

public class when_subscribing_to_a_collection_without_identity : given.an_observable_query_demultiplexer
{
    record Item(string Name);

    readonly Subject<IEnumerable<Item>> _subject = new();
    readonly List<QueryResult> _deltaResults = [];
    readonly List<QueryResult> _legacyResults = [];
    given.observed_logger _observedLogger;

    void Establish()
    {
        _observedLogger = new given.observed_logger(_signals);
        _logger = _observedLogger;
        UseRealQueryContextManager();
    }

    async Task Because()
    {
        using var delta = Subscribe("delta", "delta-query", _deltaResults);
        using var legacy = Subscribe(null, "legacy-query", _legacyResults);

        _subject.OnNext([new Item("first")]);
        await WaitFor(() => _deltaResults.Count == 1 && _legacyResults.Count == 1);
        _subject.OnNext([new Item("changed"), new Item("second")]);
        await WaitFor(() => _deltaResults.Count == 2 && _legacyResults.Count == 2);
        _subject.OnNext([new Item("last")]);
        await WaitFor(() => _deltaResults.Count == 3 && _legacyResults.Count == 3);
    }

    [Fact] void should_send_all_data_in_delta_mode() => ((IEnumerable<Item>)_deltaResults[2].Data).Select(_ => _.Name).ShouldEqual(["last"]);
    [Fact] void should_send_all_data_in_legacy_mode() => ((IEnumerable<Item>)_legacyResults[1].Data).Select(_ => _.Name).ShouldEqual(["changed", "second"]);
    [Fact] void should_never_send_a_change_set() => _deltaResults.Concat(_legacyResults).All(_ => _.ChangeSet is null).ShouldBeTrue();
    [Fact] void should_warn_once_for_each_subscription() => WarningEntries().Count.ShouldEqual(2);
    [Fact] void should_include_query_identity_in_structured_warnings() =>
        WarningEntries().Select(_ => _.State.Single(property => property.Key == "QueryId").Value?.ToString()).Order()
            .ShouldEqual(["delta-query", "legacy-query"]);
    [Fact] void should_explain_the_identity_requirement() =>
        WarningEntries().All(_ => _.State.Single(property => property.Key == "{OriginalFormat}").Value?.ToString()?.Contains("delta transfer requires a stable identity (Id)") is true).ShouldBeTrue();

    IReadOnlyCollection<(LogLevel Level, IReadOnlyList<KeyValuePair<string, object?>> State)> WarningEntries() =>
        _observedLogger.Entries.Where(_ => _.Level == LogLevel.Warning).ToArray();

    IDisposable Subscribe(string? mode, string queryId, List<QueryResult> results) =>
        _hub.SubscribeToStreamingData(
            Substitute.For<IHttpRequestContext>(),
            _subject,
            queryId,
            new PagingInfo(0, 0, 0),
            mode,
            CorrelationId.New(),
            new ObservableQuerySubscriptionIdentity("[TestApp].[TestQuery]", QueryArguments.Empty, null),
            (result, _) =>
            {
                results.Add(result);
                _signals.Signal();
                return Task.CompletedTask;
            },
            (_, _, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            CancellationToken.None)!;
}
