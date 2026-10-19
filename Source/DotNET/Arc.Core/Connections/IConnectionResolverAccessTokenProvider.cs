// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Defines a provider of bearer tokens for <see cref="HttpConnectionResolver"/> to authenticate to its endpoint with.
/// </summary>
/// <remarks>
/// Register implementations in the service collection; the first one that can provide for the configured
/// <see cref="HttpConnectionResolverAuthenticationOptions"/> is used. Tokens are cached by the resolver, so a provider
/// is only called when a token is missing, about to expire or was rejected.
/// </remarks>
public interface IConnectionResolverAccessTokenProvider
{
    /// <summary>
    /// Check whether the provider can provide tokens for the given options.
    /// </summary>
    /// <param name="options">The <see cref="HttpConnectionResolverAuthenticationOptions"/>.</param>
    /// <returns>True if it can, false if not.</returns>
    bool CanProvide(HttpConnectionResolverAuthenticationOptions options);

    /// <summary>
    /// Get a fresh token.
    /// </summary>
    /// <param name="options">The <see cref="HttpConnectionResolverAuthenticationOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ConnectionResolverAccessToken"/>.</returns>
    ValueTask<ConnectionResolverAccessToken> GetToken(HttpConnectionResolverAuthenticationOptions options, CancellationToken cancellationToken);
}
