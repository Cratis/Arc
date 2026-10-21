// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.when_resolving.with_authentication;

public class and_the_endpoint_rejects_the_token_once : given.an_authenticated_http_connection_resolver
{
    ResolvedConnection _result;

    void Establish() => _statuses.Enqueue(HttpStatusCode.Unauthorized);

    async Task Because() => _result = await _resolver.Resolve(_request);

    [Fact] void should_send_twice() => _sentRequests.Count.ShouldEqual(2);
    [Fact] void should_retry_with_a_fresh_token() => AuthorizationOf(1).ShouldEqual("Bearer token-2");
    [Fact] void should_answer() => _result.ShouldEqual(ResolvedConnection.Unchanged);
}
