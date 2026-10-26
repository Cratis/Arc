// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Chronicle.Identities;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

/// <summary>
/// A bearer token carrying an RFC 8693 <c>act</c> claim (the JWT handlers' <c>actort</c>) makes the token's subject the
/// principal and the delegating client its <see cref="ClaimsIdentity.Actor"/>. Nothing about this is AuthProxy specific:
/// any scheme that sets an actor changes the identity Chronicle records.
/// </summary>
public class with_an_actor_set_by_another_authentication_scheme : given.an_identity_provider
{
    static readonly ChronicleIdentity _client = new("billing-service", "Billing Service", "billing-service");
    static readonly ChronicleIdentity _user = new("user-123", "User", "user");

    void Establish()
    {
        var user = new ClaimsIdentity([new Claim("sub", "user-123"), new Claim("name", "User"), new Claim("preferred_username", "user")], "Bearer")
        {
            Actor = new ClaimsIdentity([new Claim("sub", "billing-service"), new Claim("name", "Billing Service"), new Claim("preferred_username", "billing-service")], "AuthenticationTypes.Federation")
        };
        var request = Substitute.For<IHttpRequestContext>();
        request.User.Returns(new ClaimsPrincipal(user));
        _requests.Current.Returns(request);
        _fallback = Substitute.For<IIdentityProvider>();
        _fallback.GetCurrent().Returns(_user);
        _provider = new(_fallback, _principals, () => _requests.Current is not null);
    }

    void Because() => _result = _provider.GetCurrent();

    [Fact] void should_record_the_actor_as_the_cause() => _result.ShouldEqual(_client with { OnBehalfOf = _user });
    [Fact] void should_not_record_what_chronicle_resolved_for_the_request() => _result.ShouldNotEqual(_user);
}
