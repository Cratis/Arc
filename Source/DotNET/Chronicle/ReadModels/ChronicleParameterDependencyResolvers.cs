// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Runtime.CompilerServices;
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
    static readonly ConditionalWeakTable<ITypes, Lazy<Type[]>> _discoveredTypes = new();

    /// <inheritdoc/>
    public IEnumerator<ICanResolveParameterDependency> GetEnumerator()
    {
        yield return serviceProvider.GetRequiredService<ChronicleParameterDependencyResolver>();

        var registered = serviceProvider.GetServices<ICanResolveParameterDependency>()
            .Concat(serviceProvider.GetServices<IAsyncParameterDependencyResolver>())
            .Where(_ => _ is not ChronicleParameterDependencyResolver)
            .DistinctBy(_ => _.GetType())
            .ToDictionary(_ => _.GetType());
        var discovered = serviceProvider.GetService<ITypes>() is { } types
            ? _discoveredTypes.GetValue(types, static types => new(() => Discover(types))).Value
            : [];
        foreach (var type in discovered)
        {
            yield return registered.Remove(type, out var resolver)
                ? resolver
                : ResolveDiscovered(type);
        }

        foreach (var resolver in registered.Values)
        {
            yield return resolver;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    static Type[] Discover(ITypes types) => types.FindMultiple<ICanResolveParameterDependency>()
        .Where(_ => !_.IsAbstract && !_.ContainsGenericParameters && _ != typeof(ChronicleParameterDependencyResolver))
        .Distinct()
        .ToArray();

    ICanResolveParameterDependency ResolveDiscovered(Type type)
    {
        try
        {
            return (ICanResolveParameterDependency)serviceProvider.GetRequiredService(type);
        }
        catch (InvalidOperationException failure)
        {
            throw new InvalidOperationException($"Parameter dependency resolver '{type.FullName}' could not be resolved. It must be registered with the service container, along with its dependencies.", failure);
        }
    }
}
