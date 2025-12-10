// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working with tenancy middleware.
/// </summary>
public static class TenancyMiddlewareExtensions
{
    /// <summary>
    /// Add tenancy middleware to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for building continuation.</returns>
    public static IServiceCollection AddTenancy(this IServiceCollection services)
    {
        services.AddTransient<TenantIdMiddleware>();
        return services;
    }
}
