// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Execution;
using Cratis.Execution;

namespace Cratis.Arc.Http;

/// <summary>
/// Extension methods for handling correlation IDs in HTTP contexts.
/// </summary>
public static class HttpRequestContextExtensions
{
    /// <summary>
    /// The SSE content type value.
    /// </summary>
    public const string SseContentType = "text/event-stream";

    /// <summary>
    /// Handles correlation ID for the HTTP request context.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <param name="correlationIdAccessor">The <see cref="ICorrelationIdAccessor"/>.</param>
    /// <param name="options">The <see cref="CorrelationIdOptions"/>.</param>
    public static void HandleCorrelationId(
        this IHttpRequestContext context,
        ICorrelationIdAccessor correlationIdAccessor,
        CorrelationIdOptions options)
    {
        var correlationIdString = context.Headers.TryGetValue(options.HttpHeader, out var value)
            ? value
            : null;
        var correlationId = CorrelationIdResolver.ResolveOrCreate(correlationIdString, correlationIdAccessor);

        if (correlationIdAccessor is ICorrelationIdModifier correlationIdModifier)
        {
            correlationIdModifier.Modify(correlationId);
        }
        else
        {
            CorrelationIdAccessor.SetCurrent(correlationId);
        }

        context.SetResponseHeader(options.HttpHeader, correlationId.Value.ToString());
    }

    /// <summary>
    /// Sets the HTTP response status code.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> to set.</param>
    public static void SetStatusCode(this IHttpRequestContext context, HttpStatusCode statusCode)
    {
        context.SetStatusCode((int)statusCode);
    }

    /// <summary>
    /// Sets the standard SSE (Server-Sent Events) response headers on the context.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    public static void SetSseResponseHeaders(this IHttpRequestContext context)
    {
        context.ContentType = "text/event-stream; charset=utf-8";
        context.SetResponseHeader("Cache-Control", "no-cache");
        context.SetResponseHeader("Connection", "keep-alive");
        context.SetResponseHeader("X-Accel-Buffering", "no");
    }

    /// <summary>
    /// Prevents identity-bearing responses from being stored by caches.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <remarks>
    /// <c>Vary</c> is merged rather than replaced, so tokens set earlier in the pipeline (for example <c>Origin</c> from CORS)
    /// are kept. <c>Cookie</c> is added once, compared case-insensitively, and a <c>*</c> value is left as is since it
    /// already varies on everything.
    /// </remarks>
    internal static void SetNoStoreResponseHeaders(this IHttpRequestContext context)
    {
        context.SetResponseHeader("Cache-Control", "no-store, private");
        context.AddVaryResponseHeaderToken("Cookie");
    }

    static void AddVaryResponseHeaderToken(this IHttpRequestContext context, string token)
    {
        const string varyHeader = "Vary";
        var tokens = (context.GetResponseHeader(varyHeader) ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Any(existing => existing == "*" || existing.Equals(token, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        context.SetResponseHeader(varyHeader, string.Join(", ", tokens.Append(token)));
    }
}
