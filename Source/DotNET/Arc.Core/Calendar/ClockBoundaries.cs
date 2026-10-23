// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Represents an implementation of <see cref="IClockBoundaries"/>.
/// </summary>
/// <param name="calendar">The <see cref="IApplicationCalendar"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> the operator arms its timers on.</param>
/// <param name="tenantScope">The <see cref="ITenantScope"/> the operator evaluates inside.</param>
/// <param name="options">The <see cref="ArcOptions"/>.</param>
[Singleton]
public class ClockBoundaries(
    IApplicationCalendar calendar,
    TimeProvider timeProvider,
    ITenantScope tenantScope,
    IOptions<ArcOptions> options) : IClockBoundaries
{
    readonly ChangeSetComputor _changeSetComputor = new(options.Value.JsonSerializerOptions);

    /// <inheritdoc/>
    public async ValueTask<ClockBoundaryContext> Begin(TenantId tenant, TimeZoneInfo? operationZone, CancellationToken token)
    {
        var zone = await calendar.ResolveZone(tenant, operationZone, token);
        return new ClockBoundaryContext(tenant, zone, timeProvider, tenantScope, _changeSetComputor, token);
    }
}
