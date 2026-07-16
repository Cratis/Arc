// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents an implementation of <see cref="ICurrentPrincipalAccessor"/> and <see cref="ICurrentPrincipalOverride"/>
/// that resolves the current principal from the HTTP request when one is in progress, falling back to a server-side
/// override otherwise.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> used to detect an in-progress HTTP request and read its principal.</param>
public class CurrentPrincipalAccessor(IHttpRequestContextAccessor httpRequestContextAccessor) : ICurrentPrincipalAccessor, ICurrentPrincipalOverride
{
    static readonly AsyncLocal<ClaimsPrincipal?> _override = new();

    /// <inheritdoc/>
    public ClaimsPrincipal? Current =>
        httpRequestContextAccessor.Current is not null
            ? httpRequestContextAccessor.Current.User
            : _override.Value;

    /// <inheritdoc/>
    public IDisposable BeginScope(ClaimsPrincipal principal)
    {
        if (httpRequestContextAccessor.Current is not null)
        {
            return NoScope.Instance;
        }

        var previous = _override.Value;
        _override.Value = principal;
        return new Scope(previous);
    }

    sealed class Scope(ClaimsPrincipal? previous) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _override.Value = previous;
        }
    }

    sealed class NoScope : IDisposable
    {
        public static readonly NoScope Instance = new();

        public void Dispose()
        {
        }
    }
}
