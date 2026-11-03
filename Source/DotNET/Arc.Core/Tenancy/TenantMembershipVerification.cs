// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Verifies request tenant candidates using the current request's membership checks.
/// </summary>
/// <remarks>
/// Hook instances are resolved once per logical context (one HTTP request or one observable-query subscription) and
/// reused only inside it. Isolation invariant: a resolved <see cref="ITenantMembership"/> instance is used only by checks
/// whose current <see cref="IHttpRequestContext"/> is the same object that resolved it and whose
/// <see cref="IHttpRequestContext.RequestServices"/> is the same provider instance it was resolved from. That provider must
/// still be alive. The principal and tenant are passed to every <see cref="ITenantMembership.IsMember"/> call and no allow
/// or deny decision is ever cached, so no instance crosses requests, subscriptions, connections, users or tenants. A check
/// through a disposed provider fails closed. The reuse is keyed by the context object, not by the provider (subscriptions
/// share the root provider) and not by <see cref="IHttpRequestContext.Items"/> (a subscription copies them). The context
/// must therefore represent one logical request; a host that pools context objects must not reuse one across requests.
/// </remarks>
/// <param name="requestContexts">The current request accessor.</param>
/// <param name="principals">The effective principal accessor.</param>
/// <param name="logger">The logger.</param>
[IgnoreConvention]
internal sealed class TenantMembershipVerification(
    IHttpRequestContextAccessor requestContexts,
    ICurrentPrincipalAccessor principals,
    ILogger<TenantMembershipVerification> logger)
{
    static readonly ConditionalWeakTable<IHttpRequestContext, ResolvedMemberships> _resolved = new();

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

        try
        {
            var services = request.RequestServices;
            if (_resolved.TryGetValue(request, out var resolved) && ReferenceEquals(resolved.Services, services))
            {
                // A disposed scope throws here, exactly where a fresh resolution would have thrown.
                _ = services.GetService(typeof(IServiceScopeFactory));
                return resolved.Memberships;
            }

            var result = services.GetService(typeof(IEnumerable<ITenantMembership>));
            var memberships = result as ITenantMembership[] ?? (result as IEnumerable<ITenantMembership>)?.ToArray() ?? [];
            if (memberships.Length > 0)
            {
                _resolved.AddOrUpdate(request, new ResolvedMemberships(services, memberships));
            }

            return memberships;
        }
        catch (Exception exception)
        {
            logger.ResolutionFailed(exception);
            throw new TenantMembershipDenied();
        }
    }

    sealed record ResolvedMemberships(IServiceProvider Services, ITenantMembership[] Memberships);
}
