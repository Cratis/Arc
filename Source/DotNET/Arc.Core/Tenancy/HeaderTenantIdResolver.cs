// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdResolver"/> that resolves the tenant ID from an HTTP header.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/>.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/>.</param>
/// <remarks>
/// Commas separate header values, including repeated header lines combined by the host. Each value is trimmed and
/// empty values are ignored. The tenant is resolved only when all remaining values are equal using ordinal comparison;
/// conflicting or entirely empty values resolve to no tenant. A literal comma in a header-supplied tenant ID is therefore
/// treated as a separator, not as part of the ID. Tenant IDs from other sources are unaffected.
/// Managed HttpListener can discard earlier repeated header lines before Arc sees them; this resolver cannot reject
/// conflicts it never receives. The HttpListener host warns by default, or refuses startup when
/// <see cref="TenancyOptions.RequireDuplicateHeaderPreservation"/> is enabled. A trusted ingress must strip all incoming
/// tenant-header occurrences and inject exactly one value, and direct backend access must be prevented.
/// </remarks>
[IgnoreConvention]
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
        return TenantHeader.Resolve(context.Headers, headerName);
    }
}
