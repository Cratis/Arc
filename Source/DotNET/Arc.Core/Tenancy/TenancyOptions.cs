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
    /// Gets or sets the base domain the application is served from when using <see cref="TenantResolverType.Subdomain"/>.
    /// </summary>
    /// <remarks>
    /// A request host carries a tenant only when it is exactly one label in front of this value - <c>acme.myapp.com</c>
    /// for the base domain <c>myapp.com</c>. Every other host, including the base domain itself, falls back to
    /// <see cref="HttpHeader"/>. It is required for <see cref="TenantResolverType.Subdomain"/> and must be the
    /// registrable domain the application is served from; anything the resolver could not match a tenant host against
    /// - an empty value, a single label, an address literal - throws <see cref="BaseDomainIsNotADomainName"/> rather
    /// than leaving every request to resolve its tenant from the client-supplied header.
    /// </remarks>
    public string BaseDomain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the query string parameter to use for the tenant ID when using <see cref="TenantResolverType.Query"/>.
    /// </summary>
    public string QueryParameter { get; set; } = "tenantId";

    /// <summary>
    /// Gets or sets the claim type to use for the tenant ID when using <see cref="TenantResolverType.Claim"/>.
    /// </summary>
    public string ClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Gets or sets the tenant ID every request resolves to when using <see cref="TenantResolverType.Fixed"/> or
    /// <see cref="TenantResolverType.Development"/>.
    /// </summary>
    public string FixedTenantId { get; set; } = Constants.DefaultFixedTenantId;

    /// <summary>
    /// Gets or sets the fixed tenant ID to use when using <see cref="TenantResolverType.Development"/>.
    /// </summary>
    /// <remarks>
    /// This is <see cref="FixedTenantId"/> under its original name - both names read and write the same value, so
    /// configuration and code may use either. When a configuration source supplies both keys, the last one the
    /// binder visits wins; supply only one.
    /// </remarks>
    public string DevelopmentTenantId
    {
        get => FixedTenantId;
        set => FixedTenantId = value;
    }
}