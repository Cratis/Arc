// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.given;

public class a_subdomain_tenant_id_resolver : Specification
{
    protected const string BaseDomain = "myapp.com";
    protected const string FallbackHeaderName = "X-Tenant-Id";
    protected const string HeaderTenantId = "header-tenant";

    protected SubdomainTenantIdResolver _resolver;
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IOptions<ArcOptions> _options;
    protected IHttpRequestContext _context;
    protected Dictionary<string, string> _headers;
    protected ArcOptions _arcOptions;

    void Establish()
    {
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _options = Substitute.For<IOptions<ArcOptions>>();
        _context = Substitute.For<IHttpRequestContext>();
        _headers = new() { [FallbackHeaderName] = HeaderTenantId };

        _arcOptions = new ArcOptions();
        _arcOptions.UseSubdomainTenancy(BaseDomain, FallbackHeaderName);
        _options.Value.Returns(_arcOptions);

        _context.Headers.Returns(_headers);
        _httpRequestContextAccessor.Current.Returns(_context);

        _resolver = new SubdomainTenantIdResolver(_httpRequestContextAccessor, _options);
    }

    /// <summary>
    /// Configures a different base domain and creates the resolver again for it.
    /// </summary>
    /// <param name="baseDomain">The base domain to resolve tenants in front of.</param>
    /// <remarks>
    /// The base domain is read once, when the resolver is created, so writing the option on its own changes nothing -
    /// which is the point of <c>and_the_base_domain_is_changed_after_the_resolver_was_created</c>. A spec that wants
    /// to resolve against a different base domain has to configure it the way an application does, before the
    /// resolver exists.
    /// </remarks>
    protected void ConfigureBaseDomain(string baseDomain)
    {
        _arcOptions.Tenancy.BaseDomain = baseDomain;
        _resolver = new SubdomainTenantIdResolver(_httpRequestContextAccessor, _options);
    }
}
