// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_identity_request;

public class with_a_forged_identity_cookie_and_an_authenticated_principal : given.an_asp_net_core_identity_request
{
    void Establish()
    {
        AuthenticateAs("user-123", "Test User", "Reader");
        SendForgedIdentityCookie();
    }

    async Task Because() => await _handler(_context);

    [Fact] void should_respond_with_ok() => _httpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_report_the_principal() => ResponseBody.ShouldContain("\"id\":\"user-123\"");
    [Fact] void should_report_the_roles_of_the_principal() => ResponseBody.ShouldContain("\"roles\":[\"Reader\"]");
    [Fact] void should_report_the_details_provided_for_the_principal() => ResponseBody.ShouldContain("Engineering");
    [Fact] void should_not_report_the_forged_identity() => ResponseBody.ShouldNotContain("Forged");
    [Fact] void should_not_write_an_identity_cookie() => _httpContext.Response.Headers.SetCookie.ToString().ShouldNotContain(ForgedIdentityCookieName);
}
