// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks;

/// <summary>
/// Defines the clients of the named MongoDB sinks configured in <see cref="MongoDBOptions.Sinks"/>.
/// </summary>
public interface IMongoDBSinks
{
    /// <summary>
    /// Get the <see cref="IMongoClient"/> of a named sink.
    /// </summary>
    /// <param name="sinkName">The name of the sink. Matched without regard to case.</param>
    /// <returns>The <see cref="IMongoClient"/> connected to the sink's server.</returns>
    /// <exception cref="UnknownMongoDBSink">Thrown when no sink of that name is configured.</exception>
    IMongoClient GetClientFor(string sinkName);
}
