// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Sinks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> for knowing which named sink read models are stored in.
/// </summary>
public static class ReadModelSinksServiceCollectionExtensions
{
    /// <summary>
    /// Add <see cref="IReadModelSinks"/>, which storage integrations use to read a read model from its named sink.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>Calling it more than once registers it once.</remarks>
    public static IServiceCollection AddReadModelSinks(this IServiceCollection services)
    {
        services.TryAddSingleton<IReadModelSinks, ReadModelSinks>();
        return services;
    }

    /// <summary>
    /// Add an <see cref="IReadModelSinkNameProvider"/> that tells which named sink read model types are stored in.
    /// </summary>
    /// <typeparam name="TProvider">The type of <see cref="IReadModelSinkNameProvider"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="factory">The factory creating the provider.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>A provider type is registered once, however often this is called.</remarks>
    public static IServiceCollection AddReadModelSinkNameProvider<TProvider>(this IServiceCollection services, Func<IServiceProvider, TProvider> factory)
        where TProvider : class, IReadModelSinkNameProvider
    {
        services.AddReadModelSinks();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReadModelSinkNameProvider, TProvider>(factory));
        return services;
    }
}
