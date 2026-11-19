// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Search;

namespace Cratis.Arc.MongoDB.Resilience;

/// <summary>
/// Represents an <see cref="IMongoCollection{T}"/> that limits concurrent calls and treats a missing collection as an empty one.
/// </summary>
/// <typeparam name="T">Type of document.</typeparam>
/// <param name="inner">The collection to decorate.</param>
/// <param name="maxConcurrentCallsPerMethod">How many calls to one method may be in flight at once.</param>
/// <remarks>
/// Every asynchronous operation waits for one of a limited number of slots for its method before it reaches the driver,
/// and fails with a <see cref="TimeoutException"/> when none frees up within a second. An operation whose token is
/// already cancelled never reaches the driver. Some MongoDB-compatible servers, such as the Azure Cosmos DB MongoDB API,
/// fail an operation on a collection that does not exist yet instead of treating it as empty; such an operation completes
/// with the empty result instead: an empty cursor, a default value, or a change stream that starts once the collection
/// exists. Synchronous operations are passed straight through. A slot is a semaphore without a wait handle, so it holds
/// nothing to dispose and lives as long as the collection.
/// This is a plain decorator rather than a generated proxy, so the first use of a document type defines no new type and
/// takes no lock, and a call costs no reflection.
/// </remarks>
public class ResilientMongoCollection<T>(IMongoCollection<T> inner, int maxConcurrentCallsPerMethod) : IMongoCollection<T>
{
    const int MethodCount = 46;
    static readonly TimeSpan _acquireTimeout = TimeSpan.FromSeconds(1);
    static readonly TimeSpan _changeStreamRetryInterval = TimeSpan.FromSeconds(1);
    readonly IMongoCollection<T> _inner = inner;
    readonly SemaphoreSlim?[] _slots = new SemaphoreSlim?[MethodCount];

#pragma warning disable CS0618, CS8625, SA1600, SA1127
    public CollectionNamespace CollectionNamespace => _inner.CollectionNamespace;
    public IMongoDatabase Database => _inner.Database;
    public IBsonSerializer<T> DocumentSerializer => _inner.DocumentSerializer;
    public IMongoIndexManager<T> Indexes => _inner.Indexes;
    public MongoCollectionSettings Settings => _inner.Settings;
    public IMongoSearchIndexManager SearchIndexes => _inner.SearchIndexes;
    public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.Aggregate(pipeline, options, cancellationToken);
    public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.Aggregate(session, pipeline, options, cancellationToken);
    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => Run(0, () => _inner.AggregateAsync(pipeline, options, cancellationToken), static () => new EmptyAsyncCursor<TResult>(), cancellationToken);
    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => Run(1, () => _inner.AggregateAsync(session, pipeline, options, cancellationToken), static () => new EmptyAsyncCursor<TResult>(), cancellationToken);
    public void AggregateToCollection<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.AggregateToCollection(pipeline, options, cancellationToken);
    public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _inner.AggregateToCollection(session, pipeline, options, cancellationToken);
    public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => Run(2, () => _inner.AggregateToCollectionAsync(pipeline, options, cancellationToken), cancellationToken);
    public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => Run(3, () => _inner.AggregateToCollectionAsync(session, pipeline, options, cancellationToken), cancellationToken);
    public BulkWriteResult<T> BulkWrite(IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default) => _inner.BulkWrite(requests, options, cancellationToken);
    public BulkWriteResult<T> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default) => _inner.BulkWrite(session, requests, options, cancellationToken);
    public Task<BulkWriteResult<T>> BulkWriteAsync(IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default) => Run(4, () => _inner.BulkWriteAsync(requests, options, cancellationToken), cancellationToken);
    public Task<BulkWriteResult<T>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default) => Run(5, () => _inner.BulkWriteAsync(session, requests, options, cancellationToken), cancellationToken);
    public long Count(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => _inner.Count(filter, options, cancellationToken);
    public long Count(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => _inner.Count(session, filter, options, cancellationToken);
    public Task<long> CountAsync(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => Run(6, () => _inner.CountAsync(filter, options, cancellationToken), cancellationToken);
    public Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => Run(7, () => _inner.CountAsync(session, filter, options, cancellationToken), cancellationToken);
    public long CountDocuments(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => _inner.CountDocuments(filter, options, cancellationToken);
    public long CountDocuments(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => _inner.CountDocuments(session, filter, options, cancellationToken);
    public Task<long> CountDocumentsAsync(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => Run(8, () => _inner.CountDocumentsAsync(filter, options, cancellationToken), cancellationToken);
    public Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => Run(9, () => _inner.CountDocumentsAsync(session, filter, options, cancellationToken), cancellationToken);
    public DeleteResult DeleteMany(FilterDefinition<T> filter, CancellationToken cancellationToken = default) => _inner.DeleteMany(filter, cancellationToken);
    public DeleteResult DeleteMany(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default) => _inner.DeleteMany(filter, options, cancellationToken);
    public DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) => _inner.DeleteMany(session, filter, options, cancellationToken);
    public Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default) => Run(10, () => _inner.DeleteManyAsync(filter, cancellationToken), cancellationToken);
    public Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default) => Run(11, () => _inner.DeleteManyAsync(filter, options, cancellationToken), cancellationToken);
    public Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) => Run(12, () => _inner.DeleteManyAsync(session, filter, options, cancellationToken), cancellationToken);
    public DeleteResult DeleteOne(FilterDefinition<T> filter, CancellationToken cancellationToken = default) => _inner.DeleteOne(filter, cancellationToken);
    public DeleteResult DeleteOne(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default) => _inner.DeleteOne(filter, options, cancellationToken);
    public DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) => _inner.DeleteOne(session, filter, options, cancellationToken);
    public Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default) => Run(13, () => _inner.DeleteOneAsync(filter, cancellationToken), cancellationToken);
    public Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default) => Run(14, () => _inner.DeleteOneAsync(filter, options, cancellationToken), cancellationToken);
    public Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) => Run(15, () => _inner.DeleteOneAsync(session, filter, options, cancellationToken), cancellationToken);
    public IAsyncCursor<TField> Distinct<TField>(FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => _inner.Distinct(field, filter, options, cancellationToken);
    public IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => _inner.Distinct(session, field, filter, options, cancellationToken);
    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => Run(16, () => _inner.DistinctAsync(field, filter, options, cancellationToken), static () => new EmptyAsyncCursor<TField>(), cancellationToken);
    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => Run(17, () => _inner.DistinctAsync(session, field, filter, options, cancellationToken), static () => new EmptyAsyncCursor<TField>(), cancellationToken);
    public IAsyncCursor<TItem> DistinctMany<TItem>(FieldDefinition<T, IEnumerable<TItem>> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => _inner.DistinctMany(field, filter, options, cancellationToken);
    public IAsyncCursor<TItem> DistinctMany<TItem>(IClientSessionHandle session, FieldDefinition<T, IEnumerable<TItem>> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => _inner.DistinctMany(session, field, filter, options, cancellationToken);
    public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(FieldDefinition<T, IEnumerable<TItem>> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => Run(18, () => _inner.DistinctManyAsync(field, filter, options, cancellationToken), static () => new EmptyAsyncCursor<TItem>(), cancellationToken);
    public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(IClientSessionHandle session, FieldDefinition<T, IEnumerable<TItem>> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => Run(19, () => _inner.DistinctManyAsync(session, field, filter, options, cancellationToken), static () => new EmptyAsyncCursor<TItem>(), cancellationToken);
    public long EstimatedDocumentCount(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = default) => _inner.EstimatedDocumentCount(options, cancellationToken);
    public Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = default) => Run(20, () => _inner.EstimatedDocumentCountAsync(options, cancellationToken), cancellationToken);
    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => Run(21, () => _inner.FindAsync(filter, options, cancellationToken), static () => new EmptyAsyncCursor<TProjection>(), cancellationToken);
    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => Run(22, () => _inner.FindAsync(session, filter, options, cancellationToken), static () => new EmptyAsyncCursor<TProjection>(), cancellationToken);
    public TProjection FindOneAndDelete<TProjection>(FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => _inner.FindOneAndDelete(filter, options, cancellationToken);
    public TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => _inner.FindOneAndDelete(session, filter, options, cancellationToken);
    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => Run(23, () => _inner.FindOneAndDeleteAsync(filter, options, cancellationToken), cancellationToken);
    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => Run(24, () => _inner.FindOneAndDeleteAsync(session, filter, options, cancellationToken), cancellationToken);
    public TProjection FindOneAndReplace<TProjection>(FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => _inner.FindOneAndReplace(filter, replacement, options, cancellationToken);
    public TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => _inner.FindOneAndReplace(session, filter, replacement, options, cancellationToken);
    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => Run(25, () => _inner.FindOneAndReplaceAsync(filter, replacement, options, cancellationToken), cancellationToken);
    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => Run(26, () => _inner.FindOneAndReplaceAsync(session, filter, replacement, options, cancellationToken), cancellationToken);
    public TProjection FindOneAndUpdate<TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => _inner.FindOneAndUpdate(filter, update, options, cancellationToken);
    public TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => _inner.FindOneAndUpdate(session, filter, update, options, cancellationToken);
    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => Run(27, () => _inner.FindOneAndUpdateAsync(filter, update, options, cancellationToken), cancellationToken);
    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => Run(28, () => _inner.FindOneAndUpdateAsync(session, filter, update, options, cancellationToken), cancellationToken);
    public IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => _inner.FindSync(filter, options, cancellationToken);
    public IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => _inner.FindSync(session, filter, options, cancellationToken);
    public void InsertMany(IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default) => _inner.InsertMany(documents, options, cancellationToken);
    public void InsertMany(IClientSessionHandle session, IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default) => _inner.InsertMany(session, documents, options, cancellationToken);
    public Task InsertManyAsync(IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default) => Run(29, () => _inner.InsertManyAsync(documents, options, cancellationToken), cancellationToken);
    public Task InsertManyAsync(IClientSessionHandle session, IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default) => Run(30, () => _inner.InsertManyAsync(session, documents, options, cancellationToken), cancellationToken);
    public void InsertOne(T document, InsertOneOptions options = null, CancellationToken cancellationToken = default) => _inner.InsertOne(document, options, cancellationToken);
    public void InsertOne(IClientSessionHandle session, T document, InsertOneOptions options = null, CancellationToken cancellationToken = default) => _inner.InsertOne(session, document, options, cancellationToken);
    public Task InsertOneAsync(T document, CancellationToken _cancellationToken) => Run(31, () => _inner.InsertOneAsync(document, _cancellationToken), _cancellationToken);
    public Task InsertOneAsync(T document, InsertOneOptions options = null, CancellationToken cancellationToken = default) => Run(32, () => _inner.InsertOneAsync(document, options, cancellationToken), cancellationToken);
    public Task InsertOneAsync(IClientSessionHandle session, T document, InsertOneOptions options = null, CancellationToken cancellationToken = default) => Run(33, () => _inner.InsertOneAsync(session, document, options, cancellationToken), cancellationToken);
    public IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = default) => _inner.MapReduce(map, reduce, options, cancellationToken);
    public IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = default) => _inner.MapReduce(session, map, reduce, options, cancellationToken);
    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = default) => Run(34, () => _inner.MapReduceAsync(map, reduce, options, cancellationToken), static () => new EmptyAsyncCursor<TResult>(), cancellationToken);
    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = default) => Run(35, () => _inner.MapReduceAsync(session, map, reduce, options, cancellationToken), static () => new EmptyAsyncCursor<TResult>(), cancellationToken);
    public IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : T => _inner.OfType<TDerivedDocument>();
    public ReplaceOneResult ReplaceOne(FilterDefinition<T> filter, T replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default) => _inner.ReplaceOne(filter, replacement, options, cancellationToken);
    public ReplaceOneResult ReplaceOne(FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken = default) => _inner.ReplaceOne(filter, replacement, options, cancellationToken);
    public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default) => _inner.ReplaceOne(session, filter, replacement, options, cancellationToken);
    public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken = default) => _inner.ReplaceOne(session, filter, replacement, options, cancellationToken);
    public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<T> filter, T replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default) => Run(36, () => _inner.ReplaceOneAsync(filter, replacement, options, cancellationToken), cancellationToken);
    public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken = default) => Run(37, () => _inner.ReplaceOneAsync(filter, replacement, options, cancellationToken), cancellationToken);
    public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default) => Run(38, () => _inner.ReplaceOneAsync(session, filter, replacement, options, cancellationToken), cancellationToken);
    public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken = default) => Run(39, () => _inner.ReplaceOneAsync(session, filter, replacement, options, cancellationToken), cancellationToken);
    public UpdateResult UpdateMany(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default) => _inner.UpdateMany(filter, update, options, cancellationToken);
    public UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default) => _inner.UpdateMany(session, filter, update, options, cancellationToken);
    public Task<UpdateResult> UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default) => Run(40, () => _inner.UpdateManyAsync(filter, update, options, cancellationToken), cancellationToken);
    public Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default) => Run(41, () => _inner.UpdateManyAsync(session, filter, update, options, cancellationToken), cancellationToken);
    public UpdateResult UpdateOne(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default) => _inner.UpdateOne(filter, update, options, cancellationToken);
    public UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default) => _inner.UpdateOne(session, filter, update, options, cancellationToken);
    public Task<UpdateResult> UpdateOneAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default) => Run(42, () => _inner.UpdateOneAsync(filter, update, options, cancellationToken), cancellationToken);
    public Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default) => Run(43, () => _inner.UpdateOneAsync(session, filter, update, options, cancellationToken), cancellationToken);
    public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _inner.Watch(pipeline, options, cancellationToken);
    public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _inner.Watch(session, pipeline, options, cancellationToken);
    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => RunChangeStream(44, () => _inner.WatchAsync(pipeline, options, cancellationToken), cancellationToken);
    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => RunChangeStream(45, () => _inner.WatchAsync(session, pipeline, options, cancellationToken), cancellationToken);
    public IMongoCollection<T> WithReadConcern(ReadConcern readConcern) => _inner.WithReadConcern(readConcern);
    public IMongoCollection<T> WithReadPreference(ReadPreference readPreference) => _inner.WithReadPreference(readPreference);
    public IMongoCollection<T> WithWriteConcern(WriteConcern writeConcern) => _inner.WithWriteConcern(writeConcern);
#pragma warning restore CS0618, CS8625, SA1600, SA1127

    static bool IsCollectionNotFound(MongoCommandException exception) =>
        exception.Message.Contains(WellKnownErrorMessages.CollectionNotFound, StringComparison.OrdinalIgnoreCase);

    async Task Run(int method, Func<Task> operation, CancellationToken cancellationToken)
    {
        var slot = await Acquire(method, cancellationToken).ConfigureAwait(false);
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (IsCollectionNotFound(ex))
        {
        }
        finally
        {
            slot.Release();
        }
    }

    Task<TResult> Run<TResult>(int method, Func<Task<TResult>> operation, CancellationToken cancellationToken) =>
        Run(method, operation, static () => default!, cancellationToken);

    async Task<TResult> Run<TResult>(int method, Func<Task<TResult>> operation, Func<TResult> whenCollectionNotFound, CancellationToken cancellationToken)
    {
        var slot = await Acquire(method, cancellationToken).ConfigureAwait(false);
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (IsCollectionNotFound(ex))
        {
            return whenCollectionNotFound();
        }
        finally
        {
            slot.Release();
        }
    }

    Task<IChangeStreamCursor<TResult>> RunChangeStream<TResult>(int method, Func<Task<IChangeStreamCursor<TResult>>> operation, CancellationToken cancellationToken) =>
        Run(method, operation, () => new RetryingChangeStreamCursor<TResult>(operation, _changeStreamRetryInterval), cancellationToken);

    async Task<SemaphoreSlim> Acquire(int method, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var slot = SlotFor(method);
        if (!await slot.WaitAsync(_acquireTimeout, cancellationToken).ConfigureAwait(false))
        {
            throw new TimeoutException("Failed to acquire semaphore.");
        }
        return slot;
    }

    SemaphoreSlim SlotFor(int method) =>
        LazyInitializer.EnsureInitialized(ref _slots[method], () => new SemaphoreSlim(maxConcurrentCallsPerMethod, maxConcurrentCallsPerMethod));
}
