// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Defines a development provider for tenants.
/// </summary>
public interface ICanProvideTenants
{
    /// <summary>
    /// Provide tenants for development tooling.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tenants.</returns>
    Task<IEnumerable<Tenant>> Provide(CancellationToken cancellationToken = default);
}
