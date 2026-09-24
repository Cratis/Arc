// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Prevents a nested command from changing identity while it would join the outer command's transaction.
/// </summary>
internal static class AuthorizationCommandIdentity
{
    static readonly AsyncLocal<Frame?> _current = new();

    /// <summary>
    /// Enters a command frame before context providers and execution scopes run.
    /// </summary>
    /// <param name="principal">The selected or ambient identity.</param>
    /// <param name="host">The pipeline scope factory identifying one Arc host across nested executions.</param>
    /// <returns>A frame restoring the parent identity after completion.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">A same-host nested command would change an active transaction's identity.</exception>
    internal static IDisposable Enter(ClaimsPrincipal? principal, object? host = null)
    {
        var previous = _current.Value;
        if (previous?.IsTransactional == true && ReferenceEquals(previous.Host, host) &&
            !AuthorizationPrincipalIdentity.Same(previous.Principal, principal))
        {
            throw new InvalidAuthorizationConfiguration("A nested command cannot change the identity of an active command transaction.");
        }

        var frame = new Frame(previous, host, AuthorizationPrincipalIdentity.Capture(principal));
        _current.Value = frame;
        return frame;
    }

    /// <summary>
    /// Marks that this command has an execution scope capable of owning a transaction.
    /// </summary>
    internal static void MarkTransactional()
    {
        if (_current.Value is { } current)
        {
            current.IsTransactional = true;
        }
    }

    sealed class Frame(Frame? previous, object? host, PrincipalSnapshot principal) : IDisposable
    {
        public object? Host { get; } = host;

        public PrincipalSnapshot Principal { get; } = principal;

        public bool IsTransactional { get; set; }

        public void Dispose() => _current.Value = previous;
    }
}
