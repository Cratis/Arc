// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Defines a provider of the time zone of a tenant. Register implementations to give tenants their own calendar.
/// </summary>
public interface ICanProvideTenantCalendar
{
    /// <summary>
    /// Provides the IANA time zone id for a tenant.
    /// </summary>
    /// <param name="tenant">The tenant.</param>
    /// <param name="token">The <see cref="CancellationToken"/>.</param>
    /// <returns>The IANA id, or <see langword="null"/> when this provider has none for the tenant.</returns>
    ValueTask<string?> Provide(TenantId tenant, CancellationToken token);
}
