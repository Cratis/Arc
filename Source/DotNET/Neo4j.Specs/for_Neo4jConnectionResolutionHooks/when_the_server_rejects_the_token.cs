// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_Neo4jConnectionResolutionHooks;

public class when_the_server_rejects_the_token : Specification
{
    ICachingConnectionResolver _resolver;
    ConnectionRequest _request;
    Neo4jConnectionResolutionHooks _hooks;
    bool _retryUnauthorized;
    bool _retryExpired;
    bool _retryOther;

    void Establish()
    {
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _request = ConnectionRequest.ForTenant(WellKnownConnectionStorage.Neo4j, ConnectionPurpose.Runtime, "acme");
        _hooks = new(_resolver, _request, new(null, null));
    }

    async Task Because()
    {
        _retryOther = await _hooks.HandleSecurityExceptionAsync(AuthTokens.None, new SecurityException("Neo.ClientError.Security.Forbidden", "no"));
        _resolver.DidNotReceiveWithAnyArgs().Invalidate(default!);
        _retryUnauthorized = await _hooks.HandleSecurityExceptionAsync(AuthTokens.None, new SecurityException("Neo.ClientError.Security.Unauthorized", "no"));
        _retryExpired = await _hooks.HandleSecurityExceptionAsync(AuthTokens.None, new SecurityException("Neo.ClientError.Security.TokenExpired", "expired"));
    }

    [Fact] void should_not_retry_another_security_failure() => _retryOther.ShouldBeFalse();
    [Fact] void should_retry_when_unauthorized() => _retryUnauthorized.ShouldBeTrue();
    [Fact] void should_retry_when_the_token_expired() => _retryExpired.ShouldBeTrue();
    [Fact] void should_drop_the_cached_resolution() => _resolver.Received(2).Invalidate(_request);
}
