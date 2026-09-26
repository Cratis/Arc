// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Restores a subscriber's identity before resolving any scoped emission collaborator on a producer's flow.
/// </summary>
internal static class ObservableEmissionIdentity
{
    /// <summary>Begins an operation-local identity boundary for one subscriber emission.</summary>
    /// <param name="context">The subscriber request snapshot.</param>
    /// <param name="services">The live emission provider.</param>
    /// <param name="principal">The subscriber principal.</param>
    /// <param name="tenant">The captured subscriber tenant.</param>
    /// <param name="capturedNativeRequest">The still-live direct request's isolated native scope factory.</param>
    /// <param name="accessor">An already-resolved Arc request accessor, if available.</param>
    /// <returns>The identity boundary to dispose after the emission.</returns>
    internal static IDisposable Begin(
        IHttpRequestContext context,
        IServiceProvider services,
        ClaimsPrincipal principal,
        TenantId? tenant,
        Func<ClaimsPrincipal, IServiceProvider, IDisposable?>? capturedNativeRequest = null,
        IHttpRequestContextAccessor? accessor = null)
    {
        var requestAccessor = accessor ?? services.GetRequiredService<IHttpRequestContextAccessor>();
        var previous = requestAccessor.Current;
        requestAccessor.Current = context;
        IDisposable? principalScope = null;
        IDisposable? nativeScope = null;
        var tenantBoundary = TenantIdAccessor.Independent.BeginEmission(tenant);
        try
        {
            var principals = services.GetService<CurrentPrincipalAccessor>();
            if (tenant is not null && principals is null)
            {
                throw new InvalidAuthorizationConfiguration("A tenant-bound emission requires the Arc principal accessor.");
            }

            principalScope = principals?.UseAuthorizationPrincipal(principal, services);
            if (services.GetService<IAuthorizationPolicyRuntime>() is IAuthorizationEmissionRuntime native)
            {
                nativeScope = capturedNativeRequest is not null
                    ? capturedNativeRequest(principal, services)
                    : native.BeginEmissionScope(principal, services, hasLiveRequest: false);
            }

            return new Scope(requestAccessor, previous, principalScope, nativeScope, tenantBoundary);
        }
        catch
        {
            nativeScope?.Dispose();
            principalScope?.Dispose();
            requestAccessor.Current = previous;
            tenantBoundary.Dispose();
            throw;
        }
    }

    sealed class Scope(
        IHttpRequestContextAccessor accessor,
        IHttpRequestContext? previous,
        IDisposable? principal,
        IDisposable? native,
        IDisposable tenantBoundary) : IDisposable
    {
        public void Dispose()
        {
            try
            {
                native?.Dispose();
                principal?.Dispose();
            }
            finally
            {
                accessor.Current = previous;
                tenantBoundary.Dispose();
            }
        }
    }
}
