// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Arc;
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
        return result.SingleOrDefault();
    }

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>Async Task holding <see cref="Subject{T}"/> with a collection of the type for the collection.</returns>
    public static ISubject<IEnumerable<TDocument>> Observe<TDocument>(
        this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>>? filter,
        FindOptions? options = null)
    {
        filter ??= _ => true;
        return collection.Observe(
            () => collection.Find(filter, options),
            filter,
            documents => new BehaviorSubject<IEnumerable<TDocument>>(documents),
            (cursor, observable) => observable.OnNext([.. cursor]));
    }

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>Async Task holding <see cref="Subject{T}"/> with a collection of the type for the collection.</returns>
    public static ISubject<TDocument> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>>? filter,
        FindOptions? options = null)
    {
        filter ??= _ => true;
        return collection.ObserveSingle(() => collection.Find(filter, options), filter);
    }

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>Async Task holding <see cref="Subject{T}"/> with a collection of the type for the collection.</returns>
    public static ISubject<IEnumerable<TDocument>> Observe<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument>? filter = null,
        FindOptions? options = null)
    {
        filter ??= FilterDefinition<TDocument>.Empty;
        return collection.Observe(
            () => collection.Find(filter, options),
            filter,
            documents => new BehaviorSubject<IEnumerable<TDocument>>(documents),
            (documents, observable) => observable.OnNext(documents));
    }

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>Async Task holding <see cref="Subject{T}"/> with a collection of the type for the collection.</returns>
    public static ISubject<TDocument> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument>? filter = null,
        FindOptions? options = null)
    {
        filter ??= FilterDefinition<TDocument>.Empty;
        return collection.ObserveSingle(() => collection.Find(filter, options), filter);
    }

    /// <summary>
    /// Create an observable query that will observe a single document based on Id of the document in the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="id">The identifier of the document to observe.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <typeparam name="TId">Type of id - key.</typeparam>
    /// <returns>Async Task holding <see cref="Subject{T}"/> with an instance of the type.</returns>
    public static ISubject<TDocument> ObserveById<TDocument, TId>(this IMongoCollection<TDocument> collection, TId id)
    {
        var filter = Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, TId>("_id"), id);
        return collection.ObserveSingle(() => collection.Find(filter), filter);
    }

    static ISubject<TDocument> ObserveSingle<TDocument>(
         this IMongoCollection<TDocument> collection,
         Func<IFindFluent<TDocument, TDocument>> findCall,
         FilterDefinition<TDocument> filter)
    {
        return collection.Observe<TDocument, TDocument>(
            findCall,
            filter,
            documents =>
            {
                var result = documents.FirstOrDefault();
                if (result is not null)
                {
                    return new BehaviorSubject<TDocument>(result);
                }

                return new Subject<TDocument>();
            },
            (documents, observable) =>
            {
                var result = documents.FirstOrDefault();
                if (result is not null)
                {
                    observable.OnNext(result);
                }
            });
    }

    static ISubject<TResult> Observe<TDocument, TResult>(
        this IMongoCollection<TDocument> collection,
        Func<IFindFluent<TDocument, TDocument>> findCall,
        FilterDefinition<TDocument> filter,
        Func<IEnumerable<TDocument>, ISubject<TResult>> createSubject,
        Action<IEnumerable<TDocument>, ISubject<TResult>> onNext)
    {
        var completedCleanup = false;
        var subject = createSubject([]);
        var logger = Internals.ServiceProvider.GetRequiredService<ILogger<MongoCollection>>();
        var queryContextManager = Internals.ServiceProvider.GetRequiredService<IQueryContextManager>();
        var queryContext = queryContextManager.Current;

        var classMap = BsonClassMap.LookupClassMap(typeof(TDocument));
        var idProperty = typeof(TDocument).GetProperty(classMap.IdMemberMap?.MemberName ?? "Id", BindingFlags.Instance | BindingFlags.Public) ?? throw new MissingIdMapping(typeof(TDocument));
        var documents = new QueryContextAwareSet<TDocument>(queryContext, idProperty);

        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };
        var filterRendered = filter.Render(new(collection.DocumentSerializer, collection.Settings.SerializerRegistry));
        PrefixKeys(filterRendered);

        var fullFilter = Builders<ChangeStreamDocument<TDocument>>.Filter.Or(
            Builders<ChangeStreamDocument<TDocument>>.Filter.And(
                   filterRendered,
                   Builders<ChangeStreamDocument<TDocument>>.Filter.In(
                       new StringFieldDefinition<ChangeStreamDocument<TDocument>, string>("operationType"),
                       ["insert", "replace", "update", "delete"])),
            Builders<ChangeStreamDocument<TDocument>>.Filter.Eq("fullDocument", BsonNull.Value));

        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TDocument>>().Match(fullFilter);

#pragma warning disable CA2000 // Dispose objects before losing scope
        var cancellationTokenSource = new CancellationTokenSource();
#pragma warning restore CA2000 // Dispose objects before losing scope
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        _ = Task.Run(Watch);
        return subject;

        async Task Watch()
        {
            try
            {
                var query = findCall();
                query = AddSorting(queryContext, query);
                query = AddPaging(queryContext, query);

                using var cursor = await collection.WatchAsync(pipeline, options, cancellationToken);
                _ = subject.Subscribe(_ => { }, _ => { }, Cleanup);
                queryContext.TotalItems = (int)await findCall().CountDocumentsAsync();
                await documents.InitializeWithQuery(query);
                onNext(documents, subject);
                await cursor.ForEachAsync(
                    async changeDocument =>
                    {
                        try
                        {
                            await HandleChange(
                                queryContext,
                                onNext,
                                changeDocument,
                                query,
                                documents,
                                subject,
                                idProperty);
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

        void Cleanup()
        {
            if (completedCleanup)
            {
                return;
            }
            logger.CleaningUp();
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            subject?.OnCompleted();
            completedCleanup = true;
        }
    }

    static async Task HandleChange<TDocument, TResult>(
        QueryContext queryContext,
        Action<IEnumerable<TDocument>, ISubject<TResult>> onNext,
        ChangeStreamDocument<TDocument> changeDocument,
        IFindFluent<TDocument, TDocument> query,
        QueryContextAwareSet<TDocument> documents,
        ISubject<TResult> subject,
        PropertyInfo idProperty)
    {
        var hasChanges = false;
        if (changeDocument.DocumentKey is not null && changeDocument.DocumentKey.TryGetValue("_id", out var idValue))
        {
            var id = GetId(idProperty, idValue);
            var fullDocument = changeDocument.FullDocument;
            if (changeDocument.OperationType == ChangeStreamOperationType.Delete)
            {
                queryContext.TotalItems--;
                if (queryContext.Paging.IsPaged)
                {
                    hasChanges = await documents.RemoveAndAddLastInQuery(id, query);
                }
                else
                {
                   hasChanges = documents.Remove(id);
                }
            }
            else if (changeDocument.OperationType == ChangeStreamOperationType.Insert)
            {
                queryContext.TotalItems++;
                hasChanges = documents.Add(fullDocument);
            }
            else if (fullDocument is not null)
            {
                hasChanges = documents.Add(fullDocument);
            }
        }
        if (hasChanges)
        {
            onNext(documents, subject);
        }
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

            var sort = queryContext.Sorting.Direction == Cratis.Arc.Queries.SortDirection.Ascending ?
                Builders<TDocument>.Sort.Ascending(memberMap.ElementName) :
                Builders<TDocument>.Sort.Descending(memberMap.ElementName);
            response = response.Sort(sort);
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
