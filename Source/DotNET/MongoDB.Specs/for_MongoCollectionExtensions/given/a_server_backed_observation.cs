// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Threading.Channels;
using Cratis.Arc.Queries;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.given;

/// <summary>
/// An observation, filtered to documents not named <see cref="Excluded"/>, over a collection that answers count and find the way a server would: it filters the stored
/// documents, sorts them by the rendered sort definition and applies skip and limit. A spec changes
/// <see cref="an_observed_collection._documents"/>, pushes the matching change event and reads the emissions.
/// </summary>
public class a_server_backed_observation : an_observed_collection
{
    protected const string Excluded = "Excluded";
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    protected Func<ObservedDocument, bool> _matches = document => document.Name != Excluded;
    protected ISubject<IEnumerable<ObservedDocument>> _subject;

    readonly Channel<Emission> _emissions = Channel.CreateUnbounded<Emission>();
    IDisposable _subscription;

    void Establish()
    {
        _collection
            .CountDocumentsAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult((long)_documents.Count(_matches)));

        _collection
            .FindAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<FindOptions<ObservedDocument, ObservedDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var options = call.Arg<FindOptions<ObservedDocument, ObservedDocument>>();
                _primaryFindOptions = options;
                await _initialQueryGate.Task;
                return CreateCursor(Read(options));
            });
    }

    void Destroy() => _subscription?.Dispose();

    /// <summary>
    /// Pages and sorts the observation by name ascending.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="size">The page size.</param>
    protected void PageByName(int page, int size) => _queryContext = _queryContext with
    {
        Paging = new Paging(page, size, true),
        Sorting = new Sorting(nameof(ObservedDocument.Name), Cratis.Arc.Queries.SortDirection.Ascending)
    };

    /// <summary>
    /// Starts observing the documents not named <see cref="Excluded"/> and returns the initial emission.
    /// </summary>
    /// <returns>The initial <see cref="Emission"/>.</returns>
    protected Task<Emission> StartObserving()
    {
        _subject = _collection.Observe(document => document.Name != Excluded);
        _subscription = _subject.Subscribe(documents => _emissions.Writer.TryWrite(new(
            [.. documents],
            _queryContext.TotalItems,
            (documents as IHaveKnownChanges)?.Changes)));
        _initialQueryGate.SetResult();
        return NextEmission();
    }

    /// <summary>
    /// Awaits the next emission not yet read.
    /// </summary>
    /// <returns>The next <see cref="Emission"/>.</returns>
    protected async Task<Emission> NextEmission() => await _emissions.Reader.ReadAsync().AsTask().WaitAsync(Deadline);

    /// <summary>
    /// Renders the sort the observation's query used.
    /// </summary>
    /// <returns>The rendered sort, or <see langword="null"/> when the query was not sorted.</returns>
    protected BsonDocument? RenderedSort() => _primaryFindOptions.Sort?.Render(
        new(BsonSerializer.LookupSerializer<ObservedDocument>(), BsonSerializer.SerializerRegistry));

    IEnumerable<ObservedDocument> Read(FindOptions<ObservedDocument, ObservedDocument> options)
    {
        IEnumerable<ObservedDocument> result = _documents.Where(_matches).ToList();
        var sort = options.Sort?.Render(new(BsonSerializer.LookupSerializer<ObservedDocument>(), BsonSerializer.SerializerRegistry));
        if (sort is not null)
        {
            result = result.Order(Comparer<ObservedDocument>.Create((left, right) => Compare(sort, left, right)));
        }

        result = result.Skip(options.Skip ?? 0);
        return options.Limit is { } limit ? result.Take(limit) : result;
    }

    static int Compare(BsonDocument sort, ObservedDocument left, ObservedDocument right)
    {
        foreach (var element in sort)
        {
            var comparison = element.Name == "_id"
                ? DocumentIdComparer.Instance.Compare(left.Id, right.Id)
                : string.CompareOrdinal(left.Name, right.Name);
            if (comparison != 0)
            {
                return element.Value.ToInt32() < 0 ? -comparison : comparison;
            }
        }

        return 0;
    }

    /// <summary>
    /// What one emission of the observation carried.
    /// </summary>
    /// <param name="Page">The documents on the page, in order.</param>
    /// <param name="TotalItems">The total reported alongside the page.</param>
    /// <param name="Changes">The changes the emission stated.</param>
    protected record Emission(IReadOnlyList<ObservedDocument> Page, int TotalItems, IReadOnlyList<CollectionChange>? Changes);
}
