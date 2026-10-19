// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.when_resolving.with_authentication;

public class and_the_endpoint_forbids : given.an_authenticated_http_connection_resolver
{
    Exception? _error;

    void Establish() => _statuses.Enqueue(HttpStatusCode.Forbidden);

    async Task Because() => _error = await Catch.Exception(() => _resolver.Resolve(_request));

    [Fact] void should_not_retry() => _sentRequests.Count.ShouldEqual(1);
    [Fact] void should_not_get_another_token() => _tokensIssued.ShouldEqual(1);
    [Fact] void should_fail_with_connection_resolution_failed() => _error.ShouldBeOfExactType<ConnectionResolutionFailed>();
}
