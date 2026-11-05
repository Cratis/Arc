// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="IGraphNamespaceResolver"/> that uses the tenant ID as the namespace.
/// </summary>
/// <remarks>
/// It maps tenants exactly as the Chronicle integration's tenant namespace resolver does: the default tenant — unset,
/// or named <see cref="TenantId.Default"/> — is the <see cref="GraphDatabaseName.DefaultNamespace"/>.
/// </remarks>
/// <param name="tenantIdAccessor">The <see cref="ITenantIdAccessor"/>.</param>
public class TenantGraphNamespaceResolver(ITenantIdAccessor tenantIdAccessor) : IGraphNamespaceResolver
{
    /// <inheritdoc/>
    public string Resolve()
    {
        var tenantId = tenantIdAccessor.Current;
        return tenantId.IsDefault ? GraphDatabaseName.DefaultNamespace : tenantId.Value;
    }
}
