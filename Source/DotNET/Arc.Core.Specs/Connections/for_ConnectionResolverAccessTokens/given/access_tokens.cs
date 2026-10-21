// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolverAccessTokens.given;

public class access_tokens : Specification
{
    protected ManualTimeProvider _time;
    protected HttpConnectionResolverAuthenticationOptions _options;
    protected IConnectionResolverAccessTokenProvider _provider;
    protected int _tokensIssued;
    internal ConnectionResolverAccessTokens _tokens;

    void Establish()
    {
        _time = new(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));
        _options = new() { Type = HttpConnectionResolverAuthenticationType.ClientCredentials, RefreshMargin = TimeSpan.FromMinutes(5) };
        _provider = Substitute.For<IConnectionResolverAccessTokenProvider>();
        _provider.GetToken(_options, Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ConnectionResolverAccessToken>(new ConnectionResolverAccessToken($"token-{++_tokensIssued}", _time.Now.AddHours(1))));
        _tokens = new(_provider, _options, _time);
    }

    protected sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
