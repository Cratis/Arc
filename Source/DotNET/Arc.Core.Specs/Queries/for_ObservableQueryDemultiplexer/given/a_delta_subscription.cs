// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

/// <summary>
/// A delta-mode subscription to a collection of items with identity, recording every result it sends.
/// </summary>
public class a_delta_subscription : an_observable_query_demultiplexer
{
    protected readonly Subject<IEnumerable<Item>> _subject = new();
    protected readonly List<QueryResult> _results = [];
    IDisposable? _subscription;

    /// <summary>
    /// Makes the query sorted by name.
    /// </summary>
    protected void SortByName() => _queryContextManager.Current.Returns(
        new QueryContext("[TestApp].[TestQuery]", CorrelationId.New(), Paging.NotPaged, new Sorting(nameof(Item.Name), SortDirection.Ascending)));

    /// <summary>
    /// Makes the query paged.
    /// </summary>
    protected void Page() => _queryContextManager.Current.Returns(
        new QueryContext("[TestApp].[TestQuery]", CorrelationId.New(), new Paging(0, 10, true), Sorting.None));

    /// <summary>
    /// Subscribes, then emits each snapshot in turn and waits for its result.
    /// </summary>
    /// <param name="snapshots">The snapshots to emit.</param>
    /// <returns>A <see cref="Task"/> that completes once every snapshot has been sent.</returns>
    protected async Task Emit(params IEnumerable<Item>[] snapshots)
    {
        _subscription = _hub.SubscribeToStreamingData(
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

        foreach (var (snapshot, index) in snapshots.Select((snapshot, index) => (snapshot, index)))
        {
            _subject.OnNext(snapshot);
            await WaitFor(() => _results.Count == index + 1);
        }
    }

    /// <summary>
    /// The ids of the items a result carries as data.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The ids, in order.</returns>
    protected static int[] IdsIn(QueryResult result) => [.. ((IEnumerable<Item>)result.Data).Select(item => item.Id)];

    void Destroy() => _subscription?.Dispose();

    /// <summary>
    /// An item with identity.
    /// </summary>
    /// <param name="Id">The identity.</param>
    /// <param name="Name">The name it is sorted by.</param>
    public record Item(int Id, string Name);
}
