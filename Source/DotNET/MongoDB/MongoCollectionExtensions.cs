// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Cratis.Arc.Queries;
using Cratis.Concepts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver;

/// <summary>
/// Extension methods for <see cref="IMongoCollection{TDocument}"/>.
/// </summary>
public static class MongoCollectionExtensions
{
    /// <summary>
    /// Find a single document based on Id.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="id">Id of document.</param>
    /// <typeparam name="T">Type of document.</typeparam>
    /// <typeparam name="TId">Type of identifier.</typeparam>
    /// <returns>The document if found, default if not.</returns>
    public static T? FindById<T, TId>(this IMongoCollection<T> collection, TId id) =>
        collection.Find(Builders<T>.Filter.Eq(new StringFieldDefinition<T, TId>("_id"), id)).SingleOrDefault();

    /// <summary>
    /// Find a single document based on Id - asynchronous.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="id">Id of document.</param>
    /// <typeparam name="T">Type of document.</typeparam>
    /// <typeparam name="TId">Type of identifier.</typeparam>
    /// <returns>The document if found, default if not.</returns>
    public static async Task<T?> FindByIdAsync<T, TId>(this IMongoCollection<T> collection, TId id)
    {
        var result = await collection.FindAsync(Builders<T>.Filter.Eq(new StringFieldDefinition<T, TId>("_id"), id));
        return await result.SingleOrDefaultAsync();
    }

    /// <summary>Observe matching documents using the current query context.</summary>
    /// <param name="collection">The collection to observe.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional find options.</param>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <returns>The observable documents.</returns>
    public static ISubject<IEnumerable<TDocument>> Observe<TDocument>(
        this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>>? filter,
        FindOptions? options = null) => collection.Observe(filter, false, options);

    /// <summary>Observe a single matching document without query-context paging, sorting, or counting.</summary>
    /// <param name="collection">The collection to observe.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional find options.</param>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <returns>The observable document.</returns>
    public static ISubject<TDocument> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>>? filter,
        FindOptions? options = null) => collection.ObserveSingle(filter, false, options);

    /// <summary>Observe matching documents using the current query context.</summary>
    /// <param name="collection">The collection to observe.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional find options.</param>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <returns>The observable documents.</returns>
    public static ISubject<IEnumerable<TDocument>> Observe<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument>? filter = null,
        FindOptions? options = null) => collection.Observe(filter, false, options);

    /// <summary>Observe a single matching document without query-context paging, sorting, or counting.</summary>
    /// <param name="collection">The collection to observe.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional find options.</param>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <returns>The observable document.</returns>
    public static ISubject<TDocument> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument>? filter = null,
        FindOptions? options = null) => collection.ObserveSingle(filter, false, options);

    /// <summary>Observe a document by id without query-context paging, sorting, or counting.</summary>
    /// <param name="collection">The collection to observe.</param>
    /// <param name="id">The document id.</param>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <typeparam name="TId">The id type.</typeparam>
    /// <returns>The observable document.</returns>
    public static ISubject<TDocument> ObserveById<TDocument, TId>(this IMongoCollection<TDocument> collection, TId id) =>
        collection.ObserveById(id, false);

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="ignoreQueryContext">Whether to read the full filtered source without client paging, sorting, or total count updates.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns><see cref="ISubject{T}"/> with a collection of the type for the collection.</returns>
    public static ISubject<IEnumerable<TDocument>> Observe<TDocument>(
        this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>>? filter,
        bool ignoreQueryContext,
        FindOptions? options = null)
    {
        filter ??= _ => true;
        return collection.Observe<TDocument, IEnumerable<TDocument>>(
            () => collection.Find(filter, options),
            filter,
            ignoreQueryContext,

            // The emitted snapshot carries the changes it represents, so the delta downstream is the one the change
            // stream already stated rather than one rediscovered by comparing every item against the previous
            // snapshot. It is still an IEnumerable<TDocument> to every subscriber that does not look.
            (cursor, changes, observable) => observable.OnNext(new ObservedCollection<TDocument>([.. cursor], changes)));
    }

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="ignoreQueryContext">Retained for compatibility; has no effect for single-result observations, which always read the full filtered source without client paging, sorting, or total count updates.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>
    /// An <see cref="ISubject{T}"/> with a single instance of the type; emits <see langword="default"/> when
    /// no document matches — after the observed document is deleted, after an update or replace moves it out
    /// of the filter, or when the initial query finds none.
    /// </returns>
    public static ISubject<TDocument> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>>? filter,
        bool ignoreQueryContext,
        FindOptions? options = null)
    {
        // Single-result observations never apply query paging.
        _ = ignoreQueryContext;
        filter ??= _ => true;
        return collection.ObserveSingle(() => collection.Find(filter, options), filter);
    }

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filterDefinition">The MongoDB filter.</param>
    /// <param name="ignoreQueryContext">Whether to read the full filtered source without client paging, sorting, or total count updates.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns><see cref="ISubject{T}"/> with a collection of the type for the collection.</returns>
    /// <example><c>collection.Observe(filterDefinition, ignoreQueryContext: true)</c>.</example>
    public static ISubject<IEnumerable<TDocument>> Observe<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument>? filterDefinition,
        bool ignoreQueryContext,
        FindOptions? options = null)
    {
        filterDefinition ??= FilterDefinition<TDocument>.Empty;
        return collection.Observe<TDocument, IEnumerable<TDocument>>(
            () => collection.Find(filterDefinition, options),
            filterDefinition,
            ignoreQueryContext,
            (documents, changes, observable) => observable.OnNext(new ObservedCollection<TDocument>([.. documents], changes)));
    }

    /// <summary>
    /// Observe matching documents with the query-context option before the MongoDB filter.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns><see cref="ISubject{T}"/> with a collection of the type for the collection.</returns>
    public static ISubject<IEnumerable<TDocument>> Observe<TDocument>(
        this IMongoCollection<TDocument> collection,
        bool ignoreQueryContext,
        FilterDefinition<TDocument>? filter = null,
        FindOptions? options = null) => collection.Observe(filter, ignoreQueryContext, options);

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filterDefinition">The MongoDB filter.</param>
    /// <param name="ignoreQueryContext">Retained for compatibility; has no effect for single-result observations, which always read the full filtered source without client paging, sorting, or total count updates.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>
    /// An <see cref="ISubject{T}"/> with a single instance of the type; emits <see langword="default"/> when
    /// no document matches — after the observed document is deleted, after an update or replace moves it out
    /// of the filter, or when the initial query finds none.
    /// </returns>
    /// <example><c>collection.ObserveSingle(filterDefinition, ignoreQueryContext: true)</c>.</example>
    public static ISubject<TDocument> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument>? filterDefinition,
        bool ignoreQueryContext,
        FindOptions? options = null)
    {
        // Single-result observations never apply query paging.
        _ = ignoreQueryContext;
        filterDefinition ??= FilterDefinition<TDocument>.Empty;
        return collection.ObserveSingle(() => collection.Find(filterDefinition, options), filterDefinition);
    }

    /// <summary>
    /// Observe a single matching document with the compatibility query-context option before the MongoDB filter.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="ignoreQueryContext">Retained for compatibility; has no effect for single-result observations, which always read the full filtered source without client paging, sorting, or total count updates.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>
    /// An <see cref="ISubject{T}"/> with a single instance of the type; emits <see langword="default"/> when
    /// no document matches — after the observed document is deleted, after an update or replace moves it out
    /// of the filter, or when the initial query finds none.
    /// </returns>
    public static ISubject<TDocument> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        bool ignoreQueryContext,
        FilterDefinition<TDocument>? filter = null,
        FindOptions? options = null) => collection.ObserveSingle(filter, ignoreQueryContext, options);

    /// <summary>
    /// Create an observable query that will observe a single document based on Id of the document in the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="id">The identifier of the document to observe.</param>
    /// <param name="ignoreQueryContext">Retained for compatibility; has no effect for single-result observations, which always read the full filtered source without client paging, sorting, or total count updates.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <typeparam name="TId">Type of id - key.</typeparam>
    /// <returns>
    /// An <see cref="ISubject{T}"/> with an instance of the type; emits <see langword="default"/> when no
    /// document matches — after the observed document is deleted, or when the initial query finds none.
    /// </returns>
    public static ISubject<TDocument> ObserveById<TDocument, TId>(this IMongoCollection<TDocument> collection, TId id, bool ignoreQueryContext)
    {
        // Single-result observations never apply query paging.
        _ = ignoreQueryContext;
        var filter = Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, TId>("_id"), id);
        return collection.ObserveSingle(() => collection.Find(filter), filter);
    }

    /// <summary>
    /// Create an observable query that will observe a single document in the collection, driven by an
    /// explicit initial query and filter.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="findCall">Produces the initial query for the observed document.</param>
    /// <param name="filter">The filter identifying the observed document.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>An <see cref="ISubject{T}"/> with a single instance of the type.</returns>
    /// <remarks>
    /// The single-document observable emits <see langword="default"/> when no document matches — after a hard
    /// delete, after an update or replace that moves the document out of the filter, and when the initial
    /// query finds nothing. <see langword="default"/> is deliberate: it matches the existing "no such
    /// document" convention (see <see cref="FindById{T,TId}"/>) and lets a subscriber tell "removed" apart
    /// from "still catching up" — completing the observable instead would close the connection out from
    /// under the client rather than report absence.
    /// </remarks>
    static ISubject<TDocument> ObserveSingle<TDocument>(
         this IMongoCollection<TDocument> collection,
         Func<IFindFluent<TDocument, TDocument>> findCall,
         FilterDefinition<TDocument> filter)
    {
        return collection.Observe<TDocument, TDocument>(
            findCall,
            filter,
            ignoreQueryContext: true,
            (documents, _, observable) => observable.OnNext(documents.FirstOrDefault()!));
    }

    static ISubject<TResult> Observe<TDocument, TResult>(
        this IMongoCollection<TDocument> collection,
        Func<IFindFluent<TDocument, TDocument>> findCall,
        FilterDefinition<TDocument> filter,
        bool ignoreQueryContext,
        Action<IEnumerable<TDocument>, IReadOnlyList<CollectionChange>?, ISubject<TResult>> onNext)
    {
        var completedCleanup = false;
        var cancelling = false;
        var cancellationGate = new object();
        var logger = Internals.ServiceProvider.GetRequiredService<ILogger<MongoCollection>>();
        var queryContextManager = Internals.ServiceProvider.GetRequiredService<IQueryContextManager>();
        var queryContext = queryContextManager.Current;
        var observationContext = ignoreQueryContext ? queryContext with { Paging = Paging.NotPaged, Sorting = Sorting.None } : queryContext;

        var classMap = BsonClassMap.LookupClassMap(typeof(TDocument));
        var idProperty = typeof(TDocument).GetProperty(classMap.IdMemberMap?.MemberName ?? "Id", BindingFlags.Instance | BindingFlags.Public) ?? throw new MissingIdMapping(typeof(TDocument));
        var documents = new QueryContextAwareSet<TDocument>(observationContext, idProperty);

        var filterRendered = filter.Render(new(collection.DocumentSerializer, collection.Settings.SerializerRegistry));
        var matchesEverything = filterRendered.ElementCount == 0;
        var watcher = Internals.ServiceProvider.GetRequiredService<IMongoDBWatcher>();
        var collectionNamespace = collection.CollectionNamespace;

    #pragma warning disable CA2000 // Dispose objects before losing scope
        var cancellationTokenSource = new CancellationTokenSource();

        // The initial query runs asynchronously in Watch() below, so the subject must not carry a value before it
        // completes. A subject seeded up front hands every subscriber — including a one-shot waitForFirstResult read —
        // an empty emission that predates the query and is indistinguishable from a genuinely empty result. Replaying
        // the single latest emission keeps a late subscriber from missing the initial query result instead.
        var subject = new LifetimeAwareSubject<TResult>(
            new ReplaySubject<TResult>(1),
            CancelWatch);
    #pragma warning restore CA2000 // Dispose objects before losing scope
        ISubject<TResult> observable = subject;

        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        _ = Task.Run(Watch);
        return observable;

        async Task Watch()
        {
            try
            {
                var query = findCall();
                query = AddSorting(observationContext, query);
                query = AddPaging(observationContext, query);

                // Every observation of the database shares one change stream, so this costs no connection of its own.
                // Subscribe before the initial query so a change made while it runs is held, not missed, and apply
                // changes on this task. What is held is collapsed while this task is busy (one change per document,
                // or one read again), so a slow query cannot make it grow with the write rate.
                var changes = new PendingChanges(observationContext.Paging.IsPaged);
                var databaseChanges = watcher.GetChanges(collection.Database);
                using var subscription = databaseChanges.Subscribe(
                    change =>
                    {
                        if (IsRelevantChange(change, collectionNamespace))
                        {
                            changes.Add(change, RequiresResynchronization(change, collectionNamespace));
                        }
                    },
                    changes.Complete,
                    () => changes.Complete());

                // Subscribing alone does not mean WatchAsync has returned a cursor. Writes before it opens
                // must be included in the snapshot, since the stream cannot deliver them afterward.
                if (databaseChanges is MongoDBDatabaseWatch databaseWatch)
                {
                    await databaseWatch.WhenOpen(cancellationToken);
                }

                if (!ignoreQueryContext)
                {
                    queryContext.TotalItems = (int)await findCall().CountDocumentsAsync(cancellationToken);
                }
                await documents.InitializeWithQuery(query);

                // The initial emission is a whole snapshot, not a delta - there is no previous state to state a
                // change against, so it carries no changes and downstream treats it as the baseline.
                onNext(documents, null, subject);
                while (await changes.WaitForChanges(cancellationToken))
                {
                    await DispatchChanges(changes.Take(), query);
                }

                logger.IteratingChangeStreamCursorCompleted();
            }
            catch (ObjectDisposedException)
            {
                logger.ObjectDisposed();
            }
            catch (OperationCanceledException)
            {
                logger.OperationCancelled();
            }
            catch (Exception ex)
            {
                logger.UnexpectedError(collection.CollectionNamespace.FullName, typeof(TDocument).FullName, ex);
                subject.OnError(ex);
            }
            finally
            {
                Cleanup();
            }
        }

        async Task DispatchChanges((bool ReadAgain, IReadOnlyList<ChangeStreamDocument<BsonDocument>> Changes) pending, IFindFluent<TDocument, TDocument> query)
        {
            // A failure here propagates out of the cursor loop and faults the observable in Watch(): a change
            // that could not be applied would otherwise be lost silently and leave the client on stale data.
            // A paged observation re-reads its page after any change. Any observation re-reads after a resynchronization,
            // since the shared stream was reopened, or a collection was dropped or renamed, and changes may be missing,
            // and after more documents changed than are held. One read again answers every change held.
            if (pending.ReadAgain)
            {
                await HandlePagedChange(
                    collection,
                    findCall,
                    observationContext,
                    ignoreQueryContext,
                    onNext,
                    query,
                    documents,
                    subject,
                    cancellationToken);
                return;
            }

            foreach (var changeDocument in pending.Changes)
            {
                await HandleChange(
                    collection,
                    filter,
                    matchesEverything,
                    observationContext,
                    onNext,
                    changeDocument,
                    documents,
                    subject,
                    idProperty,
                    classMap.IdMemberMap,
                    cancellationToken);
            }
        }

        void CancelWatch()
        {
            logger.ClientUnsubscribed();
            lock (cancellationGate)
            {
                if (completedCleanup)
                {
                    return;
                }
                cancelling = true;
            }

            // Cancel invokes driver callbacks synchronously. Do not hold the gate while they run:
            // a callback can itself allow Watch to finish, even on this same thread.
            try
            {
                cancellationTokenSource.Cancel();
            }
            finally
            {
                lock (cancellationGate)
                {
                    cancelling = false;
                    if (completedCleanup)
                    {
                        cancellationTokenSource.Dispose();
                    }
                }
            }
        }

        void Cleanup()
        {
            logger.CleaningUp();
            try
            {
                try
                {
                    subject.OnCompleted();
                }
                finally
                {
                    subject.Dispose();
                }
            }
            finally
            {
                lock (cancellationGate)
                {
                    // Stop the subject before releasing its cancellation resource. If its one-shot
                    // stop callback is still inside Cancel, that callback owns the final disposal.
                    completedCleanup = true;
                    if (!cancelling)
                    {
                        cancellationTokenSource.Dispose();
                    }
                }
            }
        }
    }

    static async Task HandleChange<TDocument, TResult>(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        bool matchesEverything,
        QueryContext queryContext,
        Action<IEnumerable<TDocument>, IReadOnlyList<CollectionChange>?, ISubject<TResult>> onNext,
        ChangeStreamDocument<BsonDocument> changeDocument,
        QueryContextAwareSet<TDocument> documents,
        ISubject<TResult> subject,
        PropertyInfo idProperty,
        BsonMemberMap? idMemberMap,
        CancellationToken cancellationToken)
    {
        var hasChanges = false;
        var changes = new List<CollectionChange>();
        if (changeDocument.DocumentKey is not null && changeDocument.DocumentKey.TryGetValue("_id", out var idValue))
        {
            var id = GetId(idProperty, idMemberMap, idValue);
            var fullDocument = changeDocument.FullDocument is null ? default : Deserialize(collection, changeDocument.FullDocument);
            if (changeDocument.OperationType == ChangeStreamOperationType.Delete)
            {
                // A delete cannot be narrowed by the filter - the document is gone - so it arrives for every
                // document in the collection. Only one the observer holds was ever counted. The unpaged set holds
                // every matching document, so membership in it is membership in the result.
                hasChanges = RemoveFromSet(documents, id, changes);
                if (hasChanges)
                {
                    queryContext.TotalItems--;
                }
            }
            else if (fullDocument is not null)
            {
                // An insert, an update or a replace. The shared change stream carries every change in the
                // database, so none of them has been narrowed by the observed filter, and an event saying a
                // document has left the result set looks exactly like one saying it never belonged. Ask the
                // collection which it is: one indexed lookup on the document's own key, skipped when the
                // observation takes every document.
                var belongs = matchesEverything || await collection
                    .Find(Builders<TDocument>.Filter.And(filter, Builders<TDocument>.Filter.Eq("_id", idValue)))
                    .AnyAsync(cancellationToken);

                var wasPresent = documents.Contains(id);
                if (belongs)
                {
                    if (!wasPresent)
                    {
                        queryContext.TotalItems++;
                    }
                    hasChanges = documents.Add(fullDocument);
                    if (hasChanges)
                    {
                        changes.Add(new(wasPresent ? CollectionChangeKind.Replaced : CollectionChangeKind.Added, id));
                    }
                }
                else if (wasPresent)
                {
                    queryContext.TotalItems--;
                    hasChanges = RemoveFromSet(documents, id, changes);
                }
            }
        }
        if (hasChanges)
        {
            onNext(documents, changes, subject);
        }
    }

    /// <summary>
    /// Re-reads a paged observation after any change and emits when the page or its total moved.
    /// </summary>
    /// <param name="collection">The observed collection.</param>
    /// <param name="findCall">Produces the unsorted, unpaged query the total is counted from.</param>
    /// <param name="queryContext">The <see cref="QueryContext"/> carrying the paging state and the total.</param>
    /// <param name="ignoreQueryContext">Whether the total is left alone.</param>
    /// <param name="onNext">Emits the page.</param>
    /// <param name="query">The sorted and paged query the page is read from.</param>
    /// <param name="documents">The observed page.</param>
    /// <param name="subject">The subject to emit on.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> ending the observation.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <typeparam name="TResult">Type of the emitted result.</typeparam>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// A paged set holds the current page only, so a single change event cannot say whether the document belongs
    /// before, on or after the page, whether it was counted, or where it now sorts. One count and one page read per
    /// batch of held changes answers all of it, the same trade the Entity Framework Core provider makes.
    /// </remarks>
    static async Task HandlePagedChange<TDocument, TResult>(
        IMongoCollection<TDocument> collection,
        Func<IFindFluent<TDocument, TDocument>> findCall,
        QueryContext queryContext,
        bool ignoreQueryContext,
        Action<IEnumerable<TDocument>, IReadOnlyList<CollectionChange>?, ISubject<TResult>> onNext,
        IFindFluent<TDocument, TDocument> query,
        QueryContextAwareSet<TDocument> documents,
        ISubject<TResult> subject,
        CancellationToken cancellationToken)
    {
        var totalItems = queryContext.TotalItems;
        if (!ignoreQueryContext)
        {
            totalItems = (int)await findCall().CountDocumentsAsync(cancellationToken);
        }

        var page = await query.ToListAsync(cancellationToken);
        var changes = documents.ReplaceWith(page, (previous, current) => HasSameContent(collection, previous, current));
        var totalChanged = totalItems != queryContext.TotalItems;
        queryContext.TotalItems = totalItems;
        if (changes is not null || totalChanged)
        {
            onNext(documents, changes ?? [], subject);
        }
    }

    static bool HasSameContent<TDocument>(IMongoCollection<TDocument> collection, TDocument previous, TDocument current) =>
        previous.ToBsonDocument(collection.DocumentSerializer).Equals(current.ToBsonDocument(collection.DocumentSerializer));

    /// <summary>
    /// Takes a document out of an unpaged observed set.
    /// </summary>
    /// <param name="documents">The observed set to remove from.</param>
    /// <param name="id">The identifier of the document to remove.</param>
    /// <param name="changes">Collects the removal.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>True when a document was removed.</returns>
    static bool RemoveFromSet<TDocument>(
        QueryContextAwareSet<TDocument> documents,
        object id,
        List<CollectionChange> changes)
    {
        var removed = documents.Remove(id);
        if (removed)
        {
            changes.Add(new(CollectionChangeKind.Removed, id));
        }

        return removed;
    }

    static object GetId(PropertyInfo idProperty, BsonMemberMap? idMemberMap, BsonValue idValue)
    {
        if (idMemberMap is not null)
        {
            using var reader = new BsonDocumentReader(new BsonDocument("_id", idValue));
            reader.ReadStartDocument();
            reader.ReadName(Utf8NameDecoder.Instance);
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = idMemberMap.GetSerializer().Deserialize(context, new BsonDeserializationArgs { NominalType = idMemberMap.MemberType });
            reader.ReadEndDocument();
            return result;
        }

        var id = BsonTypeMapper.MapToDotNetValue(idValue);
        if (idProperty.PropertyType.IsConcept())
        {
            id = ConceptFactory.CreateConceptInstance(idProperty.PropertyType, id);
        }

        return id;
    }

    static IFindFluent<TDocument, TDocument> AddPaging<TDocument>(QueryContext queryContext, IFindFluent<TDocument, TDocument> response)
    {
        if (queryContext.Paging.IsPaged)
        {
            response = response
                .Skip(queryContext.Paging.Skip)
                .Limit(queryContext.Paging.Size);
        }

        return response;
    }

    static IFindFluent<TDocument, TDocument> AddSorting<TDocument>(QueryContext queryContext, IFindFluent<TDocument, TDocument> response)
    {
        // Documents sharing a sort value - or every document, when nothing is sorted - come back in whatever order
        // the server finds convenient, which may differ between two reads. A page boundary drawn through such a run
        // then shows a document on two pages or on none, so ties are broken on _id, the one field always unique.
        var byId = Builders<TDocument>.Sort.Ascending("_id");
        if (queryContext.Sorting != Sorting.None)
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(TDocument));
            var memberMap = classMap.GetMemberMap(queryContext.Sorting.Field);

            // An unknown sort field has no member map — degrade to unsorted rather than dereferencing null
            // (which surfaces as an HTTP 500), matching the EF Core provider's AddSorting graceful degradation.
            if (memberMap is not null)
            {
                var sort = queryContext.Sorting.Direction == Cratis.Arc.Queries.SortDirection.Ascending ?
                    Builders<TDocument>.Sort.Ascending(memberMap.ElementName) :
                    Builders<TDocument>.Sort.Descending(memberMap.ElementName);
                return response.Sort(memberMap.ElementName == "_id" ? sort : Builders<TDocument>.Sort.Combine(sort, byId));
            }
        }

        return queryContext.Paging.IsPaged ? response.Sort(byId) : response;
    }

    static bool IsRelevantChange(ChangeStreamDocument<BsonDocument> change, CollectionNamespace collectionNamespace) =>
        RequiresResynchronization(change, collectionNamespace) ||
        (collectionNamespace.Equals(change.CollectionNamespace) &&
        change.OperationType is
            ChangeStreamOperationType.Insert or
            ChangeStreamOperationType.Update or
            ChangeStreamOperationType.Replace or
            ChangeStreamOperationType.Delete);

    static bool RequiresResynchronization(ChangeStreamDocument<BsonDocument> change, CollectionNamespace collectionNamespace) =>
        change.OperationType is ChangeStreamOperationType.Invalidate or ChangeStreamOperationType.DropDatabase ||
        (change.OperationType is ChangeStreamOperationType.Drop or ChangeStreamOperationType.Rename && collectionNamespace.Equals(change.CollectionNamespace)) ||
        (change.OperationType == ChangeStreamOperationType.Rename && collectionNamespace.Equals(change.RenameTo));

    static TDocument Deserialize<TDocument>(IMongoCollection<TDocument> collection, BsonDocument document)
    {
        using var reader = new BsonDocumentReader(document);
        return collection.DocumentSerializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }

    /// <summary>
    /// Internal class used as an identifying type for logging purpose.
    /// </summary>
    internal sealed class MongoCollection;
}
