// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdAccessor"/>.
/// </summary>
[Singleton]
public class TenantIdAccessor : ITenantIdAccessor
{
    static readonly AsyncLocal<TenantId> _current = new();

    /// <inheritdoc/>
    public TenantId Current
    {
        get
        {
            var current = _current.Value;
            CurrentTenantIdIsNotSet.ThrowIfNotSet(current);
            return current!;
        }
    }

    /// <summary>
    /// Internal: Set the current tenant ID.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> to set.</param>
    internal static void SetCurrent(TenantId tenantId) => _current.Value = tenantId;
}
