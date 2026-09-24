// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Cratis.Arc.Queries.ControllerBased;

/// <summary>
/// Wires discovered controller performers without retaining a scoped evaluator at discovery.
/// </summary>
public static class QueryPerformerProviderRegistration
{
    /// <summary>
    /// Replaces the convention registration of controller performers with a scope-aware factory.
    /// </summary>
    /// <param name="services">The application's services.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddScopedControllerQueryAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<QueryPerformerProvider>(sp => new QueryPerformerProvider(
            sp.GetRequiredService<IActionDescriptorCollectionProvider>(),
            sp.GetRequiredService<IServiceProviderIsService>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<Func<IServiceProvider, IAuthorizationEvaluator>>()));
        var descriptor = services.FirstOrDefault(candidate => candidate.ServiceType == typeof(IQueryPerformerProvider) &&
            candidate.ImplementationType == typeof(QueryPerformerProvider));
        if (descriptor is not null)
        {
            services[services.IndexOf(descriptor)] = ServiceDescriptor.Singleton<IQueryPerformerProvider>(sp => sp.GetRequiredService<QueryPerformerProvider>());
        }

        return services;
    }
}
