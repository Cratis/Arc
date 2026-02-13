// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Extension methods for configuring tenancy in Arc.
/// </summary>
public static class TenancyOptionsExtensions
{
    /// <summary>
    /// Configure tenancy to use HTTP headers for resolving tenant IDs.
    /// </summary>
    /// <param name="options">The <see cref="ArcOptions"/> to configure.</param>
    /// <param name="headerName">Optional header name to use. Defaults to 'X-Tenant-ID'.</param>
    /// <returns>The <see cref="ArcOptions"/> for continuation.</returns>
    public static ArcOptions UseHeaderTenancy(this ArcOptions options, string? headerName = null)
    {
        options.Tenancy.ResolverType = TenantResolverType.Header;
        if (headerName is not null)
        {
            options.Tenancy.HttpHeader = headerName;
        }
        return options;
    }

    /// <summary>
    /// Configure tenancy to use query string parameters for resolving tenant IDs.
    /// </summary>
    /// <param name="options">The <see cref="ArcOptions"/> to configure.</param>
    /// <param name="queryParameter">Optional query parameter name to use. Defaults to 'tenantId'.</param>
    /// <returns>The <see cref="ArcOptions"/> for continuation.</returns>
    public static ArcOptions UseQueryTenancy(this ArcOptions options, string? queryParameter = null)
    {
        options.Tenancy.ResolverType = TenantResolverType.Query;
        if (queryParameter is not null)
        {
            options.Tenancy.QueryParameter = queryParameter;
        }
        return options;
    }

    /// <summary>
    /// Configure tenancy to use claims for resolving tenant IDs.
    /// </summary>
    /// <param name="options">The <see cref="ArcOptions"/> to configure.</param>
    /// <param name="claimType">Optional claim type to use. Defaults to 'tenant_id'.</param>
    /// <returns>The <see cref="ArcOptions"/> for continuation.</returns>
    public static ArcOptions UseClaimTenancy(this ArcOptions options, string? claimType = null)
    {
        options.Tenancy.ResolverType = TenantResolverType.Claim;
        if (claimType is not null)
        {
            options.Tenancy.ClaimType = claimType;
        }
        return options;
    }

    /// <summary>
    /// Configure tenancy to use a fixed tenant ID for development purposes.
    /// </summary>
    /// <param name="options">The <see cref="ArcOptions"/> to configure.</param>
    /// <param name="tenantId">Optional tenant ID to use. Defaults to 'development'.</param>
    /// <returns>The <see cref="ArcOptions"/> for continuation.</returns>
    public static ArcOptions UseDevelopmentTenancy(this ArcOptions options, string? tenantId = null)
    {
        options.Tenancy.ResolverType = TenantResolverType.Development;
        if (tenantId is not null)
        {
            options.Tenancy.DevelopmentTenantId = tenantId;
        }
        return options;
    }
}
