// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Marks a clean Arc-owned provider, and prevents changing its identity after work has bound services to it.
/// </summary>
internal static class AuthorizationExecutionScopes
{
    static readonly AsyncLocal<OwnedFrame?> _current = new();

    /// <summary>
    /// Binds a selected identity to an Arc-owned provider before any identity-bound work begins.
    /// </summary>
    /// <param name="services">The execution provider.</param>
    /// <param name="principal">The selected principal.</param>
    /// <exception cref="InvalidAuthorizationConfiguration">The provider was supplied by the caller or already bound to another identity.</exception>
    internal static void Bind(IServiceProvider services, ClaimsPrincipal principal)
    {
        if (_current.Value is not { } frame || !ReferenceEquals(frame.Services, services))
        {
            throw new InvalidAuthorizationConfiguration("Scheme-selected execution requires an Arc-owned fresh service scope. Use the scope-owning pipeline entry point.");
        }

        frame.Bind(principal);
    }

    /// <summary>
    /// Records that this provider may now hold scoped collaborators bound to its current identity.
    /// </summary>
    /// <param name="services">The execution provider.</param>
    internal static void MarkWorkStarted(IServiceProvider services)
    {
        if (_current.Value is { } frame && ReferenceEquals(frame.Services, services))
        {
            frame.MarkWorkStarted();
        }
    }

    /// <summary>
    /// Marks a newly created scope for the duration of one execution.
    /// </summary>
    /// <param name="services">The newly created scope's provider.</param>
    /// <returns>A scope restoring the previous ownership frame.</returns>
    internal static IDisposable Begin(IServiceProvider services)
    {
        var frame = new OwnedFrame(services, _current.Value);
        _current.Value = frame;
        return frame;
    }

    sealed class OwnedFrame(IServiceProvider services, OwnedFrame? previous) : IDisposable
    {
        readonly object _sync = new();
        PrincipalSnapshot? _identity;
        bool _workStarted;

        public IServiceProvider Services { get; } = services;

        public void Bind(ClaimsPrincipal principal)
        {
            lock (_sync)
            {
                if (_identity is { } existing)
                {
                    if (!AuthorizationPrincipalIdentity.Same(existing, principal))
                    {
                        throw new InvalidAuthorizationConfiguration("This execution scope already holds services for a different identity.");
                    }
                }
                else if (_workStarted)
                {
                    throw new InvalidAuthorizationConfiguration("This execution scope resolved services before scheme authentication selected another identity.");
                }
                else
                {
                    _identity = AuthorizationPrincipalIdentity.Capture(principal);
                }
            }
        }

        public void MarkWorkStarted()
        {
            lock (_sync)
            {
                _workStarted = true;
            }
        }

        public void Dispose() => _current.Value = previous;
    }
}
