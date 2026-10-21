// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Connections;

/// <summary>
/// The exception that is thrown when a token endpoint does not issue an access token for the HTTP connection resolver.
/// </summary>
/// <param name="tokenEndpoint">The token endpoint that was called.</param>
/// <param name="clientId">The client id the token was requested for.</param>
/// <param name="scope">The scope the token was requested for.</param>
/// <param name="statusCode">The <see cref="HttpStatusCode"/> it answered with.</param>
public class ConnectionResolverAccessTokenRequestFailed(Uri tokenEndpoint, string clientId, string? scope, HttpStatusCode statusCode)
    : Exception($"Requesting an access token from '{tokenEndpoint}' for client '{clientId}' and scope '{scope}' failed with status code {(int)statusCode} ({statusCode})");
