// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdResolver"/> that resolves the tenant ID from an HTTP header.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/>.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/>.</param>
public class HeaderTenantIdResolver(IHttpRequestContextAccessor httpRequestContextAccessor, IOptions<ArcOptions> options) : ITenantIdResolver
{
    /// <inheritdoc/>
    public string Resolve()
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return string.Empty;
        }

        var headerName = options.Value.Tenancy.HttpHeader;
        return context.Headers.TryGetValue(headerName, out var tenantId) ? tenantId : string.Empty;
    }
}
