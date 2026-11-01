// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Connections;

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

    /// <summary>
    /// Gets or sets whether MongoDB command bodies and failure messages are written to the log.
    /// </summary>
    /// <remarks>
    /// Defaults to false: with Trace logging enabled for the client factory, only metadata is logged (request id,
    /// command, database, collection, duration, and failure type and server error code). When true, the full command
    /// body and the driver failure message are logged too. These contain document, update and filter values, which
    /// may include personal data, so enable this only while diagnosing and never in production.
    /// </remarks>
    public bool LogCommandBodies { get; set; }

    /// <summary>
    /// Gets or sets how connections are resolved per tenant, overriding the application-wide
    /// <see cref="ArcOptions.ConnectionResolution"/> for MongoDB.
    /// </summary>
    /// <remarks>
    /// When neither is configured, <see cref="Server"/> and <see cref="Database"/> are used as they are. A resolved
    /// connection string replaces <see cref="Server"/>, a resolved database replaces the database name, and a resolved
    /// identity becomes the credential — a user name and password for SCRAM, or an access token for MONGODB-OIDC.
    /// </remarks>
    public ConnectionResolutionOptions ConnectionResolution { get; set; } = new();
}
