// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Defines a system that maintains a single change stream connection per database per process,
/// allowing multiple collection observations to share the same underlying change stream.
/// </summary>
public interface IMongoDBWatcher
{
    /// <summary>
    /// Start observing a collection using the shared database-level change stream.
    /// </summary>
    /// <param name="filter">Optional filter expression for the documents to observe.</param>
    /// <typeparam name="TDocument">Type of document to observe.</typeparam>
    /// <returns>A <see cref="IMongoDBObserveBuilder{TDocument}"/> for chaining joins and finalizing the observation.</returns>
    IMongoDBObserveBuilder<TDocument> Observe<TDocument>(
        Expression<Func<TDocument, bool>>? filter = null);

    /// <summary>
    /// Gets the changes of the shared change stream for a database, starting the stream when it is not already open.
    /// </summary>
    /// <param name="databaseName">Name of the database whose changes to get.</param>
    /// <returns>A hot <see cref="IObservable{T}"/> of every change in the database, across all of its collections.</returns>
    /// <remarks>
    /// Every subscription to one database shares a single change stream, so subscribing costs no database connection.
    /// A subscriber sees only changes that arrive after it subscribed.
    /// </remarks>
    IObservable<ChangeStreamDocument<BsonDocument>> GetChanges(string databaseName);
}
