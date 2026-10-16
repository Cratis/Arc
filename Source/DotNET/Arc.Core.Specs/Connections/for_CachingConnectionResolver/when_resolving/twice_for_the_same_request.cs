// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_CachingConnectionResolver.when_resolving;

public class twice_for_the_same_request : given.a_caching_connection_resolver
{
    ResolvedConnection _result;

    async Task Because()
    {
        await _resolver.Resolve(_request);
        _result = await _resolver.Resolve(_request with { Scope = new Dictionary<string, string> { ["tenant"] = "acme" } });
    }

    [Fact] void should_resolve_once() => _inner.Received(1).Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>());
    [Fact] void should_return_the_resolved_connection() => _result.ShouldEqual(_resolved);
}
