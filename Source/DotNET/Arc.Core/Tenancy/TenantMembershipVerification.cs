// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Verifies request tenant candidates using the current request's membership checks.
/// The <see cref="ITenantMembership"/> implementations are resolved once per request scope (when any are registered)
/// and reused, while membership is checked on every call.
/// </summary>
/// <param name="requestContexts">The current request accessor.</param>
/// <param name="principals">The effective principal accessor.</param>
/// <param name="logger">The logger.</param>
[IgnoreConvention]
internal sealed class TenantMembershipVerification(
    IHttpRequestContextAccessor requestContexts,
    ICurrentPrincipalAccessor principals,
    ILogger<TenantMembershipVerification> logger)
{
    static readonly ConditionalWeakTable<IServiceProvider, ITenantMembership[]> _resolved = new();

    /// <summary>Gets whether the request has membership checks.</summary>
    internal bool HasMembership => Memberships().Length > 0;

    /// <summary>Verifies a candidate against all request membership checks.</summary>
    /// <param name="tenant">The candidate tenant.</param>
    /// <returns>True when checks ran and allowed the candidate, false when no checks apply.</returns>
    /// <exception cref="TenantMembershipDenied">Membership was denied or could not be checked.</exception>
    internal bool Verify(TenantId tenant)
    {
        var memberships = Memberships();
        if (memberships.Length == 0)
        {
            return false;
        }

        var principal = principals.Current ?? new ClaimsPrincipal(new ClaimsIdentity());
        foreach (var membership in memberships)
        {
            bool allowed;
            try
            {
                allowed = membership.IsMember(principal, tenant);
            }
            catch (Exception exception)
            {
                logger.VerificationFailed(membership.GetType().FullName ?? membership.GetType().Name, exception);
                throw new TenantMembershipDenied();
            }

            if (!allowed)
            {
                logger.MembershipDenied(membership.GetType().FullName ?? membership.GetType().Name);
                throw new TenantMembershipDenied();
            }
        }

        return true;
    }

    ITenantMembership[] Memberships()
    {
        if (requestContexts.Current is not { } request)
        {
            return [];
        }

        var services = request.RequestServices;
        if (_resolved.TryGetValue(services, out var cached))
        {
            return cached;
        }

        try
        {
            var resolved = services.GetService(typeof(IEnumerable<ITenantMembership>));
            var memberships = resolved as ITenantMembership[] ?? (resolved as IEnumerable<ITenantMembership>)?.ToArray() ?? [];
            if (memberships.Length > 0)
            {
                _resolved.TryAdd(services, memberships);
            }

            return memberships;
        }
        catch (Exception exception)
        {
            logger.ResolutionFailed(exception);
            throw new TenantMembershipDenied();
        }
    }
}
