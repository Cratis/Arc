// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http;

/// <summary>
/// Middleware for setting the tenant ID from HTTP headers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TenantIdMiddleware"/> class.
/// </remarks>
/// <param name="options">The options for Arc.</param>
/// <param name="logger">The logger for the middleware.</param>
public class TenantIdMiddleware(IOptions<ArcOptions> options, ILogger<TenantIdMiddleware> logger) : IHttpRequestMiddleware
{
    /// <inheritdoc/>
    public async Task<bool> InvokeAsync(HttpListenerContext context, HttpRequestDelegate next)
    {
        // Extract tenant ID from headers
        var tenantIdHeader = context.Request.Headers[options.Value.Tenancy.HttpHeader];
        if (!string.IsNullOrEmpty(tenantIdHeader))
        {
            logger.SettingTenantId(tenantIdHeader);
            TenantIdAccessor.SetCurrent(tenantIdHeader);
        }

        // Always call next middleware
        return await next(context);
    }
}
