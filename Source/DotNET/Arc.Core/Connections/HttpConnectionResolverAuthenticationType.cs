// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Defines how <see cref="HttpConnectionResolver"/> authenticates to its endpoint.
/// </summary>
public enum HttpConnectionResolverAuthenticationType
{
    /// <summary>
    /// No authentication beyond any configured headers.
    /// </summary>
    None = 0,

    /// <summary>
    /// An OAuth 2.0 bearer token acquired with the client credentials grant from a token endpoint.
    /// </summary>
    ClientCredentials = 1,

    /// <summary>
    /// A Microsoft Entra ID bearer token acquired through an Azure credential, such as a managed identity.
    /// Requires the <c>Cratis.Arc.Connections.Azure</c> package.
    /// </summary>
    AzureCredential = 2
}
