// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Invokes <see cref="IInterceptReadModel{TReadModel}"/> interceptors for a read model type that is only known at runtime.
/// </summary>
internal abstract class ReadModelInterceptorInvoker
{
    /// <summary>
    /// Gets the closed <see cref="IInterceptReadModel{TReadModel}"/> type for the read model.
    /// </summary>
    public abstract Type InterceptorInterface { get; }

    /// <summary>
    /// Gets the interceptors registered as <see cref="IInterceptReadModel{TReadModel}"/> services for the read model.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve the interceptors from.</param>
    /// <returns>The registered interceptors, or an empty list when there are none.</returns>
    public abstract IReadOnlyList<object> GetServiceInterceptors(IServiceProvider serviceProvider);

    /// <summary>
    /// Intercepts a read model with an interceptor.
    /// </summary>
    /// <param name="interceptor">The <see cref="IInterceptReadModel{TReadModel}"/> instance.</param>
    /// <param name="readModel">The read model to intercept.</param>
    /// <returns>The intercepted read model.</returns>
    public abstract Task<object> Intercept(object interceptor, object readModel);
}
