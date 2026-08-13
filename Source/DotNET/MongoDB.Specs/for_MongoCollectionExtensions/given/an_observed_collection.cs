// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.given;

public class an_observed_collection : Specification
{
    protected IMongoCollection<ObservedDocument> _collection;
    protected List<ObservedDocument> _documents;
    protected TaskCompletionSource _initialQueryGate;
    protected CancellationTokenSource _changeStreamLifetime;

    void Establish()
    {
        _documents = [new(Guid.NewGuid(), "First"), new(Guid.NewGuid(), "Second")];
        _initialQueryGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _changeStreamLifetime = new();

        var queryContextManager = Substitute.For<IQueryContextManager>();
        queryContextManager.Current.Returns(new QueryContext("ObservedDocuments", CorrelationId.New(), Paging.NotPaged, Sorting.None));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(queryContextManager);
        Internals.ServiceProvider = services.BuildServiceProvider();

        _collection = Substitute.For<IMongoCollection<ObservedDocument>>();
        _collection.CollectionNamespace.Returns(new CollectionNamespace("testdb", "observeddocument"));
        _collection.Settings.Returns(new MongoCollectionSettings());

        _collection
            .CountDocumentsAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult((long)_documents.Count));

        // The initial find is gated so a spec can observe the subject both before and after the query completes —
        // the whole point being that nothing is emitted until it has.
        _collection
            .FindAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<FindOptions<ObservedDocument, ObservedDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await _initialQueryGate.Task;
                return CreateCursor(_documents);
            });

        // The change stream stays open for the lifetime of the spec, so the observable is never completed and
        // disposed underneath a subscriber.
        _collection
            .WatchAsync(
                Arg.Any<PipelineDefinition<ChangeStreamDocument<ObservedDocument>, ChangeStreamDocument<ObservedDocument>>>(),
                Arg.Any<ChangeStreamOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CreateChangeStreamCursor()));
    }

    void Destroy()
    {
        _initialQueryGate.TrySetResult();
        _changeStreamLifetime.Cancel();
        _changeStreamLifetime.Dispose();
    }

    protected static async Task<IEnumerable<ObservedDocument>> FirstEmission(
        ISubject<IEnumerable<ObservedDocument>> subject,
        TimeSpan timeout)
    {
        var emission = new TaskCompletionSource<IEnumerable<ObservedDocument>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = subject.Subscribe(documents => emission.TrySetResult(documents));
        return await emission.Task.WaitAsync(timeout);
    }

    IChangeStreamCursor<ChangeStreamDocument<ObservedDocument>> CreateChangeStreamCursor()
    {
        var cursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<ObservedDocument>>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await Task.Delay(Timeout.Infinite, _changeStreamLifetime.Token);
            return false;
        });
        return cursor;
    }

    static IAsyncCursor<ObservedDocument> CreateCursor(IEnumerable<ObservedDocument> documents)
    {
        var list = documents.ToList();
        var cursor = Substitute.For<IAsyncCursor<ObservedDocument>>();
        cursor.Current.Returns(list);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        return cursor;
    }
}
