// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Execution;
using Cratis.Execution;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods related to <see cref="CorrelationId"/> on <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextCorrelationIdExtensions
{
    /// <summary>
    /// Get the <see cref="CorrelationId"/> for an <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext"><see cref="HttpContext"/> to get for.</param>
    /// <returns>The <see cref="CorrelationId"/>.</returns>
    /// <remarks>
    /// If the correlation ID is not set, it will return an empty <see cref="CorrelationId"/>.
    /// </remarks>
    public static CorrelationId GetCorrelationId(this HttpContext httpContext)
    {
        var correlationIdFromItems = httpContext.Items.TryGetValue(Constants.CorrelationIdItemKey, out var correlationIdValue)
            ? correlationIdValue
            : null;

        if (TryGetCorrelationId(correlationIdFromItems, out var correlationId))
        {
            return correlationId;
        }

        if (TryGetCorrelationId(httpContext.Request.Headers[Constants.DefaultCorrelationIdHeader].ToString(), out correlationId))
        {
            return correlationId;
        }

        if (TryGetCorrelationId(httpContext.Response.Headers[Constants.DefaultCorrelationIdHeader].ToString(), out correlationId))
        {
            return correlationId;
        }

        return new CorrelationId(Guid.Empty);
    }

    /// <summary>
    /// Tries to create a <see cref="CorrelationId"/> from the provided value.
    /// </summary>
    /// <param name="value">The value that might represent a correlation ID.</param>
    /// <param name="correlationId">The parsed <see cref="CorrelationId"/> when successful.</param>
    /// <returns><see langword="true"/> if the value could be converted to a <see cref="CorrelationId"/>; otherwise <see langword="false"/>.</returns>
    static bool TryGetCorrelationId(object? value, out CorrelationId correlationId)
    {
        if (value is CorrelationId existingCorrelationId)
        {
            correlationId = existingCorrelationId;
            return true;
        }

        if (value is Guid guid)
        {
            correlationId = new CorrelationId(guid);
            return true;
        }

        if (Guid.TryParse(value?.ToString(), out var parsedGuid))
        {
            correlationId = new CorrelationId(parsedGuid);
            return true;
        }

        correlationId = CorrelationId.NotSet;
        return false;
    }
}
