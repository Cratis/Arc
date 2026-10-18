// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBObserveBuilder{TDocument}"/>.
/// </summary>
/// <param name="collection">The primary collection to observe.</param>
/// <param name="observeFilter">Optional filter expression for the primary documents.</param>
/// <param name="databaseChanges">The observable database-level change stream.</param>
/// <param name="logger">The shared watch logger.</param>
/// <typeparam name="TDocument">Type of the primary document.</typeparam>
internal sealed class MongoDBObserveBuilder<TDocument>(
    IMongoCollection<TDocument> collection,
    Expression<Func<TDocument, bool>>? observeFilter,
    IObservable<ChangeStreamDocument<BsonDocument>> databaseChanges,
    ILogger<MongoDBWatcher>? logger = null)
    : IMongoDBObserveBuilder<TDocument>
{
    /// <inheritdoc/>
    public IMongoDBJoinedObserveBuilder<TDocument, TJoined> Join<TJoined>(
        Expression<Func<TJoined, bool>>? filter = null)
    {
        var joinedCollection = collection.Database.GetCollection<TJoined>();
        return new MongoDBJoinedObserveBuilder<TDocument, TJoined>(
            collection,
            observeFilter,
            joinedCollection,
            filter,
            databaseChanges,
            logger);
    }
}
