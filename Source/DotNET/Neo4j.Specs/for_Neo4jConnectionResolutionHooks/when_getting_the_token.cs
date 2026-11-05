// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_Neo4jConnectionResolutionHooks;

public class when_getting_the_token : Specification
{
    ICachingConnectionResolver _resolver;
    ConnectionRequest _request;
    Neo4jConnectionResolutionHooks _hooks;
    IAuthToken _password;
    IAuthToken _bearer;
    IAuthToken _configured;

    void Establish()
    {
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _request = ConnectionRequest.ForTenant(WellKnownConnectionStorage.Neo4j, ConnectionPurpose.Runtime, "acme");
        _hooks = new(_resolver, _request, new("configured", "configured-password"));
    }

    async Task Because()
    {
        _resolver.Resolve(_request, Arg.Any<CancellationToken>()).Returns(new ResolvedConnection(Identity: new("acme-user", "acme-password")));
        _password = await _hooks.GetTokenAsync();
        _resolver.Resolve(_request, Arg.Any<CancellationToken>()).Returns(new ResolvedConnection(Identity: new(AccessToken: "token")));
        _bearer = await _hooks.GetTokenAsync();
        _resolver.Resolve(_request, Arg.Any<CancellationToken>()).Returns(ResolvedConnection.Unchanged);
        _configured = await _hooks.GetTokenAsync();
    }

    [Fact] void should_authenticate_with_the_resolved_user_name_and_password() => _password.ShouldEqual(AuthTokens.Basic("acme-user", "acme-password"));
    [Fact] void should_authenticate_with_a_resolved_access_token_as_bearer() => _bearer.ShouldEqual(AuthTokens.Bearer("token"));
    [Fact] void should_fall_back_to_the_configured_credentials() => _configured.ShouldEqual(AuthTokens.Basic("configured", "configured-password"));
}
