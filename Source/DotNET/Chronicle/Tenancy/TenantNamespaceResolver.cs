// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceResolver"/> that uses the tenant ID as the namespace.
/// </summary>
/// <param name="tenantIdAccessor">The <see cref="ITenantIdAccessor"/> to use.</param>
public class TenantNamespaceResolver(ITenantIdAccessor tenantIdAccessor) : IEventStoreNamespaceResolver
{
    /// <inheritdoc/>
    /// <remarks>
    /// The default tenant — unset, or named <see cref="TenantId.Default"/> — maps to Chronicle's default
    /// namespace. Storage that partitions by tenant has to agree with this mapping; see
    /// <see cref="TenantId.IsDefault"/>.
    /// </remarks>
    public EventStoreNamespaceName Resolve()
    {
        var tenantId = tenantIdAccessor.Current;
        return tenantId.IsDefault
            ? EventStoreNamespaceName.Default
            : new EventStoreNamespaceName(tenantId.Value);
    }
}
