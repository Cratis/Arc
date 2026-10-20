// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// The exception that is thrown when the HTTP connection resolver client credentials token endpoint does not use HTTPS, except HTTP on loopback hosts.
/// </summary>
public class ConnectionResolverTokenEndpointMustUseHttps()
    : Exception($"ClientCredentials authentication for the HTTP connection resolver requires '{nameof(HttpConnectionResolverAuthenticationOptions.TokenEndpoint)}' to use HTTPS; HTTP is allowed only on loopback hosts for local development");
