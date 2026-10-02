// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_identity_request;

public class with_a_legacy_cookie_and_an_unauthorized_principal : given.an_asp_net_core_identity_request
{
    void Establish()
    {
        AuthenticateAs("user-123", "Test User", "Reader");
        SendForgedIdentityCookie();
        _identityDetailsProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(new IdentityDetails(false, new { }));
    }

    async Task Because() => await _handler(_context);

    [Fact] void should_respond_with_forbidden() => _httpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
    [Fact] void should_not_report_an_identity() => ResponseBody.ShouldBeEmpty();
    [Fact] void should_expire_the_identity_cookie() => _httpContext.Response.Headers.SetCookie.ToString().ShouldContain($"{ForgedIdentityCookieName}=; expires=Thu, 01 Jan 1970 00:00:00 GMT");
    [Fact] void should_expire_the_cookie_at_the_root_path() => _httpContext.Response.Headers.SetCookie.ToString().ShouldContain("path=/");
}
