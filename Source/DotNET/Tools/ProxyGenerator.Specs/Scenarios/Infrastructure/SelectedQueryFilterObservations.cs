// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Captures the tenant bound to an ordinary query filter after authorization.
/// </summary>
public class SelectedQueryFilterObservations
{
    /// <summary>
    /// Gets the tenant observed by the latest selected-scheme query filter.
    /// </summary>
    public string? Tenant { get; private set; }

    /// <summary>
    /// Resets the observation before a request.
    /// </summary>
    public void Reset() => Tenant = null;

    /// <summary>
    /// Records the tenant bound to the filter's scoped dependency.
    /// </summary>
    /// <param name="tenant">The tenant.</param>
    public void Record(string tenant) => Tenant = tenant;
}
