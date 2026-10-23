// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Represents what a clock-boundary operator needs for one subscription: the tenant, the zone resolved once, and the
/// cancellation of the subscription. Create it through <see cref="IClockBoundaries.Begin"/>.
/// </summary>
public sealed class ClockBoundaryContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClockBoundaryContext"/> class.
    /// </summary>
    /// <param name="tenant">The tenant.</param>
    /// <param name="zone">The resolved zone.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
    /// <param name="tenantScope">The <see cref="ITenantScope"/>.</param>
    /// <param name="changeSetComputor">The <see cref="ChangeSetComputor"/> used for result equivalence.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that ends the subscription.</param>
    internal ClockBoundaryContext(
        TenantId tenant,
        TimeZoneInfo zone,
        TimeProvider timeProvider,
        ITenantScope tenantScope,
        ChangeSetComputor changeSetComputor,
        CancellationToken cancellationToken)
    {
        Tenant = tenant;
        Zone = zone;
        CancellationToken = cancellationToken;
        TimeProvider = timeProvider;
        TenantScope = tenantScope;
        ChangeSetComputor = changeSetComputor;
    }

    /// <summary>
    /// Gets the tenant the subscription belongs to.
    /// </summary>
    public TenantId Tenant { get; }

    /// <summary>
    /// Gets the time zone resolved when the subscription began. It does not change for the subscription's lifetime.
    /// </summary>
    public TimeZoneInfo Zone { get; }

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> that ends the subscription.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the <see cref="TimeProvider"/> the clock is read from and timers are armed on.
    /// </summary>
    internal TimeProvider TimeProvider { get; }

    /// <summary>
    /// Gets the <see cref="ITenantScope"/> evaluations run inside.
    /// </summary>
    internal ITenantScope TenantScope { get; }

    /// <summary>
    /// Gets the <see cref="ChangeSetComputor"/> that decides whether a result changed.
    /// </summary>
    internal ChangeSetComputor ChangeSetComputor { get; }
}
