// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_identity_request;

public class with_a_forged_identity_cookie_and_no_principal : given.an_asp_net_core_identity_request
{
    void Establish() => SendForgedIdentityCookie();

    async Task Because() => await _handler(_context);

    [Fact] void should_respond_with_unauthorized() => _httpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status401Unauthorized);
    [Fact] void should_not_report_the_forged_identity() => ResponseBody.ShouldBeEmpty();
}
