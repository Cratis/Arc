// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience;

/// <summary>
/// Represents an <see cref="IMongoDatabase"/> whose collections are <see cref="ResilientMongoCollection{T}"/>.
/// </summary>
/// <param name="inner">The database to decorate.</param>
/// <param name="maxConcurrentCallsPerMethod">How many calls to one collection method may be in flight at once.</param>
/// <remarks>
/// Everything but getting a collection, and the databases derived through the With methods, is passed straight
/// through. Decorating the collection here, where its document type is known, needs no runtime type construction.
/// </remarks>
public class ResilientMongoDatabase(IMongoDatabase inner, int maxConcurrentCallsPerMethod) : IMongoDatabase
{
    readonly IMongoDatabase _inner = inner;

#pragma warning disable CS8625, SA1600
    public IMongoClient Client => _inner.Client;
    public DatabaseNamespace DatabaseNamespace => _inner.DatabaseNamespace;
    public MongoDatabaseSettings Settings => _inner.Settings;
    public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.Aggregate<TResult>(pipeline, options, cancellationToken);
    public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.Aggregate<TResult>(session, pipeline, options, cancellationToken);
    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.AggregateAsync<TResult>(pipeline, options, cancellationToken);
    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.AggregateAsync<TResult>(session, pipeline, options, cancellationToken);
    public void AggregateToCollection<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.AggregateToCollection<TResult>(pipeline, options, cancellationToken);
    public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.AggregateToCollection<TResult>(session, pipeline, options, cancellationToken);
    public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.AggregateToCollectionAsync<TResult>(pipeline, options, cancellationToken);
    public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.AggregateToCollectionAsync<TResult>(session, pipeline, options, cancellationToken);
    public void CreateCollection(string name, CreateCollectionOptions options = null, CancellationToken cancellationToken = default) => _inner.CreateCollection(name, options, cancellationToken);
    public void CreateCollection(IClientSessionHandle session, string name, CreateCollectionOptions options = null, CancellationToken cancellationToken = default) => _inner.CreateCollection(session, name, options, cancellationToken);
    public Task CreateCollectionAsync(string name, CreateCollectionOptions options = null, CancellationToken cancellationToken = default) => _inner.CreateCollectionAsync(name, options, cancellationToken);
    public Task CreateCollectionAsync(IClientSessionHandle session, string name, CreateCollectionOptions options = null, CancellationToken cancellationToken = default) => _inner.CreateCollectionAsync(session, name, options, cancellationToken);
    public void CreateView<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default) => _inner.CreateView<TDocument, TResult>(viewName, viewOn, pipeline, options, cancellationToken);
    public void CreateView<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default) => _inner.CreateView<TDocument, TResult>(session, viewName, viewOn, pipeline, options, cancellationToken);
    public Task CreateViewAsync<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default) => _inner.CreateViewAsync<TDocument, TResult>(viewName, viewOn, pipeline, options, cancellationToken);
    public Task CreateViewAsync<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default) => _inner.CreateViewAsync<TDocument, TResult>(session, viewName, viewOn, pipeline, options, cancellationToken);
    public void DropCollection(string name, CancellationToken cancellationToken = default) => _inner.DropCollection(name, cancellationToken);
    public void DropCollection(string name, DropCollectionOptions options, CancellationToken cancellationToken = default) => _inner.DropCollection(name, options, cancellationToken);
    public void DropCollection(IClientSessionHandle session, string name, CancellationToken cancellationToken = default) => _inner.DropCollection(session, name, cancellationToken);
    public void DropCollection(IClientSessionHandle session, string name, DropCollectionOptions options, CancellationToken cancellationToken = default) => _inner.DropCollection(session, name, options, cancellationToken);
    public Task DropCollectionAsync(string name, CancellationToken cancellationToken = default) => _inner.DropCollectionAsync(name, cancellationToken);
    public Task DropCollectionAsync(string name, DropCollectionOptions options, CancellationToken cancellationToken = default) => _inner.DropCollectionAsync(name, options, cancellationToken);
    public Task DropCollectionAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default) => _inner.DropCollectionAsync(session, name, cancellationToken);
    public Task DropCollectionAsync(IClientSessionHandle session, string name, DropCollectionOptions options, CancellationToken cancellationToken = default) => _inner.DropCollectionAsync(session, name, options, cancellationToken);
    public IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings = null) => new ResilientMongoCollection<TDocument>(_inner.GetCollection<TDocument>(name, settings), maxConcurrentCallsPerMethod);
    public IAsyncCursor<string> ListCollectionNames(ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default) => _inner.ListCollectionNames(options, cancellationToken);
    public IAsyncCursor<string> ListCollectionNames(IClientSessionHandle session, ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default) => _inner.ListCollectionNames(session, options, cancellationToken);
    public Task<IAsyncCursor<string>> ListCollectionNamesAsync(ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default) => _inner.ListCollectionNamesAsync(options, cancellationToken);
    public Task<IAsyncCursor<string>> ListCollectionNamesAsync(IClientSessionHandle session, ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default) => _inner.ListCollectionNamesAsync(session, options, cancellationToken);
    public IAsyncCursor<BsonDocument> ListCollections(ListCollectionsOptions options = null, CancellationToken cancellationToken = default) => _inner.ListCollections(options, cancellationToken);
    public IAsyncCursor<BsonDocument> ListCollections(IClientSessionHandle session, ListCollectionsOptions options = null, CancellationToken cancellationToken = default) => _inner.ListCollections(session, options, cancellationToken);
    public Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(ListCollectionsOptions options = null, CancellationToken cancellationToken = default) => _inner.ListCollectionsAsync(options, cancellationToken);
    public Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(IClientSessionHandle session, ListCollectionsOptions options = null, CancellationToken cancellationToken = default) => _inner.ListCollectionsAsync(session, options, cancellationToken);
    public void RenameCollection(string oldName, string newName, RenameCollectionOptions options = null, CancellationToken cancellationToken = default) => _inner.RenameCollection(oldName, newName, options, cancellationToken);
    public void RenameCollection(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions options = null, CancellationToken cancellationToken = default) => _inner.RenameCollection(session, oldName, newName, options, cancellationToken);
    public Task RenameCollectionAsync(string oldName, string newName, RenameCollectionOptions options = null, CancellationToken cancellationToken = default) => _inner.RenameCollectionAsync(oldName, newName, options, cancellationToken);
    public Task RenameCollectionAsync(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions options = null, CancellationToken cancellationToken = default) => _inner.RenameCollectionAsync(session, oldName, newName, options, cancellationToken);
    public TResult RunCommand<TResult>(Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default) => _inner.RunCommand<TResult>(command, readPreference, cancellationToken);
    public TResult RunCommand<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default) => _inner.RunCommand<TResult>(session, command, readPreference, cancellationToken);
    public Task<TResult> RunCommandAsync<TResult>(Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default) => _inner.RunCommandAsync<TResult>(command, readPreference, cancellationToken);
    public Task<TResult> RunCommandAsync<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default) => _inner.RunCommandAsync<TResult>(session, command, readPreference, cancellationToken);
    public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _inner.Watch<TResult>(pipeline, options, cancellationToken);
    public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _inner.Watch<TResult>(session, pipeline, options, cancellationToken);
    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _inner.WatchAsync<TResult>(pipeline, options, cancellationToken);
    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _inner.WatchAsync<TResult>(session, pipeline, options, cancellationToken);
    public IMongoDatabase WithReadConcern(ReadConcern readConcern) => new ResilientMongoDatabase(_inner.WithReadConcern(readConcern), maxConcurrentCallsPerMethod);
    public IMongoDatabase WithReadPreference(ReadPreference readPreference) => new ResilientMongoDatabase(_inner.WithReadPreference(readPreference), maxConcurrentCallsPerMethod);
    public IMongoDatabase WithWriteConcern(WriteConcern writeConcern) => new ResilientMongoDatabase(_inner.WithWriteConcern(writeConcern), maxConcurrentCallsPerMethod);
#pragma warning restore CS8625, SA1600
}
