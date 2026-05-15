// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdResolver"/> that resolves the tenant ID from the request
/// subdomain, with the configured HTTP header as fallback.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/>.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/>.</param>
[IgnoreConvention]
public class SubdomainTenantIdResolver(IHttpRequestContextAccessor httpRequestContextAccessor, IOptions<ArcOptions> options) : ITenantIdResolver
{
    /// <inheritdoc/>
    public string Resolve()
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return string.Empty;
        }

        var host = context.Host;
        if (!string.IsNullOrWhiteSpace(host))
        {
            var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length > 1)
            {
                return segments[0];
            }
        }

        var headerName = options.Value.Tenancy.HttpHeader;
        if (string.IsNullOrWhiteSpace(headerName))
        {
            return string.Empty;
        }

        return context.Headers.TryGetValue(headerName, out var tenantId) ? tenantId : string.Empty;
    }
}
