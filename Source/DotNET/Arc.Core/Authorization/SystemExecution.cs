// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents an implementation of <see cref="ISystemExecution"/> and <see cref="ISystemExecutionAccessor"/>
/// that manages the current server-side execution principal in an async local manner.
/// </summary>
public class SystemExecution : ISystemExecution, ISystemExecutionAccessor
{
    static readonly AsyncLocal<ClaimsPrincipal?> _current = new();

    /// <inheritdoc/>
    public ClaimsPrincipal? Current => _current.Value;

    /// <inheritdoc/>
    public IDisposable AsSystem(params string[] roles) => As(SystemPrincipal.WithRoles(roles));

    /// <inheritdoc/>
    public IDisposable As(ClaimsPrincipal principal)
    {
        var previous = _current.Value;
        _current.Value = principal;
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
            _current.Value = previous;
        }
    }
}
