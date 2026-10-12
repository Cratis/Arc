// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Testing;

/// <summary>
/// Adds the Arc core services a scenario runs on, keeping the authorization services the scenario registered itself.
/// </summary>
internal static class ScenarioServices
{
    static readonly Type[] _authorizationServices =
    [
        typeof(ICurrentPrincipalAccessor),
        typeof(ICurrentPrincipalOverride),
        typeof(IAuthorizationPolicyRuntime)
    ];

    /// <summary>
    /// Adds the Arc core services after the scenario's own registrations, so a caller, principal override or policy
    /// runtime the scenario registered is the one the run resolves rather than Arc's default.
    /// </summary>
    /// <param name="services">The scenario's <see cref="IServiceCollection"/>.</param>
    public static void AddCratisArcCoreKeepingAuthorization(this IServiceCollection services)
    {
        var explicitAuthorization = services.Where(_ => _authorizationServices.Contains(_.ServiceType)).ToArray();
        services.AddCratisArcCore();
        foreach (var type in explicitAuthorization.Select(_ => _.ServiceType).Distinct())
        {
            services.RemoveAll(type);
        }

        foreach (var registration in explicitAuthorization)
        {
            services.Add(registration);
        }
    }
}
