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
    static readonly AsyncLocal<ClaimsPrincipal?> _authorizationPrincipal = new();

    /// <inheritdoc/>
    public ClaimsPrincipal? Current =>
        _authorizationPrincipal.Value ?? (httpRequestContextAccessor.Current is not null
            ? httpRequestContextAccessor.Current.User
            : _override.Value);

    /// <inheritdoc/>
    public IDisposable BeginScope(ClaimsPrincipal principal)
    {
        if (httpRequestContextAccessor.Current is not null)
        {
            return NoScope.Instance;
        }

        var previous = _override.Value;
        var previousAuthorizationPrincipal = _authorizationPrincipal.Value;
        _override.Value = principal;
        _authorizationPrincipal.Value = null;
        return new Scope(previous, previousAuthorizationPrincipal);
    }

    /// <summary>
    /// Uses a selected authenticated scheme principal only during a legacy authorization check.
    /// </summary>
    /// <param name="principal">The selected principal.</param>
    /// <param name="services">The fresh execution provider when a subscription replaces its request scope.</param>
    /// <returns>A scope restoring the previous authorization principal.</returns>
    internal IDisposable UseAuthorizationPrincipal(ClaimsPrincipal principal, IServiceProvider? services = null)
    {
        var previous = _authorizationPrincipal.Value;
        var request = httpRequestContextAccessor.Current;
        var isolatedRequest = request as IAuthorizationRequestContext;
        var writableRequest = isolatedRequest is null ? request : null;
        var previousRequestPrincipal = writableRequest?.User;
        IDisposable? subscriptionScope = null;
        try
        {
            subscriptionScope = isolatedRequest?.BeginSelectedPrincipal(principal, services ?? request!.RequestServices);
            _authorizationPrincipal.Value = principal;
            if (writableRequest is not null)
            {
                writableRequest.User = principal;
            }

            return new AuthorizationScope(previous, writableRequest, previousRequestPrincipal, subscriptionScope);
        }
        catch
        {
            subscriptionScope?.Dispose();
            _authorizationPrincipal.Value = previous;
            throw;
        }
    }

    sealed class AuthorizationScope(
        ClaimsPrincipal? previous,
        IHttpRequestContext? request,
        ClaimsPrincipal? previousRequestPrincipal,
        IDisposable? subscriptionScope) : IDisposable
    {
        public void Dispose()
        {
            if (request is not null)
            {
                request.User = previousRequestPrincipal ?? new ClaimsPrincipal();
            }

            subscriptionScope?.Dispose();
            _authorizationPrincipal.Value = previous;
        }
    }

    sealed class Scope(ClaimsPrincipal? previous, ClaimsPrincipal? previousAuthorizationPrincipal) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _authorizationPrincipal.Value = previousAuthorizationPrincipal;
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
