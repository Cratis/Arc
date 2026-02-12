// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents the options for tenancy.
/// </summary>
public class TenancyOptions
{
    /// <summary>
    /// Gets or sets the type of tenant resolver to use.
    /// </summary>
    public TenantResolverType ResolverType { get; set; } = TenantResolverType.Header;

    /// <summary>
    /// Gets or sets the HTTP header to use for the tenant ID when using <see cref="TenantResolverType.Header"/>.
    /// </summary>
    public string HttpHeader { get; set; } = Constants.DefaultTenantIdHeader;

    /// <summary>
    /// Gets or sets the query string parameter to use for the tenant ID when using <see cref="TenantResolverType.Query"/>.
    /// </summary>
    public string QueryParameter { get; set; } = "tenantId";

    /// <summary>
    /// Gets or sets the claim type to use for the tenant ID when using <see cref="TenantResolverType.Claim"/>.
    /// </summary>
    public string ClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Gets or sets the fixed tenant ID to use when using <see cref="TenantResolverType.Development"/>.
    /// </summary>
    public string DevelopmentTenantId { get; set; } = "development";
}