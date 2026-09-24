// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Registers host-neutral policies on either Arc host.
/// </summary>
public static class AuthorizationPolicyServiceCollectionExtensions
{
    /// <summary>
    /// Registers a named, scoped policy. Duplicate names fail at startup.
    /// </summary>
    /// <typeparam name="TPolicy">The policy implementation.</typeparam>
    /// <param name="services">The application's services.</param>
    /// <param name="name">The name used by an Authorize attribute.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddArcAuthorizationPolicy<TPolicy>(this IServiceCollection services, string name)
        where TPolicy : class, IAuthorizationPolicy
    {
        services.AddScoped<TPolicy>();
        services.AddSingleton(new AuthorizationPolicyRegistration(name, typeof(TPolicy)));
        return services;
    }
}
