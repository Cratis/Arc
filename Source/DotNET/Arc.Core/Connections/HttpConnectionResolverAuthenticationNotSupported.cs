// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// The exception that is thrown when no <see cref="IConnectionResolverAccessTokenProvider"/> can provide tokens for the configured authentication.
/// </summary>
/// <param name="type">The configured <see cref="HttpConnectionResolverAuthenticationType"/>.</param>
public class HttpConnectionResolverAuthenticationNotSupported(HttpConnectionResolverAuthenticationType type)
    : Exception(type == HttpConnectionResolverAuthenticationType.AzureCredential
        ? $"{type} authentication for the HTTP connection resolver requires the Cratis.Arc.Connections.Azure package: call AddAzureConnectionResolverAuthentication() on the service collection"
        : $"No access token provider is registered for {type} authentication of the HTTP connection resolver");
