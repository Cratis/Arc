// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_QueryTenantIdResolver.given;

public class a_query_tenant_id_resolver : Specification
{
    protected QueryTenantIdResolver _resolver;
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IOptions<ArcOptions> _options;
    protected IHttpRequestContext _context;
    protected Dictionary<string, string> _query;

    void Establish()
    {
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _options = Substitute.For<IOptions<ArcOptions>>();
        _context = Substitute.For<IHttpRequestContext>();
        _query = [];

        var arcOptions = new ArcOptions();
        _options.Value.Returns(arcOptions);

        _context.Query.Returns(_query);
        _httpRequestContextAccessor.Current.Returns(_context);

        _resolver = new QueryTenantIdResolver(_httpRequestContextAccessor, _options);
    }
}
