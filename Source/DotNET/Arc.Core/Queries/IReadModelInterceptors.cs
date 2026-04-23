// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a system for running discovered <see cref="IInterceptReadModel{TReadModel}"/> instances against a read model item.
/// </summary>
public interface IReadModelInterceptors
{
    /// <summary>
    /// Applies all discovered interceptors for the given read model type to a single item.
    /// </summary>
    /// <param name="readModelType">The type of the read model being intercepted.</param>
    /// <param name="item">The read model item to intercept.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to create interceptor instances and resolve their dependencies.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Intercept(Type readModelType, object item, IServiceProvider serviceProvider);
}
