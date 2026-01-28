// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http;

/// <summary>
/// Defines a pipeline for handling HTTP requests through a chain of middlewares.
/// </summary>
public interface IHttpRequestPipeline
{
    /// <summary>
    /// Process an HTTP request through the middleware pipeline.
    /// </summary>
    /// <param name="context">The HTTP listener context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ProcessAsync(HttpListenerContext context);
}
