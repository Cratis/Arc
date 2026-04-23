// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a system for running discovered <see cref="IInterceptReadModel{TReadModel}"/> instances against read model items.
/// </summary>
public interface IReadModelInterceptors
{
    /// <summary>
    /// Applies all discovered interceptors for the given read model type to every item in the collection
    /// and returns the resulting (potentially replaced) items.
    /// </summary>
    /// <param name="readModelType">The type of the read model being intercepted.</param>
    /// <param name="items">The read model items to intercept.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to create interceptor instances and resolve their dependencies.</param>
    /// <returns>A <see cref="Task{T}"/> resolving to the intercepted items, which may be new instances.</returns>
    Task<IEnumerable<object>> Intercept(Type readModelType, IEnumerable<object> items, IServiceProvider serviceProvider);
}
