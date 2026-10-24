// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Represents an implementation of <see cref="IApplicationCalendar"/>.
/// </summary>
/// <param name="options">The <see cref="ArcOptions"/>.</param>
/// <param name="providers">The registered <see cref="ICanProvideTenantCalendar"/> providers.</param>
[Singleton]
public class ApplicationCalendar(IOptions<ArcOptions> options, IInstancesOf<ICanProvideTenantCalendar> providers) : IApplicationCalendar
{
    readonly Lazy<TimeZoneInfo> _applicationZone = new(() => ApplicationTimeZones.Resolve(options.Value.Calendar.TimeZone));

    /// <inheritdoc/>
    public async ValueTask<TimeZoneInfo> ResolveZone(TenantId tenant, TimeZoneInfo? operationZone, CancellationToken token)
    {
        if (operationZone is not null)
        {
            return operationZone;
        }

        foreach (var provider in providers)
        {
            var id = await provider.Provide(tenant, token);
            if (id is not null)
            {
                return ApplicationTimeZones.Resolve(id);
            }
        }

        return _applicationZone.Value;
    }

    /// <inheritdoc/>
    public async ValueTask<ClockReading> Read(DateTimeOffset instant, TenantId tenant, TimeZoneInfo? operationZone, CancellationToken token) =>
        new(instant, await ResolveZone(tenant, operationZone, token));
}
