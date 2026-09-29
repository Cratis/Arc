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
    readonly Type[] _discoveredInterceptorTypes = [.. types.FindMultiple(typeof(IInterceptReadModel<>))];
    readonly ConcurrentDictionary<Type, Interception> _interceptionsByReadModelType = new();

    /// <inheritdoc/>
    public async Task<IEnumerable<object>> Intercept(Type readModelType, IEnumerable<object> items, IServiceProvider serviceProvider)
    {
        var interception = _interceptionsByReadModelType.GetOrAdd(readModelType, CreateInterception);
        var serviceInterceptors = interception.Invoker.GetServiceInterceptors(serviceProvider);
        if (interception.InterceptorTypes.Count == 0 && serviceInterceptors.Count == 0)
        {
            return items;
        }

        return await Task.WhenAll(items.Select(item => InterceptItem(item, interception, serviceInterceptors, serviceProvider)));
    }

    static bool TryCloseOpenGenericInterceptor(Type openGenericInterceptorType, Type readModelType, Type interceptorInterface, out Type interceptorType)
    {
        interceptorType = openGenericInterceptorType;

        if (openGenericInterceptorType.GetGenericArguments().Length != 1)
        {
            return false;
        }

        try
        {
            interceptorType = openGenericInterceptorType.MakeGenericType(readModelType);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return interceptorInterface.IsAssignableFrom(interceptorType);
    }

    static async Task<object> InterceptItem(
        object item,
        Interception interception,
        IReadOnlyList<object> serviceInterceptors,
        IServiceProvider serviceProvider)
    {
        var current = item;
        var invokedInterceptorTypes = new HashSet<Type>();

        foreach (var interceptorType in interception.InterceptorTypes)
        {
            var interceptor = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, interceptorType);
            current = await interception.Invoker.Intercept(interceptor, current);
            invokedInterceptorTypes.Add(interceptorType);
        }

        foreach (var interceptor in serviceInterceptors.Where(_ => !invokedInterceptorTypes.Contains(_.GetType())))
        {
            current = await interception.Invoker.Intercept(interceptor, current);
        }

        return current;
    }

    static ReadModelInterceptorInvokerFor<TReadModel> CreateInvoker<TReadModel>() => new();

    Interception CreateInterception(Type readModelType)
    {
        var invoker = (ReadModelInterceptorInvoker)typeof(ReadModelInterceptors)
            .GetMethod(nameof(CreateInvoker), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(readModelType)
            .Invoke(null, null)!;

        var interceptorTypes = new List<Type>();
        interceptorTypes.AddRange(_discoveredInterceptorTypes.Where(_ => !_.IsGenericTypeDefinition && invoker.InterceptorInterface.IsAssignableFrom(_)));

        foreach (var openGenericInterceptorType in _discoveredInterceptorTypes.Where(_ => _.IsGenericTypeDefinition))
        {
            if (TryCloseOpenGenericInterceptor(openGenericInterceptorType, readModelType, invoker.InterceptorInterface, out var interceptorType))
            {
                interceptorTypes.Add(interceptorType);
            }
        }

        return new(invoker, interceptorTypes);
    }

    readonly record struct Interception(ReadModelInterceptorInvoker Invoker, IReadOnlyList<Type> InterceptorTypes);
}
