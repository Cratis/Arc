// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// Defines where the audiences granted to a tenant, a user and the system come from.
/// </summary>
/// <remarks>
/// The engine knows nothing about how grants are stored. A product library implements this over its own
/// read models; <see cref="InMemoryAudienceSource"/> is a complete implementation for specifications.
/// </remarks>
public interface IAudienceSource
{
    /// <summary>
    /// Gets the audiences granted to the subject, at every level that applies to it.
    /// </summary>
    /// <param name="subject">The subject to get the grants for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The grants, tagged with their level. Empty when nothing is granted.</returns>
    ValueTask<IReadOnlyCollection<AudienceGrant>> GetGrants(AudienceSubject subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to changes in what is granted, which is the reactive hook for invalidating cached decisions.
    /// </summary>
    /// <param name="handler">Called whenever the grants of a tenant, user or the system change.</param>
    /// <returns>A subscription; dispose it to stop receiving changes.</returns>
    IDisposable OnChanged(Func<AudienceChange, ValueTask> handler);
}
