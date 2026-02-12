// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_ClaimTenantIdResolver.given;

public class a_claim_tenant_id_resolver : Specification
{
    protected ClaimTenantIdResolver _resolver;
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IOptions<ArcOptions> _options;
    protected IHttpRequestContext _context;
    protected ClaimsPrincipal _user;

    void Establish()
    {
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _options = Substitute.For<IOptions<ArcOptions>>();
        _context = Substitute.For<IHttpRequestContext>();

        var arcOptions = new ArcOptions();
        _options.Value.Returns(arcOptions);

        _user = new ClaimsPrincipal(new ClaimsIdentity());
        _context.User.Returns(_user);
        _httpRequestContextAccessor.Current.Returns(_context);

        _resolver = new ClaimTenantIdResolver(_httpRequestContextAccessor, _options);
    }
}
