// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolverAccessTokens.when_getting;

public class before_the_refresh_margin : given.access_tokens
{
    string _first;
    string _second;

    async Task Because()
    {
        _first = await _tokens.Get(CancellationToken.None);
        _time.Now = _time.Now.AddMinutes(54);
        _second = await _tokens.Get(CancellationToken.None);
    }

    [Fact] void should_reuse_the_token() => _second.ShouldEqual(_first);
    [Fact] void should_get_one_token() => _tokensIssued.ShouldEqual(1);
}
