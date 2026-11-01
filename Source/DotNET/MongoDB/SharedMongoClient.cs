// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Forwards native client operations without transferring ownership of the factory's shared root client.
/// </summary>
/// <param name="client">The process-shared native client.</param>
internal sealed class SharedMongoClient(IMongoClient client) : IMongoClient
{
    /// <inheritdoc/>
    public ICluster Cluster => client.Cluster;

    /// <inheritdoc/>
    public MongoClientSettings Settings => client.Settings;

    /// <inheritdoc/>
    public ClientBulkWriteResult BulkWrite(IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions? options = null, CancellationToken cancellationToken = default) => client.BulkWrite(models, options, cancellationToken);

    /// <inheritdoc/>
    public ClientBulkWriteResult BulkWrite(IClientSessionHandle session, IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions? options = null, CancellationToken cancellationToken = default) => client.BulkWrite(session, models, options, cancellationToken);

    /// <inheritdoc/>
    public Task<ClientBulkWriteResult> BulkWriteAsync(IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions? options = null, CancellationToken cancellationToken = default) => client.BulkWriteAsync(models, options, cancellationToken);

    /// <inheritdoc/>
    public Task<ClientBulkWriteResult> BulkWriteAsync(IClientSessionHandle session, IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions? options = null, CancellationToken cancellationToken = default) => client.BulkWriteAsync(session, models, options, cancellationToken);

    /// <inheritdoc/>
    public void DropDatabase(string name, CancellationToken cancellationToken = default) => client.DropDatabase(name, cancellationToken);

    /// <inheritdoc/>
    public void DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken = default) => client.DropDatabase(session, name, cancellationToken);

    /// <inheritdoc/>
    public Task DropDatabaseAsync(string name, CancellationToken cancellationToken = default) => client.DropDatabaseAsync(name, cancellationToken);

    /// <inheritdoc/>
    public Task DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default) => client.DropDatabaseAsync(session, name, cancellationToken);

    /// <inheritdoc/>
    public IMongoDatabase GetDatabase(string name, MongoDatabaseSettings? settings = null) => client.GetDatabase(name, settings);

    /// <inheritdoc/>
    public IAsyncCursor<string> ListDatabaseNames(CancellationToken cancellationToken = default) => client.ListDatabaseNames(cancellationToken);

    /// <inheritdoc/>
    public IAsyncCursor<string> ListDatabaseNames(ListDatabaseNamesOptions options, CancellationToken cancellationToken = default) => client.ListDatabaseNames(options, cancellationToken);

    /// <inheritdoc/>
    public IAsyncCursor<string> ListDatabaseNames(IClientSessionHandle session, CancellationToken cancellationToken = default) => client.ListDatabaseNames(session, cancellationToken);

    /// <inheritdoc/>
    public IAsyncCursor<string> ListDatabaseNames(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken = default) => client.ListDatabaseNames(session, options, cancellationToken);

    /// <inheritdoc/>
    public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(CancellationToken cancellationToken = default) => client.ListDatabaseNamesAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(ListDatabaseNamesOptions options, CancellationToken cancellationToken = default) => client.ListDatabaseNamesAsync(options, cancellationToken);

    /// <inheritdoc/>
    public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(IClientSessionHandle session, CancellationToken cancellationToken = default) => client.ListDatabaseNamesAsync(session, cancellationToken);

    /// <inheritdoc/>
    public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken = default) => client.ListDatabaseNamesAsync(session, options, cancellationToken);

    /// <inheritdoc/>
    public IAsyncCursor<BsonDocument> ListDatabases(CancellationToken cancellationToken = default) => client.ListDatabases(cancellationToken);

    /// <inheritdoc/>
    public IAsyncCursor<BsonDocument> ListDatabases(ListDatabasesOptions options, CancellationToken cancellationToken = default) => client.ListDatabases(options, cancellationToken);

    /// <inheritdoc/>
    public IAsyncCursor<BsonDocument> ListDatabases(IClientSessionHandle session, CancellationToken cancellationToken = default) => client.ListDatabases(session, cancellationToken);

    /// <inheritdoc/>
    public IAsyncCursor<BsonDocument> ListDatabases(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken = default) => client.ListDatabases(session, options, cancellationToken);

    /// <inheritdoc/>
    public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(CancellationToken cancellationToken = default) => client.ListDatabasesAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(ListDatabasesOptions options, CancellationToken cancellationToken = default) => client.ListDatabasesAsync(options, cancellationToken);

    /// <inheritdoc/>
    public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, CancellationToken cancellationToken = default) => client.ListDatabasesAsync(session, cancellationToken);

    /// <inheritdoc/>
    public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken = default) => client.ListDatabasesAsync(session, options, cancellationToken);

    /// <inheritdoc/>
    public IClientSessionHandle StartSession(ClientSessionOptions? options = null, CancellationToken cancellationToken = default) => client.StartSession(options, cancellationToken);

    /// <inheritdoc/>
    public Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions? options = null, CancellationToken cancellationToken = default) => client.StartSessionAsync(options, cancellationToken);

    /// <inheritdoc/>
    public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default) => client.Watch(pipeline, options, cancellationToken);

    /// <inheritdoc/>
    public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default) => client.Watch(session, pipeline, options, cancellationToken);

    /// <inheritdoc/>
    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default) => client.WatchAsync(pipeline, options, cancellationToken);

    /// <inheritdoc/>
    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default) => client.WatchAsync(session, pipeline, options, cancellationToken);

    /// <inheritdoc/>
    public IMongoClient WithReadConcern(ReadConcern readConcern) => client.WithReadConcern(readConcern);

    /// <inheritdoc/>
    public IMongoClient WithReadPreference(ReadPreference readPreference) => client.WithReadPreference(readPreference);

    /// <inheritdoc/>
    public IMongoClient WithWriteConcern(WriteConcern writeConcern) => client.WithWriteConcern(writeConcern);

    /// <inheritdoc/>
    public void Dispose()
    {
        // The factory's cached client is shared across the process, not owned by a request scope.
    }
}
