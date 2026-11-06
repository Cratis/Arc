// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_a_verified_caller : given.an_identity_provider
{
    void Establish()
    {
        var caller = new VerifiedCaller("1", CallerPrincipalKind.Person, CallerCredentialKind.BrowserSession, new("credential"), null, new("person-1"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
        var request = Substitute.For<IHttpRequestContext>();
        request.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "someone-else")], "other")));
        request.Items.Returns(new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = VerifiedCallerPrincipal.Create(caller) });
        _requests.Current.Returns(request);
    }

    void Because() => _result = _provider.GetCurrent();

    [Fact] void should_not_record_the_request_user() => _result.Subject.ShouldNotEqual("someone-else");

    [Fact] void should_leave_a_plain_caller_to_the_wrapped_provider() => _result.ShouldEqual(_fallback.GetCurrent());
}
