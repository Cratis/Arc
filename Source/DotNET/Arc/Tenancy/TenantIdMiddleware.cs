// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="IMiddleware"/> that sets the tenant ID for the request.
/// </summary>
/// <param name="tenantIdResolver">The <see cref="ITenantIdResolver"/> to use.</param>
public class TenantIdMiddleware(ITenantIdResolver tenantIdResolver) : IMiddleware
{
    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tenantId = tenantIdResolver.Resolve();
        if (!string.IsNullOrEmpty(tenantId))
        {
            context.Items[Constants.TenantIdItemKey] = tenantId;
            TenantIdAccessor.SetCurrent(tenantId);
        }

        await next(context);
    }
}