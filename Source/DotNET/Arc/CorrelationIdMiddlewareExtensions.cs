// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Execution;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working with correlation ID middleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Add correlation ID middleware to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for building continuation.</returns>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddTransient<CorrelationIdMiddleware>();
        return services;
    }
}