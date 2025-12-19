// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="IMiddleware"/> that sets the tenant ID for the request.
/// </summary>
/// <param name="options">The options for Arc.</param>
public class TenantIdMiddleware(IOptions<ArcOptions> options) : IMiddleware
{
    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tenantId = context.Request.Headers[options.Value.Tenancy.HttpHeader].ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(tenantId))
        {
            context.Items[Constants.TenantIdItemKey] = tenantId;
            TenantIdAccessor.SetCurrent(tenantId);
        }

        await next(context);
    }
}