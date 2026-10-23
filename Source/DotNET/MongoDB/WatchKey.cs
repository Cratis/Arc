// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Identifies one shared change stream: a database on a deployment, as reached with a set of credentials.
/// </summary>
/// <param name="Servers">The servers the client connects to.</param>
/// <param name="Credential">The credential the client connects with, if any.</param>
/// <param name="Database">The name of the database.</param>
/// <remarks>
/// Keyed the way <see cref="MongoDBClientFactory"/> caches clients, so the same deployment reached through two client
/// instances - an intercepted client and the driver client behind it, for instance - shares one stream, while the same
/// database name on two deployments does not.
/// </remarks>
readonly record struct WatchKey(string Servers, MongoCredential? Credential, string Database)
{
    /// <summary>
    /// Creates the key for a database on a client.
    /// </summary>
    /// <param name="client">The <see cref="IMongoClient"/> the database is reached with.</param>
    /// <param name="database">The name of the database.</param>
    /// <returns>The <see cref="WatchKey"/>.</returns>
    public static WatchKey For(IMongoClient client, string database)
    {
        var settings = client.Settings;
        return new(settings?.Servers is null ? string.Empty : string.Join(',', settings.Servers), settings?.Credential, database);
    }
}
