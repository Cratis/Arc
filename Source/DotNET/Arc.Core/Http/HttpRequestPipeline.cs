// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Authorization;
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
    const int MaxDrainBytes = 64 * 1024;
    static readonly TimeSpan _maxDrainTime = TimeSpan.FromSeconds(5);

    readonly List<IHttpRequestMiddleware> _middlewares = middlewares.ToList();

    /// <inheritdoc/>
    public async Task ProcessAsync(HttpListenerContext context)
    {
        // The request stream must exist before any response is submitted, otherwise it cannot be drained afterwards.
        var body = context.Request.IsWebSocketRequest || !context.Request.HasEntityBody ? null : context.Request.InputStream;
        try
        {
            var handled = await BuildPipeline(0)(context);
            if (context.Request.IsWebSocketRequest && context.Response.StatusCode == 403)
            {
                context.Response.Close();
            }

            if (!handled)
            {
                logger.RequestNotHandled(context.Request.Url?.AbsolutePath ?? "/");
                context.Response.StatusCode = 404;
                var buffer = System.Text.Encoding.UTF8.GetBytes("Not Found");
                await context.Response.OutputStream.WriteAsync(buffer);
            }
        }
        catch (Exception exception) when (UnauthorizedCause.IsCauseOf(exception))
        {
            try
            {
                // Set the status before closing the response.
                context.Response.StatusCode = 403;
                if (context.Request.IsWebSocketRequest)
                {
                    context.Response.Close();
                }
            }
            catch (InvalidOperationException)
            {
                // A committed or disconnected response cannot be replaced.
            }
        }
        finally
        {
            // Ensure response is closed unless it's a WebSocket request
            if (!context.Request.IsWebSocketRequest)
            {
                await CloseResponse(context, body);
            }
        }
    }

    static async Task CloseResponse(HttpListenerContext context, Stream? body)
    {
        // A request rejected before its body was read must not leave unread bytes on a reused connection.
        var drained = await TryDrainRequestBody(body);
        if (drained)
        {
            context.Response.Close();
            return;
        }

        context.Response.Abort();
    }

    static async Task<bool> TryDrainRequestBody(Stream? body)
    {
        try
        {
            if (body is null)
            {
                return true;
            }

            using var cancellation = new CancellationTokenSource(_maxDrainTime);
            var buffer = new byte[4096];
            var total = 0;
            while (total <= MaxDrainBytes)
            {
                var read = await body.ReadAsync(buffer, cancellation.Token).AsTask().WaitAsync(cancellation.Token);
                if (read == 0)
                {
                    return true;
                }

                total += read;
            }

            return false;
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or ObjectDisposedException or HttpListenerException or InvalidOperationException)
        {
            return false;
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
