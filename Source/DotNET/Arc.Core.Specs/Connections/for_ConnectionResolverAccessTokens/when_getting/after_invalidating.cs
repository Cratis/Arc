// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolverAccessTokens.when_getting;

public class after_invalidating : given.access_tokens
{
    string _second;

    async Task Because()
    {
        _tokens.Invalidate(await _tokens.Get(CancellationToken.None));
        _second = await _tokens.Get(CancellationToken.None);
    }

    [Fact] void should_get_a_fresh_token() => _second.ShouldEqual("token-2");
}
