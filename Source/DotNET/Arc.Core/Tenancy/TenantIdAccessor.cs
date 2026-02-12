// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdAccessor"/>.
/// </summary>
/// <param name="tenantIdResolver">The <see cref="ITenantIdResolver"/> to use for resolving tenant IDs.</param>
[Singleton]
public class TenantIdAccessor(ITenantIdResolver tenantIdResolver) : ITenantIdAccessor
{
    static readonly AsyncLocal<TenantId> _current = new();

    /// <inheritdoc/>
    public TenantId Current
    {
        get
        {
            if (_current.Value is not null)
            {
                return _current.Value;
            }

            var tenantId = tenantIdResolver.Resolve();
            var result = string.IsNullOrEmpty(tenantId) ? TenantId.NotSet : new TenantId(tenantId);
            _current.Value = result;
            return result;
        }
    }
}
