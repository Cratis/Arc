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
    /// <param name="headerName">Optional header name to use. Defaults to 'x-cratis-tenant-id'.</param>
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

    /// <summary>
    /// Configure tenancy to resolve every request to one fixed tenant ID.
    /// </summary>
    /// <param name="options">The <see cref="ArcOptions"/> to configure.</param>
    /// <param name="tenantId">Optional tenant ID to use. Defaults to 'development'.</param>
    /// <returns>The <see cref="ArcOptions"/> for continuation.</returns>
    /// <remarks>
    /// The tenant ID is returned regardless of the request or the hosting environment, which makes this the resolver
    /// for single tenant deployments. It is the same behavior as <see cref="UseDevelopmentTenancy"/> under a name that
    /// does not imply an environment - the tenant ID both configure is the same value.
    /// </remarks>
    public static ArcOptions UseFixedTenancy(this ArcOptions options, string? tenantId = null)
    {
        options.Tenancy.ResolverType = TenantResolverType.Fixed;
        if (tenantId is not null)
        {
            options.Tenancy.FixedTenantId = tenantId;
        }
        return options;
    }

    /// <summary>
    /// Configure tenancy to resolve the tenant ID from the request subdomain of a base domain, with the HTTP header
    /// as fallback.
    /// </summary>
    /// <param name="options">The <see cref="ArcOptions"/> to configure.</param>
    /// <param name="baseDomain">The base domain the application is served from, for instance 'myapp.com'.</param>
    /// <param name="fallbackHeaderName">Optional header name used as fallback. Defaults to 'x-cratis-tenant-id'.</param>
    /// <returns>The <see cref="ArcOptions"/> for continuation.</returns>
    /// <exception cref="BaseDomainIsNotADomainName">
    /// Thrown when <paramref name="baseDomain"/> is not a domain name tenants can be resolved in front of.
    /// </exception>
    /// <remarks>
    /// A host resolves a tenant only when it is exactly one label in front of <paramref name="baseDomain"/> -
    /// <c>acme.myapp.com</c> resolves <c>acme</c> for the base domain <c>myapp.com</c>. The base domain itself, a
    /// deeper host, an IP literal and any other host fall back to <paramref name="fallbackHeaderName"/>. The base
    /// domain is required, and must be the registrable domain the application is served from - a bare top level domain
    /// such as <c>com</c> would make every host on the internet a tenant.
    /// </remarks>
    public static ArcOptions UseSubdomainTenancy(this ArcOptions options, string baseDomain, string? fallbackHeaderName = null)
    {
        BaseDomainIsNotADomainName.ThrowIfNotADomainName(baseDomain);
        options.Tenancy.ResolverType = TenantResolverType.Subdomain;
        options.Tenancy.BaseDomain = baseDomain;
        if (fallbackHeaderName is not null)
        {
            options.Tenancy.HttpHeader = fallbackHeaderName;
        }
        return options;
    }
}
