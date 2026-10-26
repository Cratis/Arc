// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Chronicle.Identities;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_an_on_behalf_http_request : given.an_on_behalf_principal
{
    void Establish()
    {
        var request = Substitute.For<IHttpRequestContext>();
        request.User.Returns(_onBehalfPrincipal);
        _requests.Current.Returns(request);
        _fallback = Substitute.For<IIdentityProvider>();
        _fallback.GetCurrent().Returns(_represented);
        _provider = new(_fallback, _principals, () => _requests.Current is not null);
    }

    void Because() => _result = _provider.GetCurrent();

    [Fact] void should_cause_with_the_actor() => _result.Subject.ShouldEqual(_actor.Subject);
    [Fact] void should_act_on_behalf_of_the_represented_user() => _result.OnBehalfOf.ShouldEqual(_represented);
    [Fact] void should_record_the_whole_chain() => _result.ShouldEqual(_actor with { OnBehalfOf = _represented });
}
