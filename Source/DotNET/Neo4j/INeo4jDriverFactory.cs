// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Defines a factory that gives the pooled <see cref="IDriver"/> for a server and an identity.
/// </summary>
/// <remarks>
/// A driver owns a connection pool and is meant to live as long as the application, so the factory keeps one per
/// server and identity and disposes them with the host. Never dispose a driver you got from it.
/// </remarks>
public interface INeo4jDriverFactory
{
    /// <summary>
    /// Get the authentication the options describe, reading a configured password secret.
    /// </summary>
    /// <returns>The <see cref="BasicGraphAuthentication"/> for the configured user name and password.</returns>
    BasicGraphAuthentication GetConfiguredAuthentication();

    /// <summary>
    /// Get the <see cref="IDriver"/> for the configured server and credentials.
    /// </summary>
    /// <returns>The shared <see cref="IDriver"/>.</returns>
    IDriver Create();

    /// <summary>
    /// Get the <see cref="IDriver"/> for a server and an identity given by an <see cref="IAuthTokenManager"/>.
    /// </summary>
    /// <param name="server">The URI of the server.</param>
    /// <param name="authentication">The <see cref="IAuthTokenManager"/> giving the identity. Drivers are shared by <paramref name="server"/> and the equality of this.</param>
    /// <returns>The shared <see cref="IDriver"/>.</returns>
    IDriver Create(Uri server, IAuthTokenManager authentication);
}
