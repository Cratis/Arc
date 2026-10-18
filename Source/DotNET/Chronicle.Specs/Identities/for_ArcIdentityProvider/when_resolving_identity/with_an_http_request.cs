// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Chronicle.Identities;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_an_http_request : given.an_identity_provider
{
    void Establish()
    {
        var request = Substitute.For<IHttpRequestContext>();
        request.User.Returns(_principal);
        _requests.Current.Returns(request);
        _fallback = Substitute.For<IIdentityProvider>();
        _fallback.GetCurrent().Returns(new ChronicleIdentity("http", "HTTP user"));
        _provider = new(_fallback, _principals, () => _requests.Current is not null);
    }

    void Because()
    {
        using var scope = _execution.As(_principal);
        _result = _provider.GetCurrent();
    }

    [Fact] void should_preserve_http_resolution() => _result.ShouldEqual(new ChronicleIdentity("http", "HTTP user"));
}
