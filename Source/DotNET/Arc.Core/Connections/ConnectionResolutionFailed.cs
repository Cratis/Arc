// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Connections;

/// <summary>
/// The exception that is thrown when an HTTP connection resolver endpoint does not answer successfully.
/// </summary>
/// <param name="endpoint">The endpoint that was called.</param>
/// <param name="statusCode">The <see cref="HttpStatusCode"/> it answered with.</param>
public class ConnectionResolutionFailed(Uri endpoint, HttpStatusCode statusCode)
    : Exception($"Resolving a connection through '{endpoint}' failed with status code {(int)statusCode} ({statusCode})");
