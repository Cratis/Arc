// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Arc.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Consults Chronicle before application parameter resolvers, resolving applications from the current scope.
/// </summary>
/// <param name="serviceProvider">The services for the current invocation.</param>
internal sealed class ChronicleParameterDependencyResolvers(IServiceProvider serviceProvider) : IInstancesOf<ICanResolveParameterDependency>
{
    /// <inheritdoc/>
    public IEnumerator<ICanResolveParameterDependency> GetEnumerator()
    {
        yield return serviceProvider.GetRequiredService<ChronicleParameterDependencyResolver>();

        var registered = serviceProvider.GetServices<ICanResolveParameterDependency>()
            .Concat(serviceProvider.GetServices<IAsyncParameterDependencyResolver>())
            .Where(_ => _ is not ChronicleParameterDependencyResolver)
            .DistinctBy(_ => _.GetType())
            .ToDictionary(_ => _.GetType());
        var discovered = serviceProvider.GetService<ITypes>()?.FindMultiple<ICanResolveParameterDependency>() ?? [];
        foreach (var type in discovered.Distinct().Where(_ => _ != typeof(ChronicleParameterDependencyResolver)))
        {
            yield return registered.Remove(type, out var resolver)
                ? resolver
                : (ICanResolveParameterDependency)serviceProvider.GetRequiredService(type);
        }

        foreach (var resolver in registered.Values)
        {
            yield return resolver;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
