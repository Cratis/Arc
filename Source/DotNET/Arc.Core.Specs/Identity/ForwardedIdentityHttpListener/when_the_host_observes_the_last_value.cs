// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHttpListener;

public class when_the_host_observes_the_last_value : given.a_listener
{
    AuthenticationResult _signedLast;
    AuthenticationResult _unsignedLast;
    string[] _signedObserved;
    string[] _unsignedObserved;

    async Task Because()
    {
        var headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers);
        headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "earlier-unsigned";
        _signedLast = await Authenticate(headers, appendedName: "n");
        _signedObserved = _observedNames;
        headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers);
        _unsignedLast = await Authenticate(headers, appendedName: "last-unsigned");
        _unsignedObserved = _observedNames;
    }

    [Fact] void should_observe_only_the_last_signed_value_on_managed_listener() => _signedObserved.ShouldEqual(["n"]);
    [Fact] void should_accept_only_when_the_observed_value_was_signed() => _signedLast.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_observe_the_last_unsigned_value_on_managed_listener() => _unsignedObserved.ShouldEqual(["last-unsigned"]);
    [Fact] void should_not_establish_identity_when_the_last_value_was_not_signed() => _unsignedLast.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_construct_identity_from_the_verified_snapshot() => _signedLast.Principal.Identity.Name.ShouldEqual("n");
}
