// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_CachingConnectionResolver.when_resolving;

public class and_the_resolution_expires_soon : given.a_caching_connection_resolver
{
    async Task Because()
    {
        _resolved = new(Identity: new(AccessToken: "token"), ExpiresAt: _time.Now + TimeSpan.FromMinutes(2));
        await _resolver.Resolve(_request);
        _time.Now += TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1);
        await _resolver.Resolve(_request);
    }

    [Fact] void should_renew_within_the_expiry_margin() => _inner.Received(2).Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>());
}
