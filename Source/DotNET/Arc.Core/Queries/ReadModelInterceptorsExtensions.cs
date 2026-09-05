// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.Queries;

/// <summary>
/// Extension methods for <see cref="IReadModelInterceptors"/> covering observable query emissions.
/// </summary>
public static class ReadModelInterceptorsExtensions
{
    /// <summary>
    /// Intercepts a single value emitted by an observable query, applying the registered
    /// <see cref="IInterceptReadModel{TReadModel}"/> interceptors per read model while preserving the emitted
    /// shape. Collection emissions (an <see cref="IEnumerable{T}"/> of read models) are intercepted element by
    /// element, keyed by the read model type rather than the enumerable type — without this, a collection query
    /// would look for a non-existent interceptor for the enumerable and silently skip compliance/PII release.
    /// </summary>
    /// <param name="interceptors">The <see cref="IReadModelInterceptors"/> to apply.</param>
    /// <param name="emittedType">The element type of the observable: the read model, or an enumerable of read models for collection queries.</param>
    /// <param name="value">The emitted value to intercept.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve interceptors.</param>
    /// <returns>The intercepted value, in the same shape (single value or enumerable) as the input.</returns>
    public static async Task<object> InterceptEmission(
        this IReadModelInterceptors interceptors,
        Type emittedType,
        object? value,
        IServiceProvider serviceProvider)
    {
        if (value is null)
        {
            return value!;
        }

        var readModelType = GetReadModelType(emittedType);

        if (value is IEnumerable enumerable and not string)
        {
            return await interceptors.Intercept(readModelType, enumerable.Cast<object>(), serviceProvider);
        }

        var intercepted = await interceptors.Intercept(readModelType, [value], serviceProvider);
        return intercepted.First();
    }

    static Type GetReadModelType(Type emittedType)
    {
        if (emittedType == typeof(string))
        {
            return emittedType;
        }

        var enumerableInterface = emittedType.IsInterface && emittedType.IsGenericType && emittedType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? emittedType
            : Array.Find(emittedType.GetInterfaces(), _ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0] ?? emittedType;
    }
}
