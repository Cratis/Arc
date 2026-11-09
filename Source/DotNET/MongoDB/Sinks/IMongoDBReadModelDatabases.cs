// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks;

/// <summary>
/// Defines a system that gives the <see cref="IMongoDatabase"/> a read model type is stored in for the current scope.
/// </summary>
/// <remarks>
/// A read model stored in a named sink (see <see cref="Cratis.Arc.Sinks.IReadModelSinks"/>) is read from that sink's server, in
/// the database the default connection resolves for the scope. Every other type is read from the default database.
/// </remarks>
public interface IMongoDBReadModelDatabases
{
    /// <summary>
    /// Get the database a read model type is stored in.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <returns>The <see cref="IMongoDatabase"/>.</returns>
    /// <exception cref="UnknownMongoDBSink">Thrown when the type is stored in a sink that has no MongoDB configuration.</exception>
    IMongoDatabase GetFor(Type readModelType);
}
