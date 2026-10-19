// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.when_resolving.with_authentication;

public class and_the_endpoint_keeps_rejecting_the_token : given.an_authenticated_http_connection_resolver
{
    Exception? _error;

    void Establish()
    {
        _statuses.Enqueue(HttpStatusCode.Unauthorized);
        _statuses.Enqueue(HttpStatusCode.Unauthorized);
        _statuses.Enqueue(HttpStatusCode.Unauthorized);
    }

    async Task Because() => _error = await Catch.Exception(() => _resolver.Resolve(_request));

    [Fact] void should_retry_exactly_once() => _sentRequests.Count.ShouldEqual(2);
    [Fact] void should_fail_with_connection_resolution_failed() => _error.ShouldBeOfExactType<ConnectionResolutionFailed>();
    [Fact] void should_name_the_status() => _error!.Message.ShouldContain("401");
    [Fact] void should_not_reveal_the_token() => _error!.Message.ShouldNotContain("token-");
}
