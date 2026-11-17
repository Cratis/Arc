// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdResolver"/> that resolves the tenant ID from a query string parameter.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/>.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/>.</param>
/// <remarks>
/// A parameter that is repeated reaches this resolver folded into one comma-separated value, exactly like a repeated header.
/// It is held to the same rule as <see cref="HeaderTenantIdResolver"/>: the tenant is resolved only when all remaining
/// values are equal, and conflicting or entirely empty values resolve to no tenant. A literal comma in a tenant ID supplied
/// this way is therefore treated as a separator.
/// </remarks>
[IgnoreConvention]
public class QueryTenantIdResolver(IHttpRequestContextAccessor httpRequestContextAccessor, IOptions<ArcOptions> options) : ITenantIdResolver
{
    /// <inheritdoc/>
    public string Resolve()
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return string.Empty;
        }

        var queryParameter = options.Value.Tenancy.QueryParameter;
        return context.Query.TryGetValue(queryParameter, out var tenantId) ? TenantHeader.Agreed(tenantId) : string.Empty;
    }
}
