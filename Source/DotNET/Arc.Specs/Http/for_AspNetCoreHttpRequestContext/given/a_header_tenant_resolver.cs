// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_AspNetCoreHttpRequestContext.given;

public class a_header_tenant_resolver : a_default_http_context
{
    protected HeaderTenantIdResolver _resolver;

    void Establish()
    {
        var accessor = Substitute.For<IHttpRequestContextAccessor>();
        accessor.Current.Returns(_context);
        _resolver = new HeaderTenantIdResolver(accessor, Options.Create(new ArcOptions()));
    }
}
