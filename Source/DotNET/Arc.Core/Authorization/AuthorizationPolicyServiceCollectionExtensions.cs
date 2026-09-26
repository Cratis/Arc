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
        return services.AddArcAuthorizationPolicy<TPolicy>(name, false);
    }

    /// <summary>
    /// Registers a named, scoped policy with an explicit anonymous-evaluation opt-in.
    /// </summary>
    /// <typeparam name="TPolicy">The policy implementation.</typeparam>
    /// <param name="services">The application's services.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="evaluatesAnonymous">Whether the policy evaluates unauthenticated callers.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddArcAuthorizationPolicy<TPolicy>(this IServiceCollection services, string name, bool evaluatesAnonymous)
        where TPolicy : class, IAuthorizationPolicy
    {
        services.AddScoped<TPolicy>();
        services.AddSingleton(new AuthorizationPolicyRegistration(name, typeof(TPolicy)) { EvaluatesAnonymous = evaluatesAnonymous });
        return services;
    }

    /// <summary>
    /// Opts an ASP.NET Core-registered policy into evaluation for unauthenticated callers.
    /// The policy must also be registered with ASP.NET Core authorization.
    /// </summary>
    /// <param name="services">The application's services.</param>
    /// <param name="name">The ASP.NET Core policy name.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddArcAnonymousAspNetAuthorizationPolicy(this IServiceCollection services, string name)
    {
        services.AddSingleton(new AnonymousAspNetAuthorizationPolicyRegistration(name));
        return services;
    }
}
