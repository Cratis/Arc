// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines an enumerable that is observed by a connected client.
/// </summary>
public interface IClientEnumerableObservable
{
    /// <summary>
    /// Handle the HTTP context for minimal API endpoints.
    /// </summary>
    /// <param name="httpContext"><see cref="HttpContext"/> to handle for.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleConnection(HttpContext httpContext);
}
