// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// A native scoped policy whose asynchronous verdict can be held while a real HTTP request is in flight.
/// </summary>
/// <param name="gate">The test-controlled release signal.</param>
/// <param name="probe">A collaborator that must resolve from the active request scope.</param>
/// <param name="native">The operation-local native HTTP context.</param>
/// <param name="underlying">The application's original unmodified HTTP accessor.</param>
public class GatedPolicy(PolicyGate gate, ScopedPolicyProbe probe, IHttpContextAccessor native, ScenarioHttpContextAccessor underlying) : IAuthorizationPolicy
{
    /// <inheritdoc/>
    public async ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
    {
        gate.Record(context.Principal.Identity?.Name, probe.Id);
        if (context.Target.DeclaringType == typeof(GatedSelectedReadModel))
        {
            gate.RecordNative(
                native.HttpContext?.User.Identity?.Name,
                underlying.HttpContext?.User.Identity?.Name,
                native.HttpContext?.RequestServices.GetRequiredService<TenantBoundService>().Tenant.Value,
                underlying.HttpContext?.RequestServices.GetRequiredService<TenantBoundService>().Tenant.Value);
        }
        gate.ObservePolicyToken(cancellationToken);
        try
        {
            await gate.Wait(cancellationToken);
            return context.Principal.Identity?.IsAuthenticated == true;
        }
        finally
        {
            gate.SignalExit();
        }
    }
}
