// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the bearer token cache of one <see cref="HttpConnectionResolver"/>.
/// </summary>
/// <remarks>
/// A token is reused until <see cref="HttpConnectionResolverAuthenticationOptions.RefreshMargin"/> before it expires. Concurrent callers
/// needing a fresh token share one call to the provider; a caller that gives up does not cancel it for the others. Tokens are kept in
/// memory only.
/// </remarks>
/// <param name="provider">The <see cref="IConnectionResolverAccessTokenProvider"/> to get tokens from.</param>
/// <param name="options">The <see cref="HttpConnectionResolverAuthenticationOptions"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
sealed class ConnectionResolverAccessTokens(
    IConnectionResolverAccessTokenProvider provider,
    HttpConnectionResolverAuthenticationOptions options,
    TimeProvider timeProvider)
{
    readonly object _lock = new();
    ConnectionResolverAccessToken? _current;
    Task<ConnectionResolverAccessToken>? _fetching;

    /// <summary>
    /// Get a token that is not about to expire.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The token.</returns>
    public async ValueTask<string> Get(CancellationToken cancellationToken)
    {
        Task<ConnectionResolverAccessToken> fetching;
        lock (_lock)
        {
            if (_current is not null && timeProvider.GetUtcNow() < _current.ExpiresAt - options.RefreshMargin)
            {
                return _current.Token;
            }

            fetching = _fetching ?? Fetch();
            if (!fetching.IsCompleted)
            {
                _fetching = fetching;
            }
        }

        return (await fetching.WaitAsync(cancellationToken)).Token;
    }

    /// <summary>
    /// Drop a token the endpoint rejected, unless it was already replaced.
    /// </summary>
    /// <param name="token">The rejected token.</param>
    public void Invalidate(string token)
    {
        lock (_lock)
        {
            if (_current?.Token == token)
            {
                _current = null;
            }
        }
    }

    async Task<ConnectionResolverAccessToken> Fetch()
    {
        try
        {
            var fresh = await provider.GetToken(options, CancellationToken.None);
            lock (_lock)
            {
                _current = fresh;
            }

            return fresh;
        }
        finally
        {
            lock (_lock)
            {
                _fetching = null;
            }
        }
    }
}
