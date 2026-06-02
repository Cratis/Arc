// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IReadModelInterceptors"/>.
/// </summary>
/// <remarks>
/// Discovers all <see cref="IInterceptReadModel{TReadModel}"/> implementations via <see cref="ITypes"/> at startup.
/// Interceptors can also be registered in the DI container as <see cref="IInterceptReadModel{TReadModel}"/> services.
/// </remarks>
/// <param name="types"><see cref="ITypes"/> used to discover interceptor implementations.</param>
[Singleton]
public class ReadModelInterceptors(ITypes types) : IReadModelInterceptors
{
    readonly InterceptorCache _cache = BuildCache(types);
    readonly ConcurrentDictionary<Type, IReadOnlyList<InterceptorEntry>> _entriesByReadModelType = new();
    readonly ConcurrentDictionary<Type, ServiceInterceptorEntry> _serviceEntriesByReadModelType = new();

    /// <inheritdoc/>
    public async Task<IEnumerable<object>> Intercept(Type readModelType, IEnumerable<object> items, IServiceProvider serviceProvider)
    {
        var entries = _entriesByReadModelType.GetOrAdd(readModelType, BuildEntriesFor);
        var serviceEntry = _serviceEntriesByReadModelType.GetOrAdd(readModelType, CreateServiceEntry);
        var serviceInterceptors = GetServiceInterceptors(serviceEntry, serviceProvider);
        if (entries.Count == 0 && serviceInterceptors.Count == 0)
        {
            return items;
        }

        return await Task.WhenAll(items.Select(item => InterceptItem(item, entries, serviceInterceptors, serviceEntry, serviceProvider)));
    }

    static InterceptorCache BuildCache(ITypes types)
    {
        var interceptorTypes = types.FindMultiple(typeof(IInterceptReadModel<>));
        var map = new Dictionary<Type, (List<Type> Types, MethodInfo? Method, PropertyInfo? ResultProperty)>();
        var openGenericInterceptorTypes = new List<Type>();

        foreach (var interceptorType in interceptorTypes)
        {
            if (interceptorType.IsGenericTypeDefinition)
            {
                openGenericInterceptorTypes.Add(interceptorType);
                continue;
            }

            var interceptorInterface = interceptorType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInterceptReadModel<>));

            if (interceptorInterface is null)
            {
                continue;
            }

            var readModelType = interceptorInterface.GetGenericArguments()[0];
            if (!map.TryGetValue(readModelType, out var entry))
            {
                var method = interceptorInterface.GetMethod(nameof(IInterceptReadModel<object>.Intercept));
                var resultProperty = method!.ReturnType.GetProperty("Result");
                entry = ([], method, resultProperty);
                map[readModelType] = entry;
            }

            entry.Types.Add(interceptorType);
        }

        var concreteInterceptors = map.ToDictionary(
            kvp => kvp.Key,
            kvp => new InterceptorEntry(kvp.Value.Types, kvp.Value.Method!, kvp.Value.ResultProperty!));

        return new(concreteInterceptors, openGenericInterceptorTypes);
    }

    static bool TryCreateOpenGenericEntry(Type openGenericInterceptorType, Type readModelType, out InterceptorEntry entry)
    {
        entry = default;

        if (!openGenericInterceptorType.IsGenericTypeDefinition ||
            openGenericInterceptorType.GetGenericArguments().Length != 1)
        {
            return false;
        }

        Type interceptorType;
        try
        {
            interceptorType = openGenericInterceptorType.MakeGenericType(readModelType);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var interceptorInterface = interceptorType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInterceptReadModel<>));

        if (interceptorInterface is null)
        {
            return false;
        }

        var method = interceptorInterface.GetMethod(nameof(IInterceptReadModel<object>.Intercept));
        var resultProperty = method!.ReturnType.GetProperty("Result");

        entry = new([interceptorType], method, resultProperty!);
        return true;
    }

    static ServiceInterceptorEntry CreateServiceEntry(Type readModelType)
    {
        var serviceType = typeof(IInterceptReadModel<>).MakeGenericType(readModelType);
        var enumerableServiceType = typeof(IEnumerable<>).MakeGenericType(serviceType);
        var method = serviceType.GetMethod(nameof(IInterceptReadModel<object>.Intercept));
        var resultProperty = method!.ReturnType.GetProperty("Result");

        return new(enumerableServiceType, method, resultProperty!);
    }

    static IReadOnlyList<object> GetServiceInterceptors(ServiceInterceptorEntry entry, IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(entry.EnumerableServiceType) is not System.Collections.IEnumerable interceptors)
        {
            return [];
        }

        return [.. interceptors.Cast<object>()];
    }

    IReadOnlyList<InterceptorEntry> BuildEntriesFor(Type readModelType)
    {
        var entries = new List<InterceptorEntry>();

        if (_cache.ConcreteInterceptors.TryGetValue(readModelType, out var concreteEntry))
        {
            entries.Add(concreteEntry);
        }

        foreach (var openGenericInterceptorType in _cache.OpenGenericInterceptorTypes)
        {
            if (TryCreateOpenGenericEntry(openGenericInterceptorType, readModelType, out var entry))
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    async Task<object> InterceptItem(
        object item,
        IEnumerable<InterceptorEntry> entries,
        IReadOnlyList<object> serviceInterceptors,
        ServiceInterceptorEntry serviceEntry,
        IServiceProvider serviceProvider)
    {
        var current = item;
        var invokedInterceptorTypes = new HashSet<Type>();

        foreach (var entry in entries)
        {
            foreach (var interceptorType in entry.InterceptorTypes)
            {
                var interceptor = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, interceptorType);
                var task = (Task)entry.InterceptMethod.Invoke(interceptor, [current])!;
                await task;
                current = entry.TaskResultProperty.GetValue(task)!;
                invokedInterceptorTypes.Add(interceptorType);
            }
        }

        foreach (var interceptor in serviceInterceptors.Where(_ => !invokedInterceptorTypes.Contains(_.GetType())))
        {
            var task = (Task)serviceEntry.InterceptMethod.Invoke(interceptor, [current])!;
            await task;
            current = serviceEntry.TaskResultProperty.GetValue(task)!;
        }

        return current;
    }

    readonly record struct InterceptorCache(
        IReadOnlyDictionary<Type, InterceptorEntry> ConcreteInterceptors,
        IReadOnlyList<Type> OpenGenericInterceptorTypes);

    readonly record struct InterceptorEntry(IReadOnlyList<Type> InterceptorTypes, MethodInfo InterceptMethod, PropertyInfo TaskResultProperty);

    readonly record struct ServiceInterceptorEntry(Type EnumerableServiceType, MethodInfo InterceptMethod, PropertyInfo TaskResultProperty);
}
