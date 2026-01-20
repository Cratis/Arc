// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines an observable that is observed by a connected client.
/// </summary>
public interface IClientObservable
{
    /// <summary>
    /// Handle the HTTP request context.
    /// </summary>
    /// <param name="context"><see cref="IHttpRequestContext"/> to handle for.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleConnection(IHttpRequestContext context);

    /// <summary>
    /// Get an async enumerator for the observable.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Async enumerator.</returns>
    object GetAsynchronousEnumerator(CancellationToken cancellationToken = default);
}
