// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_identity_request;

/// <summary>
/// The <c>/.cratis/me</c> handler marks the response as not storable on entry and the identity provider marks it again
/// when it writes the identity. Both calls land on the same ASP.NET Core response, which a CORS middleware may
/// already have given <c>Vary: Origin</c>.
/// </summary>
public class with_an_asp_net_core_response_varying_on_origin : given.an_asp_net_core_identity_request
{
    void Establish()
    {
        AuthenticateAs("test-id", "Test User");
        _httpContext.Response.Headers.Vary = "Origin";
    }

    async Task Because() => await _handler(_context);

    [Fact] void should_keep_origin_and_add_cookie_only_once() => _httpContext.Response.Headers.Vary.ToString().ShouldEqual("Origin, Cookie");
    [Fact] void should_set_no_store_cache_control() => _httpContext.Response.Headers.CacheControl.ToString().ShouldEqual("no-store, private");
    [Fact] void should_respond_with_ok() => _httpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
}
