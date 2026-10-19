// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectionResolverFactory"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> custom resolvers are created through.</param>
[Singleton]
public class ConnectionResolverFactory(IServiceProvider serviceProvider) : IConnectionResolverFactory
{
    TimeProvider TimeProvider => serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

    /// <inheritdoc/>
    public ICachingConnectionResolver Create(ConnectionResolutionOptions options) =>
        new CachingConnectionResolver(
            new CompositeConnectionResolver([CreateResolver(options.Connection), CreateResolver(options.Identity)]),
            options.CacheDuration,
            TimeProvider);

    IConnectionResolver CreateResolver(ConnectionResolverOptions options) => options.Resolver switch
    {
        ConnectionResolverType.Template => new TemplateConnectionResolver(options.Template),
        ConnectionResolverType.Http => CreateHttp(options.Http),
        ConnectionResolverType.Custom => CreateCustom(options.CustomType),
        _ => new DefaultConnectionResolver()
    };

    HttpConnectionResolver CreateHttp(HttpConnectionResolverOptions options) =>
        new(
            options,
            ConnectionResolutionHttpClient.Instance,
            [.. serviceProvider.GetServices<IConnectionResolverAccessTokenProvider>(), new ClientCredentialsAccessTokenProvider(ConnectionResolutionHttpClient.Instance, TimeProvider)],
            TimeProvider);

    IConnectionResolver CreateCustom([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type)
    {
        if (type is null || !typeof(IConnectionResolver).IsAssignableFrom(type))
        {
            throw new CustomConnectionResolverNotConfigured(type);
        }

        return (IConnectionResolver)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type);
    }
}
