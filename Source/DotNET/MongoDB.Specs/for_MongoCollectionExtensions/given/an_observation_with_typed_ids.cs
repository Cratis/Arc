// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Threading.Channels;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.given;

public sealed class an_observation_with_typed_ids<TId> : IDisposable
{
    const string DatabaseName = "testdb";
    const string CollectionName = "document";

    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(5);
    readonly IMongoCollection<Document> _collection = Substitute.For<IMongoCollection<Document>>();
    readonly Subject<ChangeStreamDocument<BsonDocument>> _changes = new();
    readonly Channel<Emission> _emissions = Channel.CreateUnbounded<Emission>();
    readonly Document _document;
    readonly bool _initiallyPresent;
    IDisposable _subscription;

    public an_observation_with_typed_ids(TId id, bool initiallyPresent = true)
    {
        _document = new(id, "Before");
        _initiallyPresent = initiallyPresent;
        _collection.Settings.Returns(new MongoCollectionSettings());
        _collection.DocumentSerializer.Returns(_ => BsonSerializer.LookupSerializer<Document>());
        _collection.CountDocumentsAsync(Arg.Any<FilterDefinition<Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(initiallyPresent ? 1L : 0L));
        _collection.FindAsync(Arg.Any<FilterDefinition<Document>>(), Arg.Any<FindOptions<Document, Document>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CreateCursor<Document>(_initiallyPresent ? [_document] : [])));
        _collection.FindAsync(Arg.Any<FilterDefinition<Document>>(), Arg.Any<FindOptions<Document, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CreateCursor<BsonDocument>([new BsonDocument()])));
        _collection.CollectionNamespace.Returns(new CollectionNamespace(DatabaseName, CollectionName));
        var database = Substitute.For<IMongoDatabase>();
        database.DatabaseNamespace.Returns(new DatabaseNamespace(DatabaseName));
        _collection.Database.Returns(database);

        // The observation reads the shared database stream through the watcher, so serve it this one.
        var watcher = Substitute.For<IMongoDBWatcher>();
        watcher.GetChanges(DatabaseName).Returns(_changes);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Internals.ServiceProvider.GetRequiredService<IQueryContextManager>());
        services.AddSingleton(watcher);
        Internals.ServiceProvider = services.BuildServiceProvider();
    }

    public async Task Start()
    {
        var subject = _collection.Observe();
        _subscription = subject.Subscribe(documents => _emissions.Writer.TryWrite(new(
            [.. documents],
            ((IHaveKnownChanges)documents).Changes)));
        await Next();
    }

    public async Task<Emission> Change(string operation)
    {
        var document = (_document with { Name = "After" }).ToBsonDocument();
        var change = new BsonDocument
        {
            { "operationType", operation },
            { "ns", new BsonDocument { { "db", DatabaseName }, { "coll", CollectionName } } },
            { "documentKey", new BsonDocument("_id", document["_id"]) }
        };
        if (operation != "delete")
        {
            change.Add("fullDocument", document);
        }
        _changes.OnNext(new(change, BsonSerializer.LookupSerializer<BsonDocument>()));
        return await Next();
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _changes.OnCompleted();
        _changes.Dispose();
    }

    static IAsyncCursor<T> CreateCursor<T>(IEnumerable<T> documents)
    {
        var cursor = Substitute.For<IAsyncCursor<T>>();
        cursor.Current.Returns(documents.ToArray());
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        return cursor;
    }

    async Task<Emission> Next() => await _emissions.Reader.ReadAsync().AsTask().WaitAsync(_deadline);

    public record Document(TId Id, string Name);

    public record Emission(IReadOnlyList<Document> Documents, IReadOnlyList<CollectionChange>? Changes);
}
