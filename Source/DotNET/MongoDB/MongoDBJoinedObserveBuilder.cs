// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Threading.Channels;
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
/// <typeparam name="T1">Type of the first document.</typeparam>
/// <typeparam name="T2">Type of the second document.</typeparam>
internal sealed class MongoDBJoinedObserveBuilder<T1, T2>(
    IMongoCollection<T1> collection1,
    Expression<Func<T1, bool>>? filter1,
    IMongoCollection<T2> collection2,
    Expression<Func<T2, bool>>? filter2,
    IObservable<ChangeStreamDocument<BsonDocument>> databaseChanges)
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
            databaseChanges);
    }

    /// <inheritdoc/>
    public ISubject<TResult> Select<TResult>(
        Func<IEnumerable<T1>, IEnumerable<T2>, TResult> selector)
    {
    #pragma warning disable CA2000 // Dispose objects before losing scope
        // The CancellationTokenSource is disposed in the finally block of the background Task.Run below.
        var cts = new CancellationTokenSource();
    #pragma warning restore CA2000 // Dispose objects before losing scope
        var subject = LifetimeAwareSubject<TResult>.Create(cts.Cancel);
        ISubject<TResult> observable = subject;
        var collectionName1 = collection1.CollectionNamespace.CollectionName;
        var collectionName2 = collection2.CollectionNamespace.CollectionName;
        var relevantCollections = new HashSet<string> { collectionName1, collectionName2 };

        _ = Task.Run(async () =>
        {
            var token = cts.Token;
            var channel = Channel.CreateUnbounded<ChangeStreamDocument<BsonDocument>>();

            var subscription = databaseChanges
                .Subscribe(change =>
                {
                    if (relevantCollections.Contains(change.CollectionNamespace?.CollectionName ?? string.Empty))
                    {
                        channel.Writer.TryWrite(change);
                    }
                });

            try
            {
                var findFilter1 = filter1 ?? (_ => true);
                var findFilter2 = filter2 ?? (_ => true);

                var docs1 = await collection1.Find(findFilter1).ToListAsync(token);
                var docs2 = await collection2.Find(findFilter2).ToListAsync(token);

                subject.OnNext(selector(docs1, docs2));

                await foreach (var change in channel.Reader.ReadAllAsync(token))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (change.CollectionNamespace?.CollectionName == collectionName1)
                    {
                        docs1 = await collection1.Find(findFilter1).ToListAsync(token);
                    }
                    else if (change.CollectionNamespace?.CollectionName == collectionName2)
                    {
                        docs2 = await collection2.Find(findFilter2).ToListAsync(token);
                    }

                    subject.OnNext(selector(docs1, docs2));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                subject.OnError(ex);
            }
            finally
            {
                subscription.Dispose();
                channel.Writer.TryComplete();
                subject.OnCompleted();
                subject.Dispose();
                cts.Dispose();
            }
        });

        return observable;
    }
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
    IObservable<ChangeStreamDocument<BsonDocument>> databaseChanges)
    : IMongoDBJoinedObserveBuilder<T1, T2, T3>
{
    /// <inheritdoc/>
    public ISubject<TResult> Select<TResult>(
        Func<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, TResult> selector)
    {
    #pragma warning disable CA2000 // Dispose objects before losing scope
        // The CancellationTokenSource is disposed in the finally block of the background Task.Run below.
        var cts = new CancellationTokenSource();
    #pragma warning restore CA2000 // Dispose objects before losing scope
        var subject = LifetimeAwareSubject<TResult>.Create(cts.Cancel);
        ISubject<TResult> observable = subject;
        var collectionName1 = collection1.CollectionNamespace.CollectionName;
        var collectionName2 = collection2.CollectionNamespace.CollectionName;
        var collectionName3 = collection3.CollectionNamespace.CollectionName;
        var relevantCollections = new HashSet<string> { collectionName1, collectionName2, collectionName3 };

        _ = Task.Run(async () =>
        {
            var token = cts.Token;
            var channel = Channel.CreateUnbounded<ChangeStreamDocument<BsonDocument>>();

            var subscription = databaseChanges
                .Subscribe(change =>
                {
                    if (relevantCollections.Contains(change.CollectionNamespace?.CollectionName ?? string.Empty))
                    {
                        channel.Writer.TryWrite(change);
                    }
                });

            try
            {
                var findFilter1 = filter1 ?? (_ => true);
                var findFilter2 = filter2 ?? (_ => true);
                var findFilter3 = filter3 ?? (_ => true);

                var docs1 = await collection1.Find(findFilter1).ToListAsync(token);
                var docs2 = await collection2.Find(findFilter2).ToListAsync(token);
                var docs3 = await collection3.Find(findFilter3).ToListAsync(token);

                subject.OnNext(selector(docs1, docs2, docs3));

                await foreach (var change in channel.Reader.ReadAllAsync(token))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (change.CollectionNamespace?.CollectionName == collectionName1)
                    {
                        docs1 = await collection1.Find(findFilter1).ToListAsync(token);
                    }
                    else if (change.CollectionNamespace?.CollectionName == collectionName2)
                    {
                        docs2 = await collection2.Find(findFilter2).ToListAsync(token);
                    }
                    else if (change.CollectionNamespace?.CollectionName == collectionName3)
                    {
                        docs3 = await collection3.Find(findFilter3).ToListAsync(token);
                    }

                    subject.OnNext(selector(docs1, docs2, docs3));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                subject.OnError(ex);
            }
            finally
            {
                subscription.Dispose();
                channel.Writer.TryComplete();
                subject.OnCompleted();
                subject.Dispose();
                cts.Dispose();
            }
        });

        return observable;
    }
}
