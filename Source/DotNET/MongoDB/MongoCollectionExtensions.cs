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

    /// <summary>Observe a single matching document using the current query context.</summary>
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

    /// <summary>Observe a single matching document using the current query context.</summary>
    /// <param name="collection">The collection to observe.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional find options.</param>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <returns>The observable document.</returns>
    public static ISubject<TDocument> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument>? filter = null,
        FindOptions? options = null) => collection.ObserveSingle(filter, false, options);

    /// <summary>Observe a document by id using the current query context.</summary>
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
    /// <param name="ignoreQueryContext">Whether to read the full filtered source without client paging, sorting, or total count updates.</param>
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
        filter ??= _ => true;
        return collection.ObserveSingle(() => collection.Find(filter, options), filter, ignoreQueryContext);
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
    /// <param name="ignoreQueryContext">Whether to read the full filtered source without client paging, sorting, or total count updates.</param>
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
        filterDefinition ??= FilterDefinition<TDocument>.Empty;
        return collection.ObserveSingle(() => collection.Find(filterDefinition, options), filterDefinition, ignoreQueryContext);
    }

    /// <summary>
    /// Observe a single matching document with the query-context option before the MongoDB filter.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
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
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <typeparam name="TId">Type of id - key.</typeparam>
    /// <returns>
    /// An <see cref="ISubject{T}"/> with an instance of the type; emits <see langword="default"/> when no
    /// document matches — after the observed document is deleted, or when the initial query finds none.
    /// </returns>
    public static ISubject<TDocument> ObserveById<TDocument, TId>(this IMongoCollection<TDocument> collection, TId id, bool ignoreQueryContext)
    {
        var filter = Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, TId>("_id"), id);
        return collection.ObserveSingle(() => collection.Find(filter), filter, ignoreQueryContext);
    }

    /// <summary>
    /// Create an observable query that will observe a single document in the collection, driven by an
    /// explicit initial query and filter.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="findCall">Produces the initial, sorted and paged query for the observed document.</param>
    /// <param name="filter">The filter identifying the observed document.</param>
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
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
         FilterDefinition<TDocument> filter,
         bool ignoreQueryContext)
    {
        return collection.Observe<TDocument, TDocument>(
            findCall,
            filter,
            ignoreQueryContext,
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

        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };
        var filterRendered = filter.Render(new(collection.DocumentSerializer, collection.Settings.SerializerRegistry));
        PrefixKeys(filterRendered);

        // An insert is narrowed by the observed filter server-side: a document that never matched is
        // of no interest to this observer and there is no reason to carry it over the wire.
        //
        // An update or a replace is deliberately NOT narrowed, and that is the whole point. The
        // filter is rendered against fullDocument - the document as it is *after* the change - so
        // narrowing update events by it discards precisely the events that say a document has left
        // the result set. An observer watching "work that is still running" would be told when work
        // started and never when it finished, so the finished item stayed in the observed set until
        // the client reconnected. Membership is decided per event in HandleChange instead.
        var fullFilter = Builders<ChangeStreamDocument<TDocument>>.Filter.Or(
            Builders<ChangeStreamDocument<TDocument>>.Filter.And(
                   filterRendered,
                   Builders<ChangeStreamDocument<TDocument>>.Filter.Eq(
                       new StringFieldDefinition<ChangeStreamDocument<TDocument>, string>("operationType"),
                       "insert")),
            Builders<ChangeStreamDocument<TDocument>>.Filter.In(
                new StringFieldDefinition<ChangeStreamDocument<TDocument>, string>("operationType"),
                ["replace", "update", "delete"]),
            Builders<ChangeStreamDocument<TDocument>>.Filter.Eq("fullDocument", BsonNull.Value));

        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TDocument>>().Match(fullFilter);

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

                using var cursor = await collection.WatchAsync(pipeline, options, cancellationToken);
                if (!ignoreQueryContext)
                {
                    queryContext.TotalItems = (int)await findCall().CountDocumentsAsync();
                }
                await documents.InitializeWithQuery(query);

                // The initial emission is a whole snapshot, not a delta - there is no previous state to state a
                // change against, so it carries no changes and downstream treats it as the baseline.
                onNext(documents, null, subject);
                await cursor.ForEachAsync(
                    async changeDocument =>
                    {
                        try
                        {
                            await HandleChange(
                                collection,
                                filter,
                                observationContext,
                                onNext,
                                changeDocument,
                                query,
                                documents,
                                subject,
                                idProperty,
                                cancellationToken);
                        }
                        catch (Exception e)
                        {
                            logger.UnexpectedError(e);
                        }
                    },
                    cancellationToken);
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
                logger.UnexpectedError(ex);
            }
            finally
            {
                Cleanup();
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
        QueryContext queryContext,
        Action<IEnumerable<TDocument>, IReadOnlyList<CollectionChange>?, ISubject<TResult>> onNext,
        ChangeStreamDocument<TDocument> changeDocument,
        IFindFluent<TDocument, TDocument> query,
        QueryContextAwareSet<TDocument> documents,
        ISubject<TResult> subject,
        PropertyInfo idProperty,
        CancellationToken cancellationToken)
    {
        var hasChanges = false;
        var changes = new List<CollectionChange>();
        if (changeDocument.DocumentKey is not null && changeDocument.DocumentKey.TryGetValue("_id", out var idValue))
        {
            var id = GetId(idProperty, idValue);
            var fullDocument = changeDocument.FullDocument;
            if (changeDocument.OperationType == ChangeStreamOperationType.Delete)
            {
                queryContext.TotalItems--;
                hasChanges = await RemoveFromSet(queryContext, query, documents, id, changes);
            }
            else if (changeDocument.OperationType == ChangeStreamOperationType.Insert)
            {
                queryContext.TotalItems++;
                hasChanges = documents.Add(fullDocument);
                if (hasChanges)
                {
                    changes.Add(new(CollectionChangeKind.Added, id));
                }
            }
            else if (fullDocument is not null)
            {
                // An update or a replace. It arrives whether or not the new document still satisfies
                // the observed filter, because an event saying a document has left the result set
                // looks exactly like one saying it never belonged. Ask the collection which it is:
                // one indexed lookup on the document's own key.
                var belongs = await collection
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
                    hasChanges = await RemoveFromSet(queryContext, query, documents, id, changes);
                }
            }
        }
        if (hasChanges)
        {
            onNext(documents, changes, subject);
        }
    }

    /// <summary>
    /// Takes a document out of the observed set, refilling the page behind it when the query is paged.
    /// </summary>
    /// <param name="queryContext">The <see cref="QueryContext"/> carrying the paging state.</param>
    /// <param name="query">The sorted and paged query the refill reads from.</param>
    /// <param name="documents">The observed set to remove from.</param>
    /// <param name="id">The identifier of the document to remove.</param>
    /// <param name="changes">Collects the changes the removal made, including any refill a paged query pulled in.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>True when a document was removed.</returns>
    static async Task<bool> RemoveFromSet<TDocument>(
        QueryContext queryContext,
        IFindFluent<TDocument, TDocument> query,
        QueryContextAwareSet<TDocument> documents,
        object id,
        List<CollectionChange> changes)
    {
        if (!queryContext.Paging.IsPaged)
        {
            var removedUnpaged = documents.Remove(id);
            if (removedUnpaged)
            {
                changes.Add(new(CollectionChangeKind.Removed, id));
            }

            return removedUnpaged;
        }

        var (removed, addedId) = await documents.RemoveAndAddLastInQuery(id, query);
        if (removed)
        {
            changes.Add(new(CollectionChangeKind.Removed, id));
        }

        if (addedId is not null)
        {
            changes.Add(new(CollectionChangeKind.Added, addedId));
        }

        return removed;
    }

    static object GetId(PropertyInfo idProperty, BsonValue idValue)
    {
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
                response = response.Sort(sort);
            }
        }

        return response;
    }

    static void PrefixKeys(BsonDocument document)
    {
        foreach (var name in document.Names.ToArray())
        {
            var value = document[name];
            if (!name.StartsWith('$'))
            {
                var index = document.IndexOfName(name);
                document.InsertAt(index, new BsonElement($"fullDocument.{name}", value));
                document.Remove(name);
            }

            if (value is BsonArray array)
            {
                foreach (var item in array)
                {
                    if (item is BsonDocument itemAsDocument)
                    {
                        PrefixKeys(itemAsDocument);
                    }
                }
            }
            else if (value is BsonDocument childAsDocument)
            {
                PrefixKeys(childAsDocument);
            }
        }
    }

    /// <summary>
    /// Internal class used as an identifying type for logging purpose.
    /// </summary>
    internal sealed class MongoCollection;
}
