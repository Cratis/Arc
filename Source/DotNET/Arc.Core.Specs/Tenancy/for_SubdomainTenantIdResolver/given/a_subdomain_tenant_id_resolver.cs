// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.given;

public class a_subdomain_tenant_id_resolver : Specification
{
    protected SubdomainTenantIdResolver _resolver;
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IOptions<ArcOptions> _options;
    protected IHttpRequestContext _context;
    protected Dictionary<string, string> _headers;

    void Establish()
    {
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _options = Substitute.For<IOptions<ArcOptions>>();
        _context = Substitute.For<IHttpRequestContext>();
        _headers = [];

        var arcOptions = new ArcOptions();
        arcOptions.UseHeaderTenancy("X-Tenant-Id");
        _options.Value.Returns(arcOptions);

        _context.Headers.Returns(_headers);
        _httpRequestContextAccessor.Current.Returns(_context);

        _resolver = new SubdomainTenantIdResolver(_httpRequestContextAccessor, _options);
    }
}
