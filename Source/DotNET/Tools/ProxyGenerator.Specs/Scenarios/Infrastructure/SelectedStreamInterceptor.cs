// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>Observes the subscriber scope before a stream guard evaluates an emitted read model.</summary>
/// <param name="bound">The scoped tenant-bound dependency.</param>
/// <param name="principal">The Arc ambient principal.</param>
/// <param name="native">The native HTTP accessor.</param>
/// <param name="tenants">The Arc tenant accessor.</param>
/// <param name="observations">The interceptor observation signal.</param>
public class SelectedStreamInterceptor(
    TenantBoundService bound,
    ICurrentPrincipalAccessor principal,
    IHttpContextAccessor native,
    ITenantIdAccessor tenants,
    SelectedStreamInterceptorObservations observations) : IInterceptReadModel<PolicyProtectedStream>
{
    /// <inheritdoc/>
    public async Task<PolicyProtectedStream> Intercept(PolicyProtectedStream readModel)
    {
        await Task.Yield();
        observations.Record(new EmissionObservation(
            principal.Current?.Identity?.Name,
            tenants.Current.Value,
            bound.Tenant.Value,
            principal.Current?.Identity?.Name,
            native.HttpContext?.User.Identity?.Name,
            native.HttpContext?.RequestServices.GetRequiredService<TenantBoundService>().Tenant.Value));
        return readModel;
    }
}
