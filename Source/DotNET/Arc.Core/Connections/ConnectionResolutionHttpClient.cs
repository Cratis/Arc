// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Holds the <see cref="HttpClient"/> shared by everything that calls out over HTTP for connection resolution.
/// </summary>
static class ConnectionResolutionHttpClient
{
    /// <summary>
    /// Gets the shared <see cref="HttpClient"/>.
    /// </summary>
    internal static HttpClient Instance { get; } = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) });
}
