// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IReadModelInterceptors"/>.
/// </summary>
/// <remarks>
/// Discovers all registered <see cref="IInterceptReadModel{TReadModel}"/> implementations at startup
/// and applies them when interception is requested.
/// </remarks>
/// <param name="types"><see cref="ITypes"/> used to discover interceptor implementations.</param>
[Singleton]
public class ReadModelInterceptors(ITypes types) : IReadModelInterceptors
{
    readonly Dictionary<Type, InterceptorEntry> _cache = BuildCache(types);

    /// <inheritdoc/>
    public async Task Intercept(Type readModelType, object item, IServiceProvider serviceProvider)
    {
        if (!_cache.TryGetValue(readModelType, out var entry))
        {
            return;
        }

        foreach (var interceptorType in entry.InterceptorTypes)
        {
            var interceptor = serviceProvider.GetService(interceptorType);
            if (interceptor is null)
            {
                continue;
            }

            await (Task)entry.InterceptMethod.Invoke(interceptor, [item])!;
        }
    }

    static Dictionary<Type, InterceptorEntry> BuildCache(ITypes types)
    {
        var interceptorTypes = types.FindMultiple(typeof(IInterceptReadModel<>));
        var map = new Dictionary<Type, (List<Type> Types, MethodInfo? Method)>();

        foreach (var interceptorType in interceptorTypes)
        {
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
                entry = ([], interceptorInterface.GetMethod(nameof(IInterceptReadModel<object>.Intercept)));
                map[readModelType] = entry;
            }

            entry.Types.Add(interceptorType);
        }

        return map.ToDictionary(
            kvp => kvp.Key,
            kvp => new InterceptorEntry(kvp.Value.Types, kvp.Value.Method!));
    }

    readonly record struct InterceptorEntry(IReadOnlyList<Type> InterceptorTypes, MethodInfo InterceptMethod);
}
