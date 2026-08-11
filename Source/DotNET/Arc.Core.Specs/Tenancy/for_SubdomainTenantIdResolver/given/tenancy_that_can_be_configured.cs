// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.given;

public class tenancy_that_can_be_configured : Specification
{
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IOptions<ArcOptions> _options;
    protected ArcOptions _arcOptions;

    void Establish()
    {
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _options = Substitute.For<IOptions<ArcOptions>>();
        _arcOptions = new ArcOptions();
        _arcOptions.Tenancy.ResolverType = TenantResolverType.Subdomain;
        _options.Value.Returns(_arcOptions);
    }

    protected SubdomainTenantIdResolver Create() => new(_httpRequestContextAccessor, _options);
}
