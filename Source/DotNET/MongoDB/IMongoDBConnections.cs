// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Defines a system that resolves the MongoDB connection for the current tenant through the configured connection resolution.
/// </summary>
public interface IMongoDBConnections
{
    /// <summary>
    /// Resolve the connection for the current tenant.
    /// </summary>
    /// <returns>The resolved <see cref="MongoDBConnection"/>.</returns>
    MongoDBConnection Resolve();
}
