// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// The exception that is thrown when the HTTP connection resolver is used without an endpoint.
/// </summary>
public class HttpConnectionResolverEndpointNotConfigured()
    : Exception($"The HTTP connection resolver requires an endpoint. Set '{nameof(HttpConnectionResolverOptions.Endpoint)}' in the '{nameof(ConnectionResolverOptions.Http)}' options");
