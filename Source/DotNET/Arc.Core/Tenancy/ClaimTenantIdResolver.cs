// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdResolver"/> that resolves the tenant ID from a claim in the authenticated user.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/>.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/>.</param>
public class ClaimTenantIdResolver(IHttpRequestContextAccessor httpRequestContextAccessor, IOptions<ArcOptions> options) : ITenantIdResolver
{
    /// <inheritdoc/>
    public string Resolve()
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return string.Empty;
        }

        var claimType = options.Value.Tenancy.ClaimType;
        var claim = context.User?.FindFirst(claimType);
        return claim?.Value ?? string.Empty;
    }
}
