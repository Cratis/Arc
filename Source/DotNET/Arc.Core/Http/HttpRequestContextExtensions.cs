// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Execution;
using Cratis.Execution;

namespace Cratis.Arc.Http;

/// <summary>
/// Extension methods for handling correlation IDs in HTTP contexts.
/// </summary>
public static class HttpRequestContextExtensions
{
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
        CorrelationId correlationId;
        if (context.Headers.TryGetValue(options.HttpHeader, out var correlationIdString) &&
            Guid.TryParse(correlationIdString, out var correlationIdValue))
        {
            correlationId = new CorrelationId(correlationIdValue);
        }
        else
        {
            correlationId = correlationIdAccessor.Current != CorrelationId.NotSet
                ? correlationIdAccessor.Current
                : CorrelationId.New();
        }

        CorrelationIdAccessor.SetCurrent(correlationId);
        context.SetResponseHeader(options.HttpHeader, correlationId.Value.ToString());
    }
}
