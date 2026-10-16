// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_CachingConnectionResolver.when_resolving;

public class without_a_cache_duration : given.a_caching_connection_resolver
{
    async Task Because()
    {
        _resolver = new(_inner, TimeSpan.Zero, _time);
        await _resolver.Resolve(_request);
        await _resolver.Resolve(_request);
    }

    [Fact] void should_resolve_every_time() => _inner.Received(2).Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>());
}
