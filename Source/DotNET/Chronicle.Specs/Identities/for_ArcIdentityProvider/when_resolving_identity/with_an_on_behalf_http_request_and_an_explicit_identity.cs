// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_an_on_behalf_http_request_and_an_explicit_identity : given.an_on_behalf_principal
{
    readonly ChronicleIdentity _explicit = new("reactor", "Reactor");

    void Establish()
    {
        var request = Substitute.For<IHttpRequestContext>();
        request.User.Returns(_onBehalfPrincipal);
        _requests.Current.Returns(request);
    }

    void Because()
    {
        _provider.SetCurrentIdentity(_explicit);
        _result = _provider.GetCurrent();
    }

    [Fact] void should_preserve_the_explicit_identity() => _result.ShouldEqual(_explicit);
}
