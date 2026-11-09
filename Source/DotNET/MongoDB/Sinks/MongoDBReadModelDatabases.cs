// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Sinks;
using Cratis.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBReadModelDatabases"/>.
/// </summary>
/// <remarks>
/// Chronicle names a named MongoDB sink's databases exactly as it names its default read model databases, so the database name
/// comes from the scope's default <see cref="MongoDBConnection"/> and only the server changes.
/// </remarks>
/// <param name="connection">The <see cref="MongoDBConnection"/> of the scope.</param>
/// <param name="defaultDatabase">The default <see cref="IMongoDatabase"/> of the scope.</param>
/// <param name="readModelSinks">The <see cref="IReadModelSinks"/> telling which sink a type is stored in.</param>
/// <param name="sinks">The <see cref="IMongoDBSinks"/> giving the clients of the named sinks.</param>
[IgnoreConvention]
public class MongoDBReadModelDatabases(
    MongoDBConnection connection,
    IMongoDatabase defaultDatabase,
    IReadModelSinks readModelSinks,
    IMongoDBSinks sinks) : IMongoDBReadModelDatabases
{
    /// <inheritdoc/>
    public IMongoDatabase GetFor(Type readModelType)
    {
        var sinkName = readModelSinks.GetSinkNameFor(readModelType);
        return sinkName is null ? defaultDatabase : sinks.GetClientFor(sinkName).GetDatabase(connection.DatabaseName);
    }
}
