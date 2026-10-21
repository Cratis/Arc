// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.when_resolving;

public class and_the_endpoint_answers_no_content : given.an_http_connection_resolver
{
    ResolvedConnection _result;

    async Task Because() => _result = await _resolver.Resolve(_request);

    [Fact] void should_leave_everything_as_configured() => _result.ShouldEqual(ResolvedConnection.Unchanged);
}
