// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents an implementation of <see cref="IMongoDBJoinedObserveBuilder{T1, T2}"/>.
/// </summary>
/// <param name="collection1">The first collection.</param>
/// <param name="filter1">Optional filter for the first collection.</param>
/// <param name="collection2">The second (joined) collection.</param>
/// <param name="filter2">Optional filter for the second collection.</param>
/// <param name="databaseChanges">The observable database-level change stream.</param>
/// <param name="logger">The shared watch logger.</param>
/// <typeparam name="T1">Type of the first document.</typeparam>
/// <typeparam name="T2">Type of the second document.</typeparam>
internal sealed class MongoDBJoinedObserveBuilder<T1, T2>(
    IMongoCollection<T1> collection1,
    Expression<Func<T1, bool>>? filter1,
    IMongoCollection<T2> collection2,
    Expression<Func<T2, bool>>? filter2,
    IObservable<ChangeStreamDocument<BsonDocument>> databaseChanges,
    ILogger<MongoDBWatcher>? logger = null)
    : IMongoDBJoinedObserveBuilder<T1, T2>
{
    /// <inheritdoc/>
    public IMongoDBJoinedObserveBuilder<T1, T2, T3> Join<T3>(
        Expression<Func<T3, bool>>? filter = null)
    {
        var collection3 = collection1.Database.GetCollection<T3>();
        return new MongoDBJoinedObserveBuilder<T1, T2, T3>(
            collection1,
            filter1,
            collection2,
            filter2,
            collection3,
            filter,
            databaseChanges,
            logger);
    }

    /// <inheritdoc/>
    public ISubject<TResult> Select<TResult>(
        Func<IEnumerable<T1>, IEnumerable<T2>, TResult> selector) =>
        MongoDBObservation.Create(
            databaseChanges,
            new HashSet<CollectionNamespace> { collection1.CollectionNamespace, collection2.CollectionNamespace },
            async token =>
            {
                var docs1 = await collection1.Find(filter1 ?? (_ => true)).ToListAsync(token);
                var docs2 = await collection2.Find(filter2 ?? (_ => true)).ToListAsync(token);
                return (docs1, docs2);
            },
            docs => selector(docs.docs1, docs.docs2),
            logger ?? NullLogger<MongoDBWatcher>.Instance);
}

/// <summary>
/// Represents an implementation of <see cref="IMongoDBJoinedObserveBuilder{T1, T2, T3}"/>.
/// </summary>
/// <param name="collection1">The first collection.</param>
/// <param name="filter1">Optional filter for the first collection.</param>
/// <param name="collection2">The second (joined) collection.</param>
/// <param name="filter2">Optional filter for the second collection.</param>
/// <param name="collection3">The third (joined) collection.</param>
/// <param name="filter3">Optional filter for the third collection.</param>
/// <param name="databaseChanges">The observable database-level change stream.</param>
/// <param name="logger">The shared watch logger.</param>
/// <typeparam name="T1">Type of the first document.</typeparam>
/// <typeparam name="T2">Type of the second document.</typeparam>
/// <typeparam name="T3">Type of the third document.</typeparam>
internal sealed class MongoDBJoinedObserveBuilder<T1, T2, T3>(
    IMongoCollection<T1> collection1,
    Expression<Func<T1, bool>>? filter1,
    IMongoCollection<T2> collection2,
    Expression<Func<T2, bool>>? filter2,
    IMongoCollection<T3> collection3,
    Expression<Func<T3, bool>>? filter3,
    IObservable<ChangeStreamDocument<BsonDocument>> databaseChanges,
    ILogger<MongoDBWatcher>? logger = null)
    : IMongoDBJoinedObserveBuilder<T1, T2, T3>
{
    /// <inheritdoc/>
    public ISubject<TResult> Select<TResult>(
        Func<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, TResult> selector) =>
        MongoDBObservation.Create(
            databaseChanges,
            new HashSet<CollectionNamespace> { collection1.CollectionNamespace, collection2.CollectionNamespace, collection3.CollectionNamespace },
            async token =>
            {
                var docs1 = await collection1.Find(filter1 ?? (_ => true)).ToListAsync(token);
                var docs2 = await collection2.Find(filter2 ?? (_ => true)).ToListAsync(token);
                var docs3 = await collection3.Find(filter3 ?? (_ => true)).ToListAsync(token);
                return (docs1, docs2, docs3);
            },
            docs => selector(docs.docs1, docs.docs2, docs.docs3),
            logger ?? NullLogger<MongoDBWatcher>.Instance);
}
