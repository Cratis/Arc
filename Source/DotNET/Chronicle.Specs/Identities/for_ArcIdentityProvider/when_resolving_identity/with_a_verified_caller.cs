// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

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

    [Fact] void should_use_the_verified_subject() => _result.Subject.ShouldEqual("person-1");

    [Fact] void should_mark_it_as_a_verified_caller() => _result.Name.ShouldEqual(ArcIdentityProvider.VerifiedCallerName);

    [Fact] void should_name_the_credential_kind() => _result.UserName.ShouldEqual($"{ArcIdentityProvider.VerifiedCallerUserNamePrefix}{nameof(CallerCredentialKind.BrowserSession)}");

    [Fact] void should_not_record_the_request_user() => _result.Subject.ShouldNotEqual("someone-else");

    [Fact] void should_not_be_a_delegation_chain() => _result.OnBehalfOf.ShouldBeNull();

    [Fact] void should_not_collapse_to_a_system_or_unknown_identity() => (_result == ChronicleIdentity.System || _result == ChronicleIdentity.NotSet).ShouldBeFalse();
}
