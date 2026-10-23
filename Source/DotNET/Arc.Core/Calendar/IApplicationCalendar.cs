// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Defines the application calendar: it resolves the time zone that applies and pairs it with an instant.
/// </summary>
/// <remarks>
/// The zone is the operation's own, else the tenant's from an <see cref="ICanProvideTenantCalendar"/>, else the
/// application's, else UTC. Reading never consults the clock; the caller supplies the instant.
/// </remarks>
public interface IApplicationCalendar
{
    /// <summary>
    /// Resolves the time zone that applies.
    /// </summary>
    /// <param name="tenant">The tenant.</param>
    /// <param name="operationZone">The zone the operation declares, if any.</param>
    /// <param name="token">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="TimeZoneInfo"/>.</returns>
    /// <exception cref="TimeZoneIsNotAnIanaZone">Thrown when a tenant provider returns an id that cannot be used.</exception>
    ValueTask<TimeZoneInfo> ResolveZone(TenantId tenant, TimeZoneInfo? operationZone, CancellationToken token);

    /// <summary>
    /// Reads an instant in the zone that applies.
    /// </summary>
    /// <param name="instant">The instant to read.</param>
    /// <param name="tenant">The tenant.</param>
    /// <param name="operationZone">The zone the operation declares, if any.</param>
    /// <param name="token">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ClockReading"/>.</returns>
    ValueTask<ClockReading> Read(DateTimeOffset instant, TenantId tenant, TimeZoneInfo? operationZone, CancellationToken token);
}
