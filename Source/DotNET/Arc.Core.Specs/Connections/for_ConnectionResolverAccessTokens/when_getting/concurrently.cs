// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolverAccessTokens.when_getting;

public class concurrently : given.access_tokens
{
    TaskCompletionSource<ConnectionResolverAccessToken> _issued;
    int _calls;
    string[] _results;

    void Establish()
    {
        _issued = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _provider.GetToken(_options, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Interlocked.Increment(ref _calls);
            return new ValueTask<ConnectionResolverAccessToken>(_issued.Task);
        });
    }

    async Task Because()
    {
        var waiting = Enumerable.Range(0, 10).Select(_ => Task.Run(async () => await _tokens.Get(CancellationToken.None))).ToArray();
        await Task.Delay(50);
        _issued.SetResult(new("the-token", _time.Now.AddHours(1)));
        _results = await Task.WhenAll(waiting);
    }

    [Fact] void should_get_one_token() => _calls.ShouldEqual(1);
    [Fact] void should_give_every_caller_the_token() => _results.ShouldEachConformTo(_ => _ == "the-token");
}
