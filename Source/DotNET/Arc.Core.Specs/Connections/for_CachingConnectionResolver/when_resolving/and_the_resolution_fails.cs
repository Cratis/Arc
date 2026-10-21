// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_CachingConnectionResolver.when_resolving;

public class and_the_resolution_fails : given.a_caching_connection_resolver
{
    Exception? _error;
    ResolvedConnection _result;

    async Task Because()
    {
        _inner.Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ResolvedConnection>(new TimeoutException()), _ => Task.FromResult(_resolved));

        try
        {
            await _resolver.Resolve(_request);
        }
        catch (TimeoutException ex)
        {
            _error = ex;
        }

        _result = await _resolver.Resolve(_request);
    }

    [Fact] void should_propagate_the_failure() => _error.ShouldNotBeNull();
    [Fact] void should_not_keep_the_failure() => _result.ShouldEqual(_resolved);
}
