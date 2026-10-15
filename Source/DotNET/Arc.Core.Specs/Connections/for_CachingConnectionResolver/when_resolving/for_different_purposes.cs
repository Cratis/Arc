// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_CachingConnectionResolver.when_resolving;

public class for_different_purposes : given.a_caching_connection_resolver
{
    async Task Because()
    {
        await _resolver.Resolve(_request);
        await _resolver.Resolve(_request with { Purpose = ConnectionPurpose.Schema });
    }

    [Fact] void should_resolve_each_purpose() => _inner.Received(2).Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>());
}
