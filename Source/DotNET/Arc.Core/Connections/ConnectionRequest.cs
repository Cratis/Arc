// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents a request for resolving a connection.
/// </summary>
/// <param name="Storage">The kind of storage to connect to, one of <see cref="WellKnownConnectionStorage"/>.</param>
/// <param name="Purpose">The <see cref="ConnectionPurpose"/> of the connection.</param>
/// <param name="Scope">The values identifying what is being connected, for instance the tenant.</param>
public record ConnectionRequest(string Storage, ConnectionPurpose Purpose, IReadOnlyDictionary<string, string> Scope)
{
    /// <summary>
    /// Creates a <see cref="ConnectionRequest"/> for a tenant.
    /// </summary>
    /// <param name="storage">The kind of storage to connect to.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> to connect for.</param>
    /// <returns>A new <see cref="ConnectionRequest"/>.</returns>
    /// <remarks>
    /// An unset tenant and the default tenant address the same data, so both are sent as <see cref="TenantId.Default"/>.
    /// </remarks>
    public static ConnectionRequest ForTenant(string storage, ConnectionPurpose purpose, TenantId tenantId) =>
        new(
            storage,
            purpose,
            new Dictionary<string, string>
            {
                [WellKnownConnectionScopeKeys.Tenant] = tenantId.IsDefault ? TenantId.Default.Value : tenantId.Value
            });

    /// <summary>
    /// Gets a key that identifies the request, independent of the order of the scope values.
    /// </summary>
    /// <returns>The key.</returns>
    public string ToKey() =>
        $"{Storage}|{Purpose}|{string.Join('|', Scope.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => $"{_.Key}={_.Value}"))}";
}
