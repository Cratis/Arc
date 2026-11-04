// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.EntityFrameworkCore.Connections;

/// <summary>
/// Represents the ambient tenant that connections opened on the current execution flow are resolved for,
/// taking precedence over the <see cref="ITenantIdAccessor"/>.
/// </summary>
static class ConnectionTenantScope
{
    static readonly AsyncLocal<TenantId?> _current = new();

    /// <summary>
    /// Gets the tenant for the current execution flow, if a scope is active.
    /// </summary>
    internal static TenantId? Current => _current.Value;

    /// <summary>
    /// Begin a scope where connections are resolved for a tenant.
    /// </summary>
    /// <param name="tenant">The <see cref="TenantId"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous tenant when disposed.</returns>
    internal static IDisposable Begin(TenantId tenant)
    {
        var previous = _current.Value;
        _current.Value = tenant;
        return new Scope(previous);
    }

    sealed class Scope(TenantId? previous) : IDisposable
    {
        public void Dispose() => _current.Value = previous;
    }
}
