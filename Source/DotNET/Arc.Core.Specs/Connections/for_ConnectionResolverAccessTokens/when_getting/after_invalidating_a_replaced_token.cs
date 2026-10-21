// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolverAccessTokens.when_getting;

public class after_invalidating_a_replaced_token : given.access_tokens
{
    string _result;

    async Task Because()
    {
        _tokens.Invalidate(await _tokens.Get(CancellationToken.None));
        await _tokens.Get(CancellationToken.None);
        _tokens.Invalidate("token-1");
        _result = await _tokens.Get(CancellationToken.None);
    }

    [Fact] void should_keep_the_current_token() => _result.ShouldEqual("token-2");
    [Fact] void should_not_get_another_token() => _tokensIssued.ShouldEqual(2);
}
