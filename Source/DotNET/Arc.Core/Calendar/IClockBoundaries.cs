// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Defines the service that begins a clock-boundary context for a subscription.
/// </summary>
public interface IClockBoundaries
{
    /// <summary>
    /// Begins a context for one subscription, resolving its time zone once.
    /// </summary>
    /// <param name="tenant">The tenant of the subscription.</param>
    /// <param name="operationZone">The zone the operation declares, if any.</param>
    /// <param name="token">The <see cref="CancellationToken"/> that ends the subscription.</param>
    /// <returns>The <see cref="ClockBoundaryContext"/>.</returns>
    ValueTask<ClockBoundaryContext> Begin(TenantId tenant, TimeZoneInfo? operationZone, CancellationToken token);
}
