// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_CachingConnectionResolver;

public class when_invalidating : given.a_caching_connection_resolver
{
    async Task Because()
    {
        await _resolver.Resolve(_request);
        _resolver.Invalidate(_request);
        await _resolver.Resolve(_request);
    }

    [Fact] void should_resolve_again() => _inner.Received(2).Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>());
}
