// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_HeaderTenantIdResolver.given;

public class a_header_tenant_id_resolver : Specification
{
    protected HeaderTenantIdResolver _resolver;
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
        _options.Value.Returns(arcOptions);

        _context.Headers.Returns(_headers);
        _httpRequestContextAccessor.Current.Returns(_context);

        _resolver = new HeaderTenantIdResolver(_httpRequestContextAccessor, _options);
    }
}
