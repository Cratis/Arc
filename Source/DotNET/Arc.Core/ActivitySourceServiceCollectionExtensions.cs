// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DiagnosticsActivitySource = System.Diagnostics.ActivitySource;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding Arc activity sources to an <see cref="IServiceCollection"/>.
/// </summary>
internal static class ActivitySourceServiceCollectionExtensions
{
    /// <summary>
    /// Adds the shared Arc <see cref="DiagnosticsActivitySource"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="sourceName">The activity source name.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddActivitySource(this IServiceCollection services, string sourceName)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        services.TryAddKeyedSingleton(sourceName, new DiagnosticsActivitySource(sourceName));
#pragma warning restore CA2000 // Dispose objects before losing scope
        return services;
    }

    /// <summary>
    /// Adds an <see cref="IActivitySource{TTarget}"/> backed by the shared Arc <see cref="DiagnosticsActivitySource"/>.
    /// </summary>
    /// <typeparam name="TTarget">The target type the activity source is for.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="sourceName">The activity source name.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddActivitySource<TTarget>(this IServiceCollection services, string sourceName)
    {
        services.TryAddSingleton<IActivitySource<TTarget>>(sp =>
            new ActivitySource<TTarget>(sp.GetRequiredKeyedService<DiagnosticsActivitySource>(sourceName)));

        return services;
    }
}
