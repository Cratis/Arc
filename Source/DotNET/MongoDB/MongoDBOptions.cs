// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents the configuration for MongoDB.
/// </summary>
public class MongoDBOptions
{
    /// <summary>
    /// The server url.
    /// </summary>
    [Required]
    public string Server { get; set; } = null!;

    /// <summary>
    /// The database name.
    /// </summary>
    [Required]
    public string Database { get; set; } = null!;

    /// <summary>
    /// Gets whether or use the direct connection option for MongoDB.
    /// </summary>
    /// <remarks>
    /// The direct connection option is used to connect directly to a single MongoDB server, instead
    /// of using the replica set discovery mechanism. This can be useful for development and testing
    /// scenarios where a single MongoDB server is used.
    /// Also in scenarios where the MongoDB server is behind a load balancer or proxy that does not
    /// support the replica set discovery mechanism, or in a Docker compose environment and the
    /// single replicate points to "localhost".
    /// Setting this to true forces a direct connection to the configured server.
    /// When not set, the value from the connection string is preserved.
    /// </remarks>
    public bool? DirectConnection { get; set; }
}
