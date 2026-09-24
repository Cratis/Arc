// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// A real emission guard observing a later streaming result under its selected scheme and tenant.
/// </summary>
/// <param name="observations">The test's callback signal.</param>
/// <param name="tenants">The ambient tenant accessor.</param>
/// <param name="principals">The ambient Arc principal accessor.</param>
/// <param name="native">The native HTTP accessor.</param>
public class SelectedEmissionGuard(
    SelectedEmissionObservations observations,
    ITenantIdAccessor tenants,
    ICurrentPrincipalAccessor principals,
    IHttpContextAccessor native) : IGuardObservableQueryEmission
{
    /// <inheritdoc/>
    public async Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
    {
        if (context.QueryName.Value != $"{typeof(Scenarios.for_Queries.ModelBound.PolicyProtectedStream).FullName}.Watch")
        {
            return ObservableQueryEmissionVerdict.Allow;
        }

        var bound = context.ServiceProvider.GetRequiredService<TenantBoundService>();
        var nativeBefore = native.HttpContext?.User.Identity?.Name;
        var nativeTenantBefore = native.HttpContext?.RequestServices.GetRequiredService<TenantBoundService>().Tenant.Value;
        await observations.WaitIfPaused();
        observations.Record(new EmissionObservation(
            context.Principal?.Identity?.Name,
            tenants.Current.Value,
            bound.Tenant.Value,
            principals.Current?.Identity?.Name,
            native.HttpContext?.User.Identity?.Name,
            native.HttpContext?.RequestServices.GetRequiredService<TenantBoundService>().Tenant.Value,
            nativeBefore,
            nativeTenantBefore));

        return ObservableQueryEmissionVerdict.Allow;
    }
}
