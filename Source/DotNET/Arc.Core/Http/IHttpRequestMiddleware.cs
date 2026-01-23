// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http;

/// <summary>
/// Defines a delegate for HTTP request middleware.
/// </summary>
/// <param name="context">The HTTP listener context.</param>
/// <returns>A task that represents the asynchronous operation, returning true if the request was handled, false otherwise.</returns>
public delegate Task<bool> HttpRequestDelegate(HttpListenerContext context);

/// <summary>
/// Defines a middleware component in the HTTP request pipeline.
/// </summary>
public interface IHttpRequestMiddleware
{
    /// <summary>
    /// Process an HTTP request.
    /// </summary>
    /// <param name="context">The HTTP listener context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the request was handled, false otherwise.</returns>
    Task<bool> InvokeAsync(HttpListenerContext context, HttpRequestDelegate next);
}
