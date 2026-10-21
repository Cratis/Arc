// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using MongoDB.Driver.Authentication.Oidc;

namespace Cratis.Arc.MongoDB.for_MongoDBConnectionResolutionHooks;

public class when_the_driver_asks_for_an_access_token : given.hooks
{
    OidcAccessToken _result;

    void Establish() => _resolver.Resolve(_request, Arg.Any<CancellationToken>())
        .Returns(new ResolvedConnection(Identity: new(AccessToken: "fresh-token"), ExpiresAt: DateTimeOffset.UtcNow.AddHours(1)));

    async Task Because() => _result = await _hooks.GetOidcAccessTokenAsync(new OidcCallbackParameters(1, null), CancellationToken.None);

    [Fact] void should_drop_the_cached_resolution_first() => _resolver.Received(1).Invalidate(_request);
    [Fact] void should_return_the_resolved_token() => _result.AccessToken.ShouldEqual("fresh-token");
    [Fact] void should_say_when_it_expires() => _result.ExpiresIn.ShouldNotBeNull();
}
