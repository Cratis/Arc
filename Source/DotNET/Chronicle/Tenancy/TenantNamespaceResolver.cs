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
    public EventStoreNamespaceName Resolve()
    {
        var tenantId = tenantIdAccessor.Current;
        return tenantId == TenantId.NotSet
            ? EventStoreNamespaceName.Default
            : new EventStoreNamespaceName(tenantId.Value);
    }
}
