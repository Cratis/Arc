// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a contract for intercepting a read model before it is served to a client.
/// </summary>
/// <typeparam name="TReadModel">Type of read model to intercept.</typeparam>
/// <remarks>
/// Implement this interface to perform cross-cutting operations on read models,
/// such as decryption or other transformations, both for regular and observable queries.
/// </remarks>
public interface IInterceptReadModel<TReadModel>
{
    /// <summary>
    /// Intercepts the given read model, allowing mutation before it is served to the client.
    /// </summary>
    /// <param name="readModel">The read model instance to intercept.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Intercept(TReadModel readModel);
}
