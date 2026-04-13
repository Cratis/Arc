// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Execution;

/// <summary>
/// Provides helper methods for handling correlation IDs.
/// </summary>
public static class CorrelationIdHelpers
{
    /// <summary>
    /// Handles setting the correlation ID for the given HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="correlationIdAccessor">The accessor for the correlation ID.</param>
    /// <param name="options">The options for the correlation ID.</param>
    public static void HandleCorrelationId(this HttpContext httpContext, ICorrelationIdAccessor correlationIdAccessor, CorrelationIdOptions options)
    {
        var correlationIdAsString = httpContext.Request.Headers[options.HttpHeader].ToString();
        var setCurrent = CorrelationIdResolver.TryGet(correlationIdAsString, out var correlationId);
        if (!setCurrent)
        {
            correlationId = CorrelationIdResolver.ResolveOrCreate(null, correlationIdAccessor);
            setCurrent = correlationIdAccessor.Current == CorrelationId.NotSet;

            correlationIdAsString = correlationId.ToString();
            httpContext.Request.Headers[options.HttpHeader] = correlationIdAsString;
        }

        httpContext.Response.Headers[options.HttpHeader] = correlationIdAsString;
        httpContext.Items[Constants.CorrelationIdItemKey] = correlationId;

        if (setCurrent)
        {
            if (correlationIdAccessor is ICorrelationIdModifier correlationIdModifier)
            {
                correlationIdModifier.Modify(correlationId);
            }
            else
            {
                CorrelationIdAccessor.SetCurrent(correlationId);
            }
        }
    }
}
