// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Chronicle.Identities;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_a_non_verified_http_user : given.an_identity_provider
{
    void Establish()
    {
        var request = Substitute.For<IHttpRequestContext>();
        request.User.Returns(_principal);
        request.Items.Returns(new Dictionary<object, object?>());
        _requests.Current.Returns(request);
        _fallback = Substitute.For<IIdentityProvider>();
        _fallback.GetCurrent().Returns(new ChronicleIdentity("http", "HTTP user"));
        _provider = new(_fallback, _principals, () => _requests.Current is not null);
    }

    void Because() => _result = _provider.GetCurrent();

    [Fact] void should_leave_the_identity_to_the_wrapped_provider() => _result.ShouldEqual(new ChronicleIdentity("http", "HTTP user"));
}
