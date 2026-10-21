// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.when_resolving.with_authentication;

public class and_the_endpoint_accepts_the_token : given.an_authenticated_http_connection_resolver
{
    ResolvedConnection _result;

    async Task Because()
    {
        await _resolver.Resolve(_request);
        _result = await _resolver.Resolve(_request);
    }

    [Fact] void should_send_the_bearer_token() => AuthorizationOf(0).ShouldEqual("Bearer token-1");
    [Fact] void should_send_the_configured_headers_alongside() => _sentRequests[0].Headers.GetValues("X-Api-Key").ShouldContain("the-key");
    [Fact] void should_reuse_the_token() => AuthorizationOf(1).ShouldEqual("Bearer token-1");
    [Fact] void should_get_one_token() => _tokensIssued.ShouldEqual(1);
    [Fact] void should_answer() => _result.ShouldEqual(ResolvedConnection.Unchanged);
}
