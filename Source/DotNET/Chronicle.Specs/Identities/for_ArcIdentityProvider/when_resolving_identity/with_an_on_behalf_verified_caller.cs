// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_an_on_behalf_verified_caller : given.an_identity_provider
{
    void Establish()
    {
        var caller = new VerifiedCaller("1", CallerPrincipalKind.Person, CallerCredentialKind.BrowserSession, new("credential"), null, new("person-1"), null, new CallerActor("https://issuer.example", "actor-1"), null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
        var request = Substitute.For<IHttpRequestContext>();
        request.User.Returns(new ClaimsPrincipal(new ClaimsIdentity("other")));
        request.Items.Returns(new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = VerifiedCallerPrincipal.Create(caller) });
        _requests.Current.Returns(request);
    }

    void Because() => _result = _provider.GetCurrent();

    [Fact] void should_cause_with_the_actor() => _result.Subject.ShouldEqual("actor-1");

    [Fact] void should_mark_the_actor_as_a_verified_caller() => _result.Name.ShouldEqual(ArcIdentityProvider.VerifiedCallerName);

    [Fact] void should_act_on_behalf_of_the_verified_subject() => _result.OnBehalfOf!.Subject.ShouldEqual("person-1");

    [Fact] void should_mark_the_represented_identity_as_a_verified_caller() => _result.OnBehalfOf!.Name.ShouldEqual(ArcIdentityProvider.VerifiedCallerName);
}
