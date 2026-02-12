// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Implementation of <see cref="IHttpRequestPipeline"/> that processes HTTP requests through a chain of middlewares.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HttpRequestPipeline"/> class.
/// </remarks>
/// <param name="middlewares">The ordered collection of middlewares to execute.</param>
/// <param name="logger">The logger for the pipeline.</param>
public class HttpRequestPipeline(IEnumerable<IHttpRequestMiddleware> middlewares, ILogger<HttpRequestPipeline> logger) : IHttpRequestPipeline
{
    readonly IList<IHttpRequestMiddleware> _middlewares = middlewares.ToList();

    /// <inheritdoc/>
    public async Task ProcessAsync(HttpListenerContext context)
    {
        try
        {
            var handled = await BuildPipeline(0)(context);

            if (!handled)
            {
                logger.RequestNotHandled(context.Request.Url?.AbsolutePath ?? "/");
                context.Response.StatusCode = 404;
                var buffer = System.Text.Encoding.UTF8.GetBytes("Not Found");
                await context.Response.OutputStream.WriteAsync(buffer);
            }
        }
        finally
        {
            // Ensure response is closed unless it's a WebSocket request
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.Close();
            }
        }
    }

    HttpRequestDelegate BuildPipeline(int index)
    {
        if (index >= _middlewares.Count)
        {
            return _ => Task.FromResult(false);
        }

        var next = BuildPipeline(index + 1);
        var middleware = _middlewares[index];

        return context => middleware.InvokeAsync(context, next);
    }
}
